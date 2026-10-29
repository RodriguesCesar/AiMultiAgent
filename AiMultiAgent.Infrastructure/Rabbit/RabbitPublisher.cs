using AiMultiAgent.Infrastructure.Rabbit.Infaces;
using RabbitMQ.Client;
using System.Text;

namespace AiMultiAgent.Infrastructure.Rabbit;

public class RabbitPublisher(RabbitSettings rabbitSettings) : IRabbitPublisher
{
    private const string RetryHeaderName = "x-retry-count";

    public async Task PublishAsync(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Mensagem não pode ser vazia.", nameof(message));

        var factory = new ConnectionFactory { HostName = rabbitSettings.HostName };
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        await EnsureTopologyAsync(channel);

        var body = Encoding.UTF8.GetBytes(message);

        var properties = new BasicProperties
        {
            Persistent = true,
            Headers = new Dictionary<string, object?>
            {
                [RetryHeaderName] = 0
            }
        };

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: rabbitSettings.QueueName,
            mandatory: false,
            basicProperties: properties,
            body: body
        );
    }

    private async Task EnsureTopologyAsync(IChannel channel)
    {
        await channel.ExchangeDeclareAsync(
            exchange: rabbitSettings.DeadLetterExchange,
            type: ExchangeType.Direct,
            durable: true
        );

        await channel.QueueDeclareAsync(
            queue: rabbitSettings.DeadLetterQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        await channel.QueueBindAsync(
            queue: rabbitSettings.DeadLetterQueueName,
            exchange: rabbitSettings.DeadLetterExchange,
            routingKey: rabbitSettings.DeadLetterQueueName
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
            arguments: queueArgs
        );
    }
}
