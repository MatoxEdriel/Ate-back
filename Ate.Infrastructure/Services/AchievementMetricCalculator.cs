using System.Globalization;
using Ate.Domain.Entities;
using Ate.Domain.Enums;

namespace Ate.Infrastructure.Services;

/// <summary>
/// Pure functions: the value of every metric from a user's order history.
/// Deterministic, so evaluating twice (or retroactively for a new trophy) gives the same result.
/// </summary>
public static class AchievementMetricCalculator
{
    public static IReadOnlyDictionary<string, int> Compute(IReadOnlyCollection<EmployeeOrder> orders)
    {
        var delivered = orders.Where(o => o.Status == EmployeeOrderStatus.Delivered).ToList();

        return new Dictionary<string, int>
        {
            [AchievementMetrics.DeliveredOrders] = delivered.Count,
            [AchievementMetrics.BreakfastOrders] = delivered.Count(o => o.MealType == MealType.Breakfast),
            [AchievementMetrics.DistinctRestaurants] = delivered.Select(o => o.RestaurantId).Distinct().Count(),
            [AchievementMetrics.DistinctProteins] = delivered
                .SelectMany(o => o.Items)
                .Select(i => i.Protein?.Trim().ToLowerInvariant())
                .Where(p => !string.IsNullOrEmpty(p))
                .Distinct()
                .Count(),
            [AchievementMetrics.WorkdayStreak] = BestWorkdayStreak(delivered),
            [AchievementMetrics.NoCancelStreak] = BestNoCancelStreak(orders),
        };
    }

    /// <summary>Longest run of consecutive workdays with a delivered order. Saturday and Sunday are skipped.</summary>
    public static int BestWorkdayStreak(IEnumerable<EmployeeOrder> delivered)
    {
        var days = delivered
            .Select(o => DateOnly.TryParseExact(o.ServiceDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : (DateOnly?)null)
            .Where(d => d.HasValue && IsWorkday(d.Value))
            .Select(d => d!.Value)
            .Distinct()
            .Order()
            .ToList();

        var best = 0;
        var current = 0;
        DateOnly? previous = null;
        foreach (var day in days)
        {
            current = previous.HasValue && day == NextWorkday(previous.Value) ? current + 1 : 1;
            best = Math.Max(best, current);
            previous = day;
        }

        return best;
    }

    /// <summary>
    /// Longest run of delivered orders without a cancellation made by the employee.
    /// A cancellation by the restaurant is not the employee's fault, so it does not break the run.
    /// </summary>
    public static int BestNoCancelStreak(IEnumerable<EmployeeOrder> orders)
    {
        var best = 0;
        var current = 0;
        foreach (var order in orders.OrderBy(o => o.OrderedAt))
        {
            if (order.Status == EmployeeOrderStatus.Delivered)
            {
                current++;
                best = Math.Max(best, current);
            }
            else if (order.Status == EmployeeOrderStatus.Cancelled && CancelledByEmployee(order))
            {
                current = 0;
            }
        }

        return best;
    }

    private static bool CancelledByEmployee(EmployeeOrder order)
    {
        var cancellation = order.StatusHistory.LastOrDefault(c => c.Status == EmployeeOrderStatus.Cancelled);
        return cancellation?.ChangedByUserId == order.UserId;
    }

    private static bool IsWorkday(DateOnly day) => day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);

    private static DateOnly NextWorkday(DateOnly day)
    {
        var next = day.AddDays(1);
        while (!IsWorkday(next))
        {
            next = next.AddDays(1);
        }

        return next;
    }
}
