namespace AiMultiAgent.Infrastructure.Rabbit;

public class RabbitSettings
{
    public string HostName { get; set; } = "rabbitmq";
    public string QueueName { get; set; } = "ai_queue";
    public string DeadLetterExchange { get; set; } = "ai_dlx";
    public string DeadLetterQueueName { get; set; } = "ai_queue.dlq";
    public int MaxRetryCount { get; set; } = 3;
}
