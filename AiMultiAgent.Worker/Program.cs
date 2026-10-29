using AiMultiAgent.Application;
using AiMultiAgent.Application.Interfaces;
using AiMultiAgent.Infrastructure.Data.MongoDB;
using AiMultiAgent.Infrastructure.Rabbit;
using AiMultiAgent.Worker;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

var otelServiceName = builder.Configuration["OpenTelemetry:ServiceName"] ?? "AiMultiAgent.Worker";
var otelEndpoint = builder.Configuration["OpenTelemetry:Otlp:Endpoint"]
                   ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
var otelProtocolRaw = builder.Configuration["OpenTelemetry:Otlp:Protocol"]
                      ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL")
                      ?? "grpc";
var otelProtocol = string.Equals(otelProtocolRaw, "http/protobuf", StringComparison.OrdinalIgnoreCase)
    ? OtlpExportProtocol.HttpProtobuf
    : OtlpExportProtocol.Grpc;

builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(otelServiceName))
    .WithTracing(tracing =>
    {
        tracing
            .AddHttpClientInstrumentation()
            .AddSource("AiMultiAgent.Worker")
            .AddSource("AiMultiAgent.Application")
            .AddOtlpExporter(options =>
            {
                if (!string.IsNullOrWhiteSpace(otelEndpoint))
                    options.Endpoint = new Uri(otelEndpoint);

                options.Protocol = otelProtocol;
            });
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddMeter("AiMultiAgent.Worker")
            .AddMeter("AiMultiAgent.Application")
            .AddOtlpExporter(options =>
            {
                if (!string.IsNullOrWhiteSpace(otelEndpoint))
                    options.Endpoint = new Uri(otelEndpoint);

                options.Protocol = otelProtocol;
            });
    });

var openAiApiKey = builder.Configuration["LLM:OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? string.Empty;
var openAiModel = builder.Configuration["LLM:OpenAI:Model"] ?? "gpt-4.1-mini";

var geminiApiKey = builder.Configuration["LLM:Gemini:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? string.Empty;
var geminiModel = builder.Configuration["LLM:Gemini:Model"] ?? "gemini-2.5-flash";

builder.Services.AddSingleton(new RabbitSettings
{
    HostName = builder.Configuration["Rabbit:HostName"] ?? "rabbitmq",
    QueueName = builder.Configuration["Rabbit:QueueName"] ?? "ai_queue",
    DeadLetterExchange = builder.Configuration["Rabbit:DeadLetterExchange"] ?? "ai_dlx",
    DeadLetterQueueName = builder.Configuration["Rabbit:DeadLetterQueueName"] ?? "ai_queue.dlq",
    MaxRetryCount = int.TryParse(builder.Configuration["Rabbit:MaxRetryCount"], out var maxRetryCount) ? maxRetryCount : 3
});

builder.Services.AddSingleton(new MongoSettings
{
    ConnectionString = builder.Configuration["Mongo:ConnectionString"] ?? "mongodb://mongo:27017",
    DatabaseName = builder.Configuration["Mongo:DatabaseName"] ?? "AiMultiAgent",
    LogsCollectionName = builder.Configuration["Mongo:LogsCollectionName"] ?? "Logs"
});

builder.Services.AddSingleton(new OpenAIAgent(openAiApiKey, openAiModel));
builder.Services.AddSingleton(new GeminiAgent(geminiApiKey, geminiModel));

builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton<IMongoRepository, MongoRepository>();
builder.Services.AddSingleton<IOrchestratorAgent, OrchestratorAgent>();

var host = builder.Build();
host.Run();
