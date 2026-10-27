namespace Ate.Application.Features.EmployeeOrders.DTOs;

public record EmployeeOrderDto(
    string Id,
    string TicketNumber,
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    string? Department,
    Guid RestaurantId,
    string RestaurantName,
    string MealType,
    string ServiceDate,
    IReadOnlyList<EmployeeOrderItemDto> Items,
    decimal TotalAmount,
    string Currency,
    string Status,
    IReadOnlyList<EmployeeOrderStatusChangeDto> StatusHistory,
    string? Notes,
    DateTimeOffset OrderedAt,
    DateTimeOffset? UpdatedAt);

public record EmployeeOrderItemDto(
    Guid DishId,
    string Name,
    string? Description,
    string? Protein,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal,
    bool? WithRice,
    string? Drink,
    string? Notes);

public record EmployeeOrderStatusChangeDto(
    string Status,
    DateTimeOffset ChangedAt,
    string? Note);
