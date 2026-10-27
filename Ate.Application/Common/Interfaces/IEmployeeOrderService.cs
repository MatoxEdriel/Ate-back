using Ate.Application.Common.Models;
using Ate.Application.Features.EmployeeOrders.DTOs;
using Ate.Domain.Common;

namespace Ate.Application.Common.Interfaces;

public interface IEmployeeOrderService
{
    Task<Result<EmployeeOrderDto>> CreateAsync(Guid userId, CreateEmployeeOrderRequest request, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<EmployeeOrderDto>>> GetMyOrdersAsync(Guid userId, PaginationParams pagination, CancellationToken cancellationToken = default);
    Task<Result<EmployeeOrderDto>> GetMyOrderAsync(Guid userId, string orderId, CancellationToken cancellationToken = default);
    Task<Result<EmployeeOrderDto>> CancelMyOrderAsync(Guid userId, string orderId, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<EmployeeOrderDto>>> GetCompanyEmployeeOrdersAsync(Guid adminUserId, Guid employeeId, PaginationParams pagination, CancellationToken cancellationToken = default);
}
