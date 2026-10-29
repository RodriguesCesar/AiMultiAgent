using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text;
using AiMultiAgent.Application.Interfaces;
using AiMultiAgent.Infrastructure.Data.MongoDB;
using AiMultiAgent.Infrastructure.Rabbit;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace AiMultiAgent.Worker;

public class Worker(
    ILogger<Worker> logger,
    IMongoRepository mongoRepository,
    IOrchestratorAgent orchestratorAgent,
    RabbitSettings rabbitSettings) : BackgroundService
{
    private const string RetryHeaderName = "x-retry-count";
    private static readonly ActivitySource ActivitySource = new("AiMultiAgent.Worker");
    private static readonly Meter Meter = new("AiMultiAgent.Worker");
    private static readonly Counter<long> ProcessedCounter = Meter.CreateCounter<long>("ai_worker_messages_processed_total");
    private static readonly Counter<long> FailedCounter = Meter.CreateCounter<long>("ai_worker_messages_failed_total");
    private static readonly Counter<long> RetriedCounter = Meter.CreateCounter<long>("ai_worker_messages_retried_total");
    private static readonly Counter<long> DlqCounter = Meter.CreateCounter<long>("ai_worker_messages_dlq_total");
    private static readonly Histogram<double> ProcessingDurationMs = Meter.CreateHistogram<double>("ai_worker_message_processing_ms");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory { HostName = rabbitSettings.HostName };

        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await EnsureTopologyAsync(channel, stoppingToken);
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            var message = Encoding.UTF8.GetString(ea.Body.ToArray());
            var startedAt = Stopwatch.GetTimestamp();

            using var activity = ActivitySource.StartActivity("worker.process_message", ActivityKind.Consumer);
            activity?.SetTag("messaging.system", "rabbitmq");
            activity?.SetTag("messaging.destination", rabbitSettings.QueueName);

            try
            {
                logger.LogInformation("Mensagem lida da fila com sucesso: {Message}", message);

                var response = await orchestratorAgent.ProcessAsync(message);
                await mongoRepository.SaveAsync(message, response);

                await channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);

                ProcessedCounter.Add(1, new KeyValuePair<string, object?>("queue", rabbitSettings.QueueName));
            }
            catch (Exception ex)
            {
                FailedCounter.Add(1, new KeyValuePair<string, object?>("queue", rabbitSettings.QueueName));

                var currentRetry = ParseRetryCount(ea.BasicProperties?.Headers);

                if (ShouldRetry(currentRetry, rabbitSettings.MaxRetryCount))
                {
                    var nextRetry = currentRetry + 1;
                    await RepublishWithRetryAsync(channel, message, ea.BasicProperties?.Headers, nextRetry, stoppingToken);
                    await channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);

                    RetriedCounter.Add(1, new KeyValuePair<string, object?>("queue", rabbitSettings.QueueName));

                    logger.LogWarning(
                        ex,
                        "Erro ao processar mensagem. Reenfileirando para retry {Retry}/{MaxRetry}. Mensagem: {Message}",
                        nextRetry,
                        rabbitSettings.MaxRetryCount,
                        message
                    );
                }
                else
                {
                    await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false, stoppingToken);
                    DlqCounter.Add(1, new KeyValuePair<string, object?>("queue", rabbitSettings.DeadLetterQueueName));

                    logger.LogError(
                        ex,
                        "Erro ao processar mensagem após {RetryCount} tentativas. Enviada para DLQ {DeadLetterQueue}. Mensagem: {Message}",
                        currentRetry,
                        rabbitSettings.DeadLetterQueueName,
                        message
                    );
                }
            }
            finally
            {
                var elapsedMs = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
                ProcessingDurationMs.Record(elapsedMs, new KeyValuePair<string, object?>("queue", rabbitSettings.QueueName));
            }
        };

        await channel.BasicConsumeAsync(
            queue: rabbitSettings.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken
        );

        logger.LogInformation("Worker aguardando mensagens na fila {QueueName}...", rabbitSettings.QueueName);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task EnsureTopologyAsync(IChannel channel, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(
            exchange: rabbitSettings.DeadLetterExchange,
            type: ExchangeType.Direct,
            durable: true,
            cancellationToken: cancellationToken
        );

        await channel.QueueDeclareAsync(
            queue: rabbitSettings.DeadLetterQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken
        );

        await channel.QueueBindAsync(
            queue: rabbitSettings.DeadLetterQueueName,
            exchange: rabbitSettings.DeadLetterExchange,
            routingKey: rabbitSettings.DeadLetterQueueName,
            cancellationToken: cancellationToken
        );

        var queueArgs = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = rabbitSettings.DeadLetterExchange,
            ["x-dead-letter-routing-key"] = rabbitSettings.DeadLetterQueueName
        };

        await channel.QueueDeclareAsync(
            queue: rabbitSettings.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArgs,
            cancellationToken: cancellationToken
        );
    }

    private async Task RepublishWithRetryAsync(
        IChannel channel,
        string message,
        IReadOnlyDictionary<string, object?>? headers,
        int nextRetry,
        CancellationToken cancellationToken)
    {
        var body = Encoding.UTF8.GetBytes(message);

        var clonedHeaders = headers == null
            ? new Dictionary<string, object?>()
            : new Dictionary<string, object?>(headers);

        clonedHeaders[RetryHeaderName] = nextRetry;

        var properties = new BasicProperties
        {
            Persistent = true,
            Headers = clonedHeaders
        };

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: rabbitSettings.QueueName,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken
        );
    }

    internal static bool ShouldRetry(int currentRetry, int maxRetryCount)
    {
        return currentRetry < maxRetryCount;
    }

    internal static int ParseRetryCount(IReadOnlyDictionary<string, object?>? headers)
    {
        if (headers == null || !headers.TryGetValue(RetryHeaderName, out var value) || value == null)
            return 0;

        return value switch
        {
            byte b => b,
            sbyte sb => sb,
            short s => s,
            int i => i,
            long l => (int)l,
            byte[] bytes when int.TryParse(Encoding.UTF8.GetString(bytes), out var parsed) => parsed,
            string text when int.TryParse(text, out var parsed) => parsed,
            _ => 0
        };
    }
}
