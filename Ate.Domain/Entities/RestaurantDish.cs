using Ate.Domain.Common;
using Ate.Domain.Enums;

namespace Ate.Domain.Entities;

public class RestaurantDish : BaseAuditableEntity
{
    public Guid RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = null!;
    public MealType MealType { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Protein { get; set; } = string.Empty;
    public bool HasRice { get; set; }
    public string DefaultDrink { get; set; } = "Jugo";
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; } = true;
    public int SortOrder { get; set; }
}
