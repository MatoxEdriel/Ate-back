using Ate.Application.Features.Restaurants;
using Ate.Domain.Entities;

namespace Ate.Application.Features.Payroll.DTOs;

/// <summary>Pedido cobrado dentro de un corte. Solo lo ve el propio colaborador.</summary>
public record SettlementLineDto(
    string TicketNumber,
    string ServiceDate,
    string RestaurantName,
    string MealType,
    IReadOnlyList<string> Items,
    decimal Amount)
{
    public static SettlementLineDto From(PayrollSettlementLine line) => new(
        line.TicketNumber,
        line.ServiceDate,
        line.RestaurantName,
        MealTypeCodes.ToCode(line.MealType),
        line.Items.Select(i => i.Quantity > 1 ? $"{i.Quantity} x {i.Name}" : i.Name).ToList(),
        line.Amount);
}

/// <summary>Corte de nómina con su detalle, para el colaborador.</summary>
public record EmployeeSettlementDto(
    string Id,
    string Type,
    string PeriodStart,
    string PeriodEnd,
    decimal TotalAmount,
    string Currency,
    int OrderCount,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<SettlementLineDto> Lines)
{
    public static EmployeeSettlementDto From(PayrollSettlement s) => new(
        s.Id,
        s.Type.ToString(),
        s.PeriodStart,
        s.PeriodEnd,
        s.TotalAmount,
        s.Currency,
        s.OrderCount,
        s.GeneratedAt,
        s.Lines.Select(SettlementLineDto.From).ToList());
}

/// <summary>Gasto del mes en curso, todavía sin cerrar.</summary>
public record CurrentSpendingDto(
    string PeriodStart,
    string PeriodEnd,
    decimal TotalAmount,
    string Currency,
    int OrderCount,
    IReadOnlyList<SettlementLineDto> Lines);
