using Ate.Domain.Entities;

namespace Ate.Application.Features.RestaurantBadges;

public record BadgeAnswer(bool Positive, DateTimeOffset RatedAt);

public record BadgeState(bool IsHeld, DateTimeOffset? EarnedAt, DateTimeOffset? LostAt);

public static class RestaurantBadgeCalculator
{
    public static BadgeState Next(
        RestaurantBadgeDefinition definition,
        BadgeState current,
        IReadOnlyCollection<BadgeAnswer> answers,
        DateTimeOffset now)
    {
        if (!current.IsHeld)
        {
            var counted = answers.Where(a => current.LostAt is null || a.RatedAt > current.LostAt).ToList();
            var positive = counted.Count(a => a.Positive);

            return positive >= definition.EarnMinPositive && Ratio(positive, counted.Count) >= definition.EarnMinRatio
                ? current with { IsHeld = true, EarnedAt = now }
                : current;
        }

        var windowStart = now.AddDays(-definition.LoseWindowDays);
        var recent = answers.Where(a => a.RatedAt > current.EarnedAt && a.RatedAt >= windowStart).ToList();
        var negative = recent.Count(a => !a.Positive);

        return negative >= definition.LoseMinNegative && Ratio(negative, recent.Count) >= definition.LoseMinRatio
            ? current with { IsHeld = false, LostAt = now }
            : current;
    }

    private static decimal Ratio(int part, int total) => total == 0 ? 0m : (decimal)part / total;
}
