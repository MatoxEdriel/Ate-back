using Ate.Domain.Entities;

namespace Ate.Application.Features.EmployeeOrders;

public static class PendingRatingSelector
{
    public static readonly TimeSpan RatingWindow = TimeSpan.FromDays(7);

    public static EmployeeOrder? Pick(IEnumerable<EmployeeOrder> orders, DateTimeOffset now)
    {
        var latest = orders
            .Where(o => o.Status == EmployeeOrderStatus.Delivered && DeliveredAt(o).HasValue)
            .MaxBy(o => DeliveredAt(o));

        return latest != null && latest.Rating == null && IsWithinWindow(latest, now) ? latest : null;
    }

    public static bool IsWithinWindow(EmployeeOrder order, DateTimeOffset now) =>
        order.Status == EmployeeOrderStatus.Delivered
        && DeliveredAt(order) is { } deliveredAt
        && now - deliveredAt <= RatingWindow;

    public static DateTimeOffset? DeliveredAt(EmployeeOrder order) =>
        order.StatusHistory.LastOrDefault(c => c.Status == EmployeeOrderStatus.Delivered)?.ChangedAt;
}
