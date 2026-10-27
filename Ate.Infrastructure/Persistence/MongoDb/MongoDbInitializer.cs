using Ate.Application.Common.Constants;
using Ate.Domain.Entities;
using MongoDB.Driver;

namespace Ate.Infrastructure.Persistence.MongoDb;

public static class MongoDbInitializer
{
    /// <summary>
    /// Crea las colecciones e índices si no existen. Es idempotente: se puede ejecutar en cada arranque.
    /// </summary>
    public static async Task EnsureCollectionsAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var existing = await (await database.ListCollectionNamesAsync(cancellationToken: cancellationToken))
            .ToListAsync(cancellationToken);

        if (!existing.Contains(MongoCollections.EmployeeOrders))
        {
            await database.CreateCollectionAsync(MongoCollections.EmployeeOrders, cancellationToken: cancellationToken);
        }

        var orders = database.GetCollection<EmployeeOrder>(MongoCollections.EmployeeOrders);
        var keys = Builders<EmployeeOrder>.IndexKeys;

        await orders.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<EmployeeOrder>(
                keys.Ascending(o => o.EmployeeId).Descending(o => o.OrderedAt),
                new CreateIndexOptions { Name = "ix_employee_orders_employee_ordered_at" }),
            new CreateIndexModel<EmployeeOrder>(
                keys.Ascending(o => o.CompanyId).Descending(o => o.OrderedAt),
                new CreateIndexOptions { Name = "ix_employee_orders_company_ordered_at" }),
            new CreateIndexModel<EmployeeOrder>(
                keys.Ascending(o => o.RestaurantId).Ascending(o => o.ServiceDate).Ascending(o => o.Status),
                new CreateIndexOptions { Name = "ix_employee_orders_restaurant_service_date" }),
            new CreateIndexModel<EmployeeOrder>(
                keys.Ascending(o => o.TicketNumber),
                new CreateIndexOptions { Name = "ux_employee_orders_ticket_number", Unique = true }),
            new CreateIndexModel<EmployeeOrder>(
                keys.Ascending(o => o.ActiveSlot),
                new CreateIndexOptions<EmployeeOrder>
                {
                    Name = "ux_employee_orders_active_slot",
                    Unique = true,
                    PartialFilterExpression = Builders<EmployeeOrder>.Filter.Exists(o => o.ActiveSlot)
                })
        ], cancellationToken);

        // Closed orders must not hold the "one active order per meal" slot (older data kept it on Delivered).
        await orders.UpdateManyAsync(
            Builders<EmployeeOrder>.Filter.And(
                Builders<EmployeeOrder>.Filter.Exists(o => o.ActiveSlot),
                Builders<EmployeeOrder>.Filter.In(o => o.Status, [EmployeeOrderStatus.Delivered, EmployeeOrderStatus.Cancelled])),
            Builders<EmployeeOrder>.Update.Unset(o => o.ActiveSlot),
            cancellationToken: cancellationToken);

        if (!existing.Contains(MongoCollections.DishCatalog))
        {
            await database.CreateCollectionAsync(MongoCollections.DishCatalog, cancellationToken: cancellationToken);
        }

        var dishes = database.GetCollection<DishCatalogEntry>(MongoCollections.DishCatalog);
        var dishKeys = Builders<DishCatalogEntry>.IndexKeys;

        await dishes.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<DishCatalogEntry>(
                dishKeys.Ascending(d => d.RestaurantId).Ascending(d => d.MealType),
                new CreateIndexOptions { Name = "ix_dish_catalog_restaurant_meal_type" }),
            new CreateIndexModel<DishCatalogEntry>(
                dishKeys.Ascending(d => d.Protein),
                new CreateIndexOptions { Name = "ix_dish_catalog_protein" })
        ], cancellationToken);
    }
}
