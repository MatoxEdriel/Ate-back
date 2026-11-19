namespace Ate.Domain.Entities;

public class RestaurantBadge
{
    public Guid RestaurantId { get; set; }
    public string BadgeCode { get; set; } = string.Empty;
    public RestaurantBadgeDefinition Badge { get; set; } = null!;
    public bool IsHeld { get; set; }
    public DateTimeOffset? EarnedAt { get; set; }
    public DateTimeOffset? LostAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
