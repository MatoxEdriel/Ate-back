using Ate.Application.Features.RestaurantBadges;
using Ate.Domain.Entities;

namespace Ate.Tests.RestaurantBadges;

public class RestaurantBadgeCalculatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

    private static readonly RestaurantBadgeDefinition Punctual = new()
    {
        Code = "punctual",
        Category = "punctual",
        EarnMinPositive = 10,
        EarnMinRatio = 0.80m,
        LoseMinNegative = 10,
        LoseMinRatio = 0.20m,
        LoseWindowDays = 90,
    };

    private static readonly BadgeState NotHeld = new(false, null, null);

    private static BadgeState HeldSince(int daysAgo) => new(true, Now.AddDays(-daysAgo), null);

    private static List<BadgeAnswer> Answers(int positive, int negative, int daysAgo = 1)
    {
        var at = Now.AddDays(-daysAgo);
        return Enumerable.Repeat(new BadgeAnswer(true, at), positive)
            .Concat(Enumerable.Repeat(new BadgeAnswer(false, at), negative))
            .ToList();
    }

    [Fact]
    public void DoesNotEarn_WhenRatioBelow80()
    {
        var next = RestaurantBadgeCalculator.Next(Punctual, NotHeld, Answers(10, 9), Now);

        Assert.False(next.IsHeld);
    }

    [Fact]
    public void Earns_WhenTenPositivesAndRatioAbove80()
    {
        var next = RestaurantBadgeCalculator.Next(Punctual, NotHeld, Answers(12, 2), Now);

        Assert.True(next.IsHeld);
        Assert.Equal(Now, next.EarnedAt);
    }

    [Fact]
    public void Earns_AtExactly80Percent()
    {
        var next = RestaurantBadgeCalculator.Next(Punctual, NotHeld, Answers(12, 3), Now);

        Assert.True(next.IsHeld);
    }

    [Fact]
    public void DoesNotEarn_WithFewerThanTenPositives()
    {
        var next = RestaurantBadgeCalculator.Next(Punctual, NotHeld, Answers(9, 0), Now);

        Assert.False(next.IsHeld);
    }

    [Fact]
    public void Keeps_WhenComplaintsAreSmallShareOfVolume()
    {
        var next = RestaurantBadgeCalculator.Next(Punctual, HeldSince(200), Answers(2000, 10), Now);

        Assert.True(next.IsHeld);
    }

    [Fact]
    public void Loses_WhenTenComplaintsAndRatioAbove20()
    {
        var next = RestaurantBadgeCalculator.Next(Punctual, HeldSince(200), Answers(30, 12), Now);

        Assert.False(next.IsHeld);
        Assert.Equal(Now, next.LostAt);
    }

    [Fact]
    public void Keeps_WhenComplaintsAreOlderThanWindow()
    {
        var next = RestaurantBadgeCalculator.Next(Punctual, HeldSince(200), Answers(0, 12, daysAgo: 100), Now);

        Assert.True(next.IsHeld);
    }

    [Fact]
    public void Keeps_WhenComplaintsPredateEarning()
    {
        var next = RestaurantBadgeCalculator.Next(Punctual, HeldSince(5), Answers(0, 12, daysAgo: 10), Now);

        Assert.True(next.IsHeld);
    }

    [Fact]
    public void DoesNotReEarn_WithPositivesBeforeLoss()
    {
        var lost = new BadgeState(false, Now.AddDays(-60), Now.AddDays(-5));

        var next = RestaurantBadgeCalculator.Next(Punctual, lost, Answers(20, 0, daysAgo: 10), Now);

        Assert.False(next.IsHeld);
    }

    [Fact]
    public void ReEarns_WithPositivesAfterLoss()
    {
        var lostAt = Now.AddDays(-5);
        var lost = new BadgeState(false, Now.AddDays(-60), lostAt);

        var next = RestaurantBadgeCalculator.Next(Punctual, lost, Answers(10, 0, daysAgo: 1), Now);

        Assert.True(next.IsHeld);
        Assert.Equal(lostAt, next.LostAt);
    }
}
