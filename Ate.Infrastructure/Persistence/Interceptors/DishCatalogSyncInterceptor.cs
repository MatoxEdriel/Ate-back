using Ate.Application.Common.Constants;
using Ate.Application.Common.Interfaces;
using Ate.Domain.Entities;
using Ate.Infrastructure.Persistence.MongoDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Ate.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Después de cada SaveChanges que toque platos o restaurantes, replica esos platos en dish_catalog (MongoDB).
/// Si Mongo falla no se revierte Postgres: se registra el error y la sincronización completa del arranque lo corrige.
/// </summary>
public class DishCatalogSyncInterceptor : SaveChangesInterceptor
{
    private readonly IMongoDbContext? _mongoContext;
    private readonly ILogger<DishCatalogSyncInterceptor> _logger;

    private readonly HashSet<Guid> _pendingDishIds = new();
    private readonly HashSet<Guid> _pendingRestaurantIds = new();

    public DishCatalogSyncInterceptor(ILogger<DishCatalogSyncInterceptor> logger, IMongoDbContext? mongoContext = null)
    {
        _logger = logger;
        _mongoContext = mongoContext;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (_mongoContext != null && eventData.Context != null)
        {
            CapturePendingChanges(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (_mongoContext != null && eventData.Context != null && (_pendingDishIds.Count > 0 || _pendingRestaurantIds.Count > 0))
        {
            var dishIds = _pendingDishIds.ToList();
            var restaurantIds = _pendingRestaurantIds.ToList();
            ClearPending();

            try
            {
                var collection = _mongoContext.GetCollection<DishCatalogEntry>(MongoCollections.DishCatalog);
                await DishCatalogWriter.SyncAsync(eventData.Context, collection, dishIds, restaurantIds, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not sync {DishCount} dishes / {RestaurantCount} restaurants to the MongoDB dish catalog.",
                    dishIds.Count, restaurantIds.Count);
            }
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        ClearPending();
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private void CapturePendingChanges(DbContext context)
    {
        foreach (var entry in context.ChangeTracker.Entries<RestaurantDish>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            {
                _pendingDishIds.Add(entry.Entity.Id);
            }
        }

        // El nombre, cocina o estado del restaurante está copiado en cada plato.
        foreach (var entry in context.ChangeTracker.Entries<Restaurant>())
        {
            if (entry.State is EntityState.Modified or EntityState.Deleted)
            {
                _pendingRestaurantIds.Add(entry.Entity.Id);
            }
        }
    }

    private void ClearPending()
    {
        _pendingDishIds.Clear();
        _pendingRestaurantIds.Clear();
    }
}
