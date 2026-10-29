using AiMultiAgent.API.Security;
using AiMultiAgent.Infrastructure.Rabbit;
using AiMultiAgent.Infrastructure.Rabbit.Infaces;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

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
