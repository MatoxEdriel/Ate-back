namespace Ate.Domain.Entities;

/// <summary>
/// Progress of one user on one trophy. It is a cache: it can always be recomputed from the orders.
/// Once unlocked it stays unlocked (like console trophies), even if a streak breaks later.
/// </summary>
public class UserAchievement
{
    public Guid UserId { get; set; }
    public string AchievementCode { get; set; } = string.Empty;
    public AchievementDefinition Achievement { get; set; } = null!;
    public int Progress { get; set; }
    public DateTimeOffset? UnlockedAt { get; set; }
    /// <summary>Null while the unlock toast has not been shown.</summary>
    public DateTimeOffset? SeenAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
