using System.Globalization;
using Ate.Domain.Entities;
using Ate.Domain.Enums;

namespace Ate.Application.Features.Payroll;

/// <summary>
/// Reglas del cálculo de nómina. Funciones puras: qué pedidos se cobran, desde qué día y cuánto suman.
/// Los períodos se miden por día de servicio (hora de negocio) y son inclusivos en ambos extremos.
/// </summary>
public static class SettlementRules
{
    public const string DateFormat = "yyyy-MM-dd";

    /// <summary>Solo se cobran los pedidos confirmados, listos o entregados.</summary>
    public static bool IsChargeable(string status) =>
        status is EmployeeOrderStatus.Confirmed or EmployeeOrderStatus.Ready or EmployeeOrderStatus.Delivered;

    /// <summary>
    /// Inicio del corte mensual. Si el colaborador ya tuvo un corte que terminó dentro del mes
    /// (por ejemplo una liquidación), el mensual empieza el día siguiente para no cobrar dos veces.
    /// </summary>
    public static DateOnly MonthlyStart(DateOnly monthStart, DateOnly? lastSettledEnd) =>
        StartAfter(monthStart, lastSettledEnd);

    /// <summary>Inicio del corte por salida: el primer día del mes de salida, o el día siguiente al último corte.</summary>
    public static DateOnly TerminationStart(DateOnly exitDate, DateOnly? lastSettledEnd) =>
        StartAfter(new DateOnly(exitDate.Year, exitDate.Month, 1), lastSettledEnd);

    /// <summary>
    /// Arma el corte con los pedidos cobrables entre <paramref name="start"/> y <paramref name="end"/>.
    /// Devuelve null cuando no hay nada que cobrar.
    /// </summary>
    public static PayrollSettlement? Build(
        SettlementType type,
        DateOnly start,
        DateOnly end,
        IReadOnlyList<EmployeeOrder> orders,
        DateTimeOffset generatedAt,
        Guid? generatedBy)
    {
        var from = Format(start);
        var to = Format(end);

        var charged = orders
            .Where(o => IsChargeable(o.Status)
                && string.CompareOrdinal(o.ServiceDate, from) >= 0
                && string.CompareOrdinal(o.ServiceDate, to) <= 0)
            .OrderBy(o => o.ServiceDate, StringComparer.Ordinal)
            .ThenBy(o => o.OrderedAt)
            .ToList();

        var total = charged.Sum(o => o.TotalAmount);
        if (charged.Count == 0 || total <= 0)
        {
            return null;
        }

        var latest = charged.MaxBy(o => o.OrderedAt)!;

        return new PayrollSettlement
        {
            Type = type,
            CompanyId = latest.CompanyId,
            EmployeeId = latest.EmployeeId,
            UserId = latest.UserId,
            EmployeeName = latest.EmployeeName,
            EmployeeDocument = latest.EmployeeDocument,
            PeriodStart = from,
            PeriodEnd = to,
            TotalAmount = total,
            Currency = latest.Currency,
            OrderCount = charged.Count,
            Lines = charged.Select(o => new PayrollSettlementLine
            {
                OrderId = o.Id,
                TicketNumber = o.TicketNumber,
                ServiceDate = o.ServiceDate,
                RestaurantName = o.RestaurantName,
                MealType = o.MealType,
                Items = o.Items,
                Amount = o.TotalAmount,
            }).ToList(),
            GeneratedAt = generatedAt,
            GeneratedByUserId = generatedBy,
        };
    }

    public static string Format(DateOnly date) => date.ToString(DateFormat, CultureInfo.InvariantCulture);

    public static DateOnly Parse(string date) => DateOnly.ParseExact(date, DateFormat, CultureInfo.InvariantCulture);

    private static DateOnly StartAfter(DateOnly periodStart, DateOnly? lastSettledEnd) =>
        lastSettledEnd.HasValue && lastSettledEnd.Value >= periodStart
            ? lastSettledEnd.Value.AddDays(1)
            : periodStart;
}
