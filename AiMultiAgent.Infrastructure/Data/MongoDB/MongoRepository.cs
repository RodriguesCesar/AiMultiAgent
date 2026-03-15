
using AiMultiAgent.Infrastructure.Model;
using MongoDB.Driver;

namespace AiMultiAgent.Infrastructure.Data.MongoDB;

public class MongoRepository: IMongoRepository
{
    private readonly IMongoDatabase _database;
    private readonly IMongoCollection<AiLogs> _logsCollection;
    public MongoRepository()
    {
        var client = new MongoClient("mongodb://mongo:27017");
        _database = client.GetDatabase("AiMultiAgent");
        _logsCollection = _database.GetCollection<AiLogs>("Logs");
    }

    public async Task SaveAsync(string message, string response)
    {
        await _logsCollection.InsertOneAsync(new AiLogs(message, response, DateTime.UtcNow));
    }
}
