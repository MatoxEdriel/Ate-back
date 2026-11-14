using Ate.Domain.Enums;

namespace Ate.Domain.Entities;

/// <summary>
/// Corte de nómina de un colaborador. Se persiste en MongoDB (colección payroll_settlements) y no cambia
/// después de generado: es el registro de lo que se le descontó.
/// </summary>
public class PayrollSettlement
{
    public string Id { get; set; } = string.Empty;
    public SettlementType Type { get; set; }

    public Guid CompanyId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid UserId { get; set; }
    /// <summary>Copia al momento del corte.</summary>
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeDocument { get; set; } = string.Empty;

    /// <summary>Inicio del período por día de servicio, yyyy-MM-dd, inclusivo.</summary>
    public string PeriodStart { get; set; } = string.Empty;
    /// <summary>Fin del período por día de servicio, yyyy-MM-dd, inclusivo.</summary>
    public string PeriodEnd { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public int OrderCount { get; set; }
    /// <summary>Detalle de los pedidos cobrados. Solo lo ve el colaborador, nunca RRHH.</summary>
    public List<PayrollSettlementLine> Lines { get; set; } = new();

    public DateTimeOffset GeneratedAt { get; set; }
    /// <summary>Null cuando lo generó el cierre automático.</summary>
    public Guid? GeneratedByUserId { get; set; }
}

public class PayrollSettlementLine
{
    public string OrderId { get; set; } = string.Empty;
    public string TicketNumber { get; set; } = string.Empty;
    public string ServiceDate { get; set; } = string.Empty;
    public string RestaurantName { get; set; } = string.Empty;
    public MealType MealType { get; set; }
    public List<EmployeeOrderItem> Items { get; set; } = new();
    public decimal Amount { get; set; }
}
