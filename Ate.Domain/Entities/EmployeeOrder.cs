using Ate.Domain.Enums;

namespace Ate.Domain.Entities;

/// <summary>
/// Pedido de comida de un colaborador. Se persiste en MongoDB (colección employee_orders).
/// Guarda una copia (snapshot) del restaurante y del plato para que el historial no cambie si el menú cambia.
/// </summary>
public class EmployeeOrder
{
    public string Id { get; set; } = string.Empty;
    public string TicketNumber { get; set; } = string.Empty;

    public Guid EmployeeId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid UserId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeDocument { get; set; } = string.Empty;
    public string? Department { get; set; }

    public Guid RestaurantId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public MealType MealType { get; set; }
    /// <summary>Día de servicio en hora de negocio (America/Guayaquil), formato yyyy-MM-dd.</summary>
    public string ServiceDate { get; set; } = string.Empty;
    /// <summary>
    /// "{employeeId}:{serviceDate}:{mealType}" mientras el pedido está activo; se elimina al cancelar.
    /// Tiene índice único: impide dos pedidos activos para la misma comida del mismo día.
    /// </summary>
    public string? ActiveSlot { get; set; }

    public List<EmployeeOrderItem> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";

    public string Status { get; set; } = EmployeeOrderStatus.Pending;
    public List<EmployeeOrderStatusChange> StatusHistory { get; set; } = new();
    public string? Notes { get; set; }

    public DateTimeOffset OrderedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}

public class EmployeeOrderItem
{
    public Guid DishId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Protein { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
    /// <summary>Null cuando el plato no ofrece la opción de arroz.</summary>
    public bool? WithRice { get; set; }
    public string? Drink { get; set; }
    public string? Notes { get; set; }
}

public class EmployeeOrderStatusChange
{
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset ChangedAt { get; set; }
    public Guid? ChangedByUserId { get; set; }
    public string? Note { get; set; }
}

public static class EmployeeOrderStatus
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Ready = "Ready";
    public const string Delivered = "Delivered";
    public const string Cancelled = "Cancelled";
}
