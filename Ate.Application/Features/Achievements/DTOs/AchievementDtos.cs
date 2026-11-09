namespace Ate.Application.Features.Achievements.DTOs;

/// <summary>One trophy as the employee sees it. A hidden locked trophy comes as "???".</summary>
public record AchievementDto(
    string Code,
    string Name,
    string Description,
    string Tier,
    string Icon,
    bool IsHidden,
    int Progress,
    int Target,
    DateTimeOffset? UnlockedAt);

public record AchievementTierSummaryDto(string Tier, int Unlocked, int Total);

/// <summary>Trophy room: every trophy plus the global completion (e.g. 4/10 · 40%).</summary>
public record AchievementsOverviewDto(
    int Unlocked,
    int Total,
    int Percent,
    IReadOnlyList<AchievementTierSummaryDto> Tiers,
    IReadOnlyList<AchievementDto> Achievements);

public record MarkAchievementsSeenRequest(IReadOnlyList<string> Codes);
