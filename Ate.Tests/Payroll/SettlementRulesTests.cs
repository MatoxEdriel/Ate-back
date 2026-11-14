using Ate.Application.Features.Payroll;
using Ate.Domain.Entities;
using Ate.Domain.Enums;

namespace Ate.Tests.Payroll;

/// <summary>
/// Cálculo del total a descontar por nómina: qué pedidos cuentan, en qué período y cuánto suman.
/// </summary>
public class SettlementRulesTests
{
    private static readonly Guid EmployeeId = Guid.NewGuid();
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTimeOffset GeneratedAt = new(2026, 12, 1, 6, 0, 0, TimeSpan.Zero);

    private static EmployeeOrder Order(string serviceDate, string status, decimal amount) => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        TicketNumber = $"T-{serviceDate}-{amount}",
        EmployeeId = EmployeeId,
        CompanyId = CompanyId,
        UserId = UserId,
        EmployeeName = "Carlos Mendoza",
        EmployeeDocument = "0943676156",
        RestaurantName = "La Sazón",
        MealType = MealType.Lunch,
        ServiceDate = serviceDate,
        Status = status,
        TotalAmount = amount,
        OrderedAt = DateTimeOffset.Parse($"{serviceDate}T12:00:00-05:00"),
    };

    [Theory]
    [InlineData(EmployeeOrderStatus.Confirmed, true)]
    [InlineData(EmployeeOrderStatus.Ready, true)]
    [InlineData(EmployeeOrderStatus.Delivered, true)]
    [InlineData(EmployeeOrderStatus.Pending, false)]
    [InlineData(EmployeeOrderStatus.Cancelled, false)]
    public void IsChargeable_OnlyConfirmedReadyDelivered(string status, bool expected)
        => Assert.Equal(expected, SettlementRules.IsChargeable(status));

    [Fact]
    public void MonthlyStart_NoPreviousSettlement_IsMonthStart()
        => Assert.Equal(new DateOnly(2026, 11, 1), SettlementRules.MonthlyStart(new DateOnly(2026, 11, 1), null));

    [Fact]
    public void MonthlyStart_AfterTerminationOnThe10th_StartsOnThe11th()
        => Assert.Equal(new DateOnly(2026, 11, 11), SettlementRules.MonthlyStart(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 10)));

    [Fact]
    public void MonthlyStart_SettlementFromPreviousMonth_IsIgnored()
        => Assert.Equal(new DateOnly(2026, 11, 1), SettlementRules.MonthlyStart(new DateOnly(2026, 11, 1), new DateOnly(2026, 10, 31)));

    [Fact]
    public void TerminationStart_FirstDayOfExitMonth()
        => Assert.Equal(new DateOnly(2026, 11, 1), SettlementRules.TerminationStart(new DateOnly(2026, 11, 20), new DateOnly(2026, 10, 31)));

    [Fact]
    public void TerminationStart_AfterSettlementInsideExitMonth_StartsNextDay()
        => Assert.Equal(new DateOnly(2026, 11, 16), SettlementRules.TerminationStart(new DateOnly(2026, 11, 20), new DateOnly(2026, 11, 15)));

    [Fact]
    public void Build_CountsOnlyChargeableOrdersInsidePeriod()
    {
        var orders = new[]
        {
            Order("2026-10-31", EmployeeOrderStatus.Delivered, 5.00m),
            Order("2026-11-01", EmployeeOrderStatus.Delivered, 3.50m),
            Order("2026-11-15", EmployeeOrderStatus.Cancelled, 4.00m),
            Order("2026-11-30", EmployeeOrderStatus.Confirmed, 2.25m),
            Order("2026-12-01", EmployeeOrderStatus.Delivered, 9.00m),
        };

        var settlement = SettlementRules.Build(
            SettlementType.Monthly, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30), orders, GeneratedAt, null);

        Assert.NotNull(settlement);
        Assert.Equal(5.75m, settlement.TotalAmount);
        Assert.Equal(2, settlement.OrderCount);
        Assert.Equal("2026-11-01", settlement.PeriodStart);
        Assert.Equal("2026-11-30", settlement.PeriodEnd);
        Assert.Equal(SettlementType.Monthly, settlement.Type);
        Assert.Equal(new[] { "2026-11-01", "2026-11-30" }, settlement.Lines.Select(l => l.ServiceDate));
        Assert.Equal(EmployeeId, settlement.EmployeeId);
        Assert.Equal(CompanyId, settlement.CompanyId);
        Assert.Equal("0943676156", settlement.EmployeeDocument);
    }

    [Fact]
    public void Build_AfterTerminationOnThe10th_ChargesOnlyFromThe11th()
    {
        var orders = new[]
        {
            Order("2026-11-05", EmployeeOrderStatus.Delivered, 4.00m),
            Order("2026-11-20", EmployeeOrderStatus.Delivered, 6.00m),
        };
        var start = SettlementRules.MonthlyStart(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 10));

        var settlement = SettlementRules.Build(
            SettlementType.Monthly, start, new DateOnly(2026, 11, 30), orders, GeneratedAt, null);

        Assert.NotNull(settlement);
        Assert.Equal(6.00m, settlement.TotalAmount);
        Assert.Equal(1, settlement.OrderCount);
        Assert.Equal("2026-11-11", settlement.PeriodStart);
    }

    [Fact]
    public void Build_SumsDecimalAmountsExactly()
    {
        var orders = new[]
        {
            Order("2026-11-03", EmployeeOrderStatus.Delivered, 2.10m),
            Order("2026-11-04", EmployeeOrderStatus.Delivered, 3.35m),
            Order("2026-11-05", EmployeeOrderStatus.Delivered, 1.05m),
        };

        var settlement = SettlementRules.Build(
            SettlementType.Monthly, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30), orders, GeneratedAt, null);

        Assert.NotNull(settlement);
        Assert.Equal(6.50m, settlement.TotalAmount);
    }

    [Fact]
    public void Build_NothingChargeable_ReturnsNull()
    {
        var orders = new[]
        {
            Order("2026-11-03", EmployeeOrderStatus.Cancelled, 2.10m),
            Order("2026-11-04", EmployeeOrderStatus.Pending, 3.35m),
        };

        var settlement = SettlementRules.Build(
            SettlementType.Monthly, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30), orders, GeneratedAt, null);

        Assert.Null(settlement);
    }
}
