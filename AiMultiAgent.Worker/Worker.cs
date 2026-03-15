using AiMultiAgent.Application;
using AiMultiAgent.Application.Interfaces;
using AiMultiAgent.Infrastructure.Data.MongoDB;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace AiMultiAgent.Worker;

public class Worker(ILogger<Worker> logger, IMongoRepository mongoRepository, IOrchestratorAgent orchestratorAgent) : BackgroundService
{

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        //Configurando a conexão com o servidor do RabbitMQ
        var factory = new ConnectionFactory { HostName = "rabbitmq" };

        //Conectando com o RabbitMQ
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        //Criando a fila
        await channel.QueueDeclareAsync(
                queue: "ai_queue",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

        //Consumer -> objeto que irá ler as mensagens da fila
        var consumer = new AsyncEventingBasicConsumer(channel);

        //Programando a leitura da fila
        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                //capturando a mensagem
                var message = Encoding.UTF8.GetString(ea.Body.ToArray());

                //log
                logger.LogInformation("Mensagem lida da fila com sucesso: {Message}", message);

                //Enviar para o Orquestrador
                var response = await orchestratorAgent.ProcessAsync(message);
                //Salvar a resposta no MongoDB
                await mongoRepository.SaveAsync(message, response);

            }
            catch (Exception e)
            {
                logger.LogError(e, "Erro ao processar mensagem: {ErrorMessage}", e.Message);
            }

        };

        //Executar o consumidor
        await channel.BasicConsumeAsync(
                queue: "ai_queue",
                autoAck: true,
                consumer: consumer
            );

        logger.LogInformation("Worker aguardando mensagens...");

        //Manter o worker lendo a fila permanentemente
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
