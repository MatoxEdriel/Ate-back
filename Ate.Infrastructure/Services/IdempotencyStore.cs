using Ate.Application.Common.Constants;
using Ate.Application.Common.Interfaces;
using Ate.Domain.Entities;
using MongoDB.Driver;

namespace Ate.Infrastructure.Services;

public class IdempotencyStore : IIdempotencyStore, IScopedDependency
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

    private readonly IMongoCollection<IdempotencyRecord> _records;

    public IdempotencyStore(IMongoDbContext mongoContext)
    {
        _records = mongoContext.GetCollection<IdempotencyRecord>(MongoCollections.IdempotencyKeys);
    }

    public async Task<bool> TryReserveAsync(string id, string requestHash, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        try
        {
            await _records.InsertOneAsync(
                new IdempotencyRecord
                {
                    Id = id,
                    RequestHash = requestHash,
                    Completed = false,
                    CreatedAt = now,
                    ExpiresAt = now.Add(Lifetime).UtcDateTime,
                },
                cancellationToken: cancellationToken);
            return true;
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }

    public async Task<bool> TryTakeOverAsync(string id, DateTimeOffset staleBefore, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var result = await _records.UpdateOneAsync(
            r => r.Id == id && !r.Completed && r.CreatedAt < staleBefore,
            Builders<IdempotencyRecord>.Update
                .Set(r => r.CreatedAt, now)
                .Set(r => r.ExpiresAt, now.Add(Lifetime).UtcDateTime),
            cancellationToken: cancellationToken);
        return result.ModifiedCount == 1;
    }

    public async Task<IdempotencyRecord?> GetAsync(string id, CancellationToken cancellationToken = default) =>
        await _records.Find(r => r.Id == id).FirstOrDefaultAsync(cancellationToken);

    public Task CompleteAsync(string id, int statusCode, string body, CancellationToken cancellationToken = default) =>
        _records.UpdateOneAsync(
            r => r.Id == id,
            Builders<IdempotencyRecord>.Update
                .Set(r => r.Completed, true)
                .Set(r => r.StatusCode, statusCode)
                .Set(r => r.Body, body),
            cancellationToken: cancellationToken);

    public Task ReleaseAsync(string id, CancellationToken cancellationToken = default) =>
        _records.DeleteOneAsync(r => r.Id == id, cancellationToken);
}
