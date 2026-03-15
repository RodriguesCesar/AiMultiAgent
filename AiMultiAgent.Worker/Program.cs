using AiMultiAgent.Application;
using AiMultiAgent.Application.Interfaces;
using AiMultiAgent.Infrastructure.Data.MongoDB;
using AiMultiAgent.Infrastructure.Rabbit;
using AiMultiAgent.Infrastructure.Rabbit.Infaces;
using AiMultiAgent.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.AddScoped<IMongoRepository, MongoRepository>();
builder.Services.AddScoped<IOrchestratorAgent, OrchestratorAgent>();
var host = builder.Build();
host.Run();
