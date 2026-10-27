using Ate.Application.Common.Constants;
using Ate.Application.Common.Interfaces;
using Ate.Domain.Entities;
using Ate.Infrastructure.Persistence;
using Ate.Infrastructure.Persistence.MongoDb;

namespace Ate.Infrastructure.Services;

public class DishCatalogSyncService : IDishCatalogSyncService, IScopedDependency
{
    private readonly AteDbContext _context;
    private readonly IMongoDbContext _mongoContext;

    public DishCatalogSyncService(AteDbContext context, IMongoDbContext mongoContext)
    {
        _context = context;
        _mongoContext = mongoContext;
    }

    public Task<int> SyncAllAsync(CancellationToken cancellationToken = default)
    {
        var collection = _mongoContext.GetCollection<DishCatalogEntry>(MongoCollections.DishCatalog);
        return DishCatalogWriter.SyncAsync(_context, collection, cancellationToken: cancellationToken);
    }
}
