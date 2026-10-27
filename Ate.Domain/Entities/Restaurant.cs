using Ate.Domain.Common;
using Ate.Domain.Enums;

namespace Ate.Domain.Entities;

public class Restaurant : BaseAuditableEntity
{
    public Guid? MerchantUserId { get; set; }
    public User? MerchantUser { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Cuisine { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string BrandColor { get; set; } = "#B5532E";
    public string? LogoUrl { get; set; }
    public string? Icon { get; set; }
    public int ReadyMinutes { get; set; } = 15;
    public TimeOnly? BreakfastCutoff { get; set; }
    public TimeOnly? LunchCutoff { get; set; }
    public TimeOnly? DinnerCutoff { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<RestaurantDish> Dishes { get; set; } = new List<RestaurantDish>();
    public ICollection<CompanyRestaurantAgreement> Agreements { get; set; } = new List<CompanyRestaurantAgreement>();

    public TimeOnly? GetCutoff(MealType mealType) => mealType switch
    {
        MealType.Breakfast => BreakfastCutoff,
        MealType.Lunch => LunchCutoff,
        MealType.Dinner => DinnerCutoff,
        _ => null
    };
}
