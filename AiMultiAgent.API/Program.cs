using AiMultiAgent.API.Security;
using AiMultiAgent.Infrastructure.Rabbit;
using AiMultiAgent.Infrastructure.Rabbit.Infaces;
using Microsoft.AspNetCore.Authentication;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var otelServiceName = builder.Configuration["OpenTelemetry:ServiceName"] ?? "AiMultiAgent.API";
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
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource("AiMultiAgent.API")
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
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddMeter("AiMultiAgent.API")
            .AddMeter("AiMultiAgent.Application")
            .AddOtlpExporter(options =>
            {
                if (!string.IsNullOrWhiteSpace(otelEndpoint))
                    options.Endpoint = new Uri(otelEndpoint);

                options.Protocol = otelProtocol;
            });
    });

builder.Services.AddSingleton(new RabbitSettings
{
    HostName = builder.Configuration["Rabbit:HostName"] ?? "rabbitmq",
    QueueName = builder.Configuration["Rabbit:QueueName"] ?? "ai_queue",
    DeadLetterExchange = builder.Configuration["Rabbit:DeadLetterExchange"] ?? "ai_dlx",
    DeadLetterQueueName = builder.Configuration["Rabbit:DeadLetterQueueName"] ?? "ai_queue.dlq",
    MaxRetryCount = int.TryParse(builder.Configuration["Rabbit:MaxRetryCount"], out var maxRetryCount) ? maxRetryCount : 3
});

builder.Services.AddScoped<IRabbitPublisher, RabbitPublisher>();

builder.Services
    .AddAuthentication("ApiKey")
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>("ApiKey", _ => { });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
