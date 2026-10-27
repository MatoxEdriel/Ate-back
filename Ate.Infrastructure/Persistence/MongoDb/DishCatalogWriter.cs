using Ate.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

namespace Ate.Infrastructure.Persistence.MongoDb;

/// <summary>
/// Copia platos de Postgres a la colección dish_catalog. Lo usan el interceptor (cambios puntuales)
/// y la sincronización completa del arranque.
/// </summary>
internal static class DishCatalogWriter
{
    /// <summary>
    /// Lee de Postgres los platos indicados (incluidos los eliminados) y los reemplaza en Mongo.
    /// Sin filtros sincroniza todo el catálogo.
    /// </summary>
    public static async Task<int> SyncAsync(
        DbContext context,
        IMongoCollection<DishCatalogEntry> collection,
        IReadOnlyCollection<Guid>? dishIds = null,
        IReadOnlyCollection<Guid>? restaurantIds = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.Set<RestaurantDish>()
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(d => d.Restaurant)
            .AsQueryable();

        if (dishIds != null || restaurantIds != null)
        {
            var dishFilter = dishIds ?? [];
            var restaurantFilter = restaurantIds ?? [];
            query = query.Where(d => dishFilter.Contains(d.Id) || restaurantFilter.Contains(d.RestaurantId));
        }

        var dishes = await query.ToListAsync(cancellationToken);
        if (dishes.Count == 0)
        {
            return 0;
        }

        var syncedAt = DateTimeOffset.UtcNow;
        var writes = dishes
            .Select(d => new ReplaceOneModel<DishCatalogEntry>(
                Builders<DishCatalogEntry>.Filter.Eq(e => e.Id, d.Id),
                ToEntry(d, syncedAt))
            { IsUpsert = true })
            .ToList();

        await collection.BulkWriteAsync(writes, new BulkWriteOptions { IsOrdered = false }, cancellationToken);
        return writes.Count;
    }

    private static DishCatalogEntry ToEntry(RestaurantDish dish, DateTimeOffset syncedAt) => new()
    {
        Id = dish.Id,
        RestaurantId = dish.RestaurantId,
        RestaurantCode = dish.Restaurant.Code,
        RestaurantName = dish.Restaurant.Name,
        Cuisine = dish.Restaurant.Cuisine,
        RestaurantIsActive = dish.Restaurant.IsActive && !dish.Restaurant.IsDeleted,
        MealType = dish.MealType,
        Name = dish.Name,
        Description = dish.Description,
        Protein = dish.Protein,
        HasRice = dish.HasRice,
        DefaultDrink = dish.DefaultDrink,
        Price = dish.Price,
        IsAvailable = dish.IsAvailable,
        SortOrder = dish.SortOrder,
        IsDeleted = dish.IsDeleted,
        CreatedAt = dish.CreatedAt,
        UpdatedAt = dish.UpdatedAt,
        DeletedAt = dish.DeletedAt,
        SyncedAt = syncedAt
    };
}
