using AiMultiAgent.Infrastructure.Model;
using MongoDB.Driver;

namespace AiMultiAgent.Infrastructure.Data.MongoDB;

public class MongoRepository : IMongoRepository
{
    private readonly IMongoCollection<AiLogs> _logsCollection;

    public MongoRepository(MongoSettings settings)
    {
        var client = new MongoClient(settings.ConnectionString);
        var database = client.GetDatabase(settings.DatabaseName);
        _logsCollection = database.GetCollection<AiLogs>(settings.LogsCollectionName);
    }

    public async Task SaveAsync(string message, string response)
    {
        await _logsCollection.InsertOneAsync(new AiLogs(message, response, DateTime.UtcNow));
    }
}
