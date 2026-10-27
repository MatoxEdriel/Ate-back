using Ate.Application.Features.MerchantOrders.DTOs;
using Ate.Domain.Common;

namespace Ate.Application.Common.Interfaces;

public interface IMerchantOrderService
{
    /// <param name="serviceDate">yyyy-MM-dd; null = hoy en hora de negocio.</param>
    Task<Result<IReadOnlyList<MerchantOrderDto>>> GetOrdersAsync(Guid merchantUserId, string? serviceDate, CancellationToken cancellationToken = default);
    Task<Result<MerchantOrderDto>> UpdateStatusAsync(Guid merchantUserId, string orderId, UpdateMerchantOrderStatusRequest request, CancellationToken cancellationToken = default);
}
