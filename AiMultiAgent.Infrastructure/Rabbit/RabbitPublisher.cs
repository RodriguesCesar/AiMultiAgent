using AiMultiAgent.Infrastructure.Rabbit.Infaces;
using RabbitMQ.Client;
using System.Text;

namespace AiMultiAgent.Infrastructure.Rabbit;

public class RabbitPublisher(RabbitSettings rabbitSettings) : IRabbitPublisher
{
    public async Task PublishAsync(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Mensagem não pode ser vazia.", nameof(message));

        var factory = new ConnectionFactory { HostName = rabbitSettings.HostName };
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync(
            queue: rabbitSettings.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        var body = Encoding.UTF8.GetBytes(message);
        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: rabbitSettings.QueueName,
            body: body
        );
    }
}
