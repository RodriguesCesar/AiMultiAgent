namespace AiMultiAgent.Infrastructure.Rabbit;

public class RabbitSettings
{
    public string HostName { get; set; } = "rabbitmq";
    public string QueueName { get; set; } = "ai_queue";
}
