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
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory { HostName = rabbitSettings.HostName };

        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: rabbitSettings.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken
        );

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                logger.LogInformation("Mensagem lida da fila com sucesso: {Message}", message);

                var response = await orchestratorAgent.ProcessAsync(message);
                await mongoRepository.SaveAsync(message, response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao processar mensagem: {ErrorMessage}", ex.Message);
            }
        };

        await channel.BasicConsumeAsync(
            queue: rabbitSettings.QueueName,
            autoAck: true,
            consumer: consumer,
            cancellationToken: stoppingToken
        );

        logger.LogInformation("Worker aguardando mensagens na fila {QueueName}...", rabbitSettings.QueueName);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
