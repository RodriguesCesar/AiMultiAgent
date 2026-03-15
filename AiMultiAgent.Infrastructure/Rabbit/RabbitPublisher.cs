using AiMultiAgent.Infrastructure.Rabbit.Infaces;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace AiMultiAgent.Infrastructure.Rabbit;

public class RabbitPublisher : IRabbitPublisher
{

    public async Task PublishAsync(string message)
    {
        var factory = new RabbitMQ.Client.ConnectionFactory() { HostName = "rabbitmq" };
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        await channel.QueueDeclareAsync(queue: "ai_queue",
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

        var body = Encoding.UTF8.GetBytes(message);
        await channel.BasicPublishAsync(
                exchange: "",
                routingKey: "ai_queue",
                body: body
            );
    }
}
