using Ate.Domain.Entities;

namespace Ate.Application.Features.Payroll.DTOs;

/// <summary>
/// Corte visto por RRHH: solo lo necesario para descontar en nómina.
/// Nunca incluye pedidos, platos, restaurantes ni fechas de consumo.
/// </summary>
public record CompanySettlementDto(
    string Id,
    string Type,
    string PeriodStart,
    string PeriodEnd,
    string EmployeeName,
    string EmployeeDocument,
    decimal TotalAmount,
    string Currency)
{
    public static CompanySettlementDto From(PayrollSettlement s) => new(
        s.Id,
        s.Type.ToString(),
        s.PeriodStart,
        s.PeriodEnd,
        s.EmployeeName,
        s.EmployeeDocument,
        s.TotalAmount,
        s.Currency);
}

/// <summary>
/// Uso de Ate en toda la empresa durante un mes. Agregado: sin datos por persona ni por área.
/// Si pidieron menos de <see cref="MinEmployees"/> colaboradores, las cifras no se envían
/// (<see cref="HasEnoughData"/> = false): con tan pocos, RRHH podría deducir el gasto de una persona.
/// </summary>
public record CompanyUsageDto(
    string Month,
    bool HasEnoughData,
    int? EmployeesWithOrders,
    int? OrderCount,
    decimal? TotalAmount,
    string Currency)
{
    public const int MinEmployees = 5;
}
