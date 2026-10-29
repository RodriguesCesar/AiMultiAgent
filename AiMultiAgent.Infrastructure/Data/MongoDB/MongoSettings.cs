namespace AiMultiAgent.Infrastructure.Data.MongoDB;

public class MongoSettings
{
    public string ConnectionString { get; set; } = "mongodb://mongo:27017";
    public string DatabaseName { get; set; } = "AiMultiAgent";
    public string LogsCollectionName { get; set; } = "Logs";
}
