namespace Ate.Domain.Entities;

public class RestaurantBadgeDefinition
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int EarnMinPositive { get; set; }
    public decimal EarnMinRatio { get; set; }
    public int LoseMinNegative { get; set; }
    public decimal LoseMinRatio { get; set; }
    public int LoseWindowDays { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
