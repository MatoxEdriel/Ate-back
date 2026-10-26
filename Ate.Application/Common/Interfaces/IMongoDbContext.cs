using MongoDB.Driver;

namespace Ate.Application.Common.Interfaces;

public interface IMongoDbContext
{
    IMongoClient Client { get; }
    IMongoDatabase Database { get; }
    IMongoCollection<TDocument> GetCollection<TDocument>(string? collectionName = null);
}
