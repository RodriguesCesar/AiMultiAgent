using AiMultiAgent.Application.Interfaces;
using AiMultiAgent.Infrastructure.Data.MongoDB;
using AiMultiAgent.Infrastructure.Rabbit;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace AiMultiAgent.Worker;

public class Worker(
    ILogger<Worker> logger,
    IMongoRepository mongoRepository,
    IOrchestratorAgent orchestratorAgent,
    RabbitSettings rabbitSettings) : BackgroundService
{
    private const string RetryHeaderName = "x-retry-count";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory { HostName = rabbitSettings.HostName };

        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await EnsureTopologyAsync(channel, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            var message = Encoding.UTF8.GetString(ea.Body.ToArray());

            try
            {
                logger.LogInformation("Mensagem lida da fila com sucesso: {Message}", message);

                var response = await orchestratorAgent.ProcessAsync(message);
                await mongoRepository.SaveAsync(message, response);

                await channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
            }
            catch (Exception ex)
            {
                var currentRetry = GetRetryCount(ea.BasicProperties?.Headers);

                if (currentRetry < rabbitSettings.MaxRetryCount)
                {
                    var nextRetry = currentRetry + 1;
                    await RepublishWithRetryAsync(channel, message, ea.BasicProperties?.Headers, nextRetry, stoppingToken);
                    await channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);

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

                    logger.LogError(
                        ex,
                        "Erro ao processar mensagem após {RetryCount} tentativas. Enviada para DLQ {DeadLetterQueue}. Mensagem: {Message}",
                        currentRetry,
                        rabbitSettings.DeadLetterQueueName,
                        message
                    );
                }
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

    private static int GetRetryCount(IReadOnlyDictionary<string, object?>? headers)
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
