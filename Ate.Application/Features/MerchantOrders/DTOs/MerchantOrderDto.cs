using Ate.Application.Features.EmployeeOrders.DTOs;

namespace Ate.Application.Features.MerchantOrders.DTOs;

/// <summary>Pedido tal como lo ve el restaurante: quién lo pidió, de qué empresa y cómo lo quiere.</summary>
public record MerchantOrderDto(
    string Id,
    string TicketNumber,
    string EmployeeName,
    string EmployeeDocument,
    string EmployeeCode,
    string? Department,
    string CompanyName,
    string MealType,
    string ServiceDate,
    IReadOnlyList<EmployeeOrderItemDto> Items,
    decimal TotalAmount,
    string Currency,
    string Status,
    string DeliveryMode,
    string? DeliveryAddress,
    string? Notes,
    DateTimeOffset OrderedAt,
    DateTimeOffset? UpdatedAt);

/// <param name="Status">Nuevo estado: "Confirmed", "Ready", "Delivered" o "Cancelled".</param>
public record UpdateMerchantOrderStatusRequest(string Status);
