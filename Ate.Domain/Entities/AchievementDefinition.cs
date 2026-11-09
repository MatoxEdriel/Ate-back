namespace Ate.Domain.Entities;

/// <summary>
/// One trophy of the catalog. It is data: a new trophy over an existing <see cref="Metric"/> is just a row.
/// </summary>
public class AchievementDefinition
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    /// <summary>bronze | silver | gold | platinum (see <see cref="AchievementTiers"/>).</summary>
    public string Tier { get; set; } = AchievementTiers.Bronze;
    public string Icon { get; set; } = string.Empty;
    /// <summary>What is measured (see <see cref="AchievementMetrics"/>).</summary>
    public string Metric { get; set; } = string.Empty;
    /// <summary>Value of the metric that unlocks it. Ignored by <see cref="AchievementMetrics.AllTrophies"/>.</summary>
    public int Threshold { get; set; }
    /// <summary>Shown as "???" until it is unlocked.</summary>
    public bool IsHidden { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public static class AchievementTiers
{
    public const string Bronze = "bronze";
    public const string Silver = "silver";
    public const string Gold = "gold";
    public const string Platinum = "platinum";
}

/// <summary>
/// Metrics computed from the employee's order history. Only delivered orders count, so ordering
/// and cancelling never adds progress. A new metric needs code in AchievementService.
/// </summary>
public static class AchievementMetrics
{
    public const string DeliveredOrders = "delivered_orders";
    public const string BreakfastOrders = "breakfast_orders";
    public const string DistinctRestaurants = "distinct_restaurants";
    public const string DistinctProteins = "distinct_proteins";
    /// <summary>Best run of consecutive workdays (Mon–Fri) with a delivered order; weekends do not break it.</summary>
    public const string WorkdayStreak = "workday_streak";
    /// <summary>Best run of delivered orders without one cancelled by the employee.</summary>
    public const string NoCancelStreak = "no_cancel_streak";
    /// <summary>Platinum: unlocked when every other active trophy is.</summary>
    public const string AllTrophies = "all_trophies";
}
