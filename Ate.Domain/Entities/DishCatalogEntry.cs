using Ate.Domain.Enums;

namespace Ate.Domain.Entities;

/// <summary>
/// Copia de solo lectura de un plato en MongoDB (colección dish_catalog) para análisis de datos.
/// La fuente de verdad es restaurant_dishes en Postgres; este documento se reescribe cada vez que cambia el plato
/// o su restaurante. El _id es el mismo Id del plato, así se cruza con employee_orders.items.dishId.
/// Los platos eliminados se conservan con IsDeleted = true porque las órdenes antiguas los siguen referenciando.
/// </summary>
public class DishCatalogEntry
{
    public Guid Id { get; set; }

    public Guid RestaurantId { get; set; }
    public string RestaurantCode { get; set; } = string.Empty;
    public string RestaurantName { get; set; } = string.Empty;
    public string Cuisine { get; set; } = string.Empty;
    public bool RestaurantIsActive { get; set; }

    public MealType MealType { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Protein { get; set; } = string.Empty;
    public bool HasRice { get; set; }
    public string DefaultDrink { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public bool IsAvailable { get; set; }
    public int SortOrder { get; set; }
    public bool IsDeleted { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public DateTimeOffset SyncedAt { get; set; }
}
