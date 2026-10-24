using Ate.Application.Common.Interfaces;
using MongoDB.Driver;

namespace Ate.Infrastructure.Persistence.MongoDb;

public class MongoDbContext : IMongoDbContext
{
    private readonly IMongoDatabase _database;
    private readonly IMongoClient _client;

    public MongoDbContext(IMongoClient client, string databaseName)
    {
        _client = client;
        _database = client.GetDatabase(databaseName);
    }

    public IMongoClient Client => _client;

    public IMongoDatabase Database => _database;

    public IMongoCollection<TDocument> GetCollection<TDocument>(string? collectionName = null)
    {
        var resolvedName = string.IsNullOrWhiteSpace(collectionName)
            ? typeof(TDocument).Name.ToLowerInvariant()
            : collectionName;

        return _database.GetCollection<TDocument>(resolvedName);
    }
}
