using Ate.Api.Extensions;
using Ate.Application.Common.Interfaces;
using Ate.Application.Common.Models;
using Ate.Application.Features.EmployeeOrders.DTOs;
using Ate.Domain.Constants;
using Ate.Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ate.Api.Controllers;

[ApiController]
[Route("api/v1/employee/orders")]
[Produces("application/json")]
[Tags("Employee Orders")]
[Authorize(Roles = RoleConstants.Employee)]
public class EmployeeOrdersController : ControllerBase
{
    private readonly IEmployeeOrderService _employeeOrderService;

    public EmployeeOrdersController(IEmployeeOrderService employeeOrderService)
    {
        _employeeOrderService = employeeOrderService;
    }

    [HttpPost]
    [EndpointSummary("Order a meal for the authenticated employee")]
    [EndpointDescription("Validates the company agreement, the dish and the restaurant cutoff time, then stores the order in MongoDB (employee_orders) with a ticket number and status history. Only one active order per meal and day is allowed.")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EmployeeOrderDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<EmployeeOrderDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<EmployeeOrderDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<EmployeeOrderDto>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateEmployeeOrderRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return ResultExtensions.ToErrorActionResult<EmployeeOrderDto>(AuthErrors.InvalidToken, this);
        }

        var result = await _employeeOrderService.CreateAsync(userId.Value, request, cancellationToken);
        return result.ToActionResult(this, "Order registered successfully.");
    }

    [HttpGet]
    [EndpointSummary("List the authenticated employee's orders")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<EmployeeOrderDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<EmployeeOrderDto>>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyOrders([FromQuery] PaginationParams pagination, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return ResultExtensions.ToErrorActionResult<PagedResult<EmployeeOrderDto>>(AuthErrors.InvalidToken, this);
        }

        var result = await _employeeOrderService.GetMyOrdersAsync(userId.Value, pagination, cancellationToken);
        return result.ToActionResult(this, "Orders retrieved successfully.");
    }

    [HttpGet("{orderId}")]
    [EndpointSummary("Get one of the authenticated employee's orders with its status history")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EmployeeOrderDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyOrder(string orderId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return ResultExtensions.ToErrorActionResult<EmployeeOrderDto>(AuthErrors.InvalidToken, this);
        }

        var result = await _employeeOrderService.GetMyOrderAsync(userId.Value, orderId, cancellationToken);
        return result.ToActionResult(this, "Order retrieved successfully.");
    }

    [HttpPost("{orderId}/cancel")]
    [EndpointSummary("Cancel a pending order before the restaurant cutoff time")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EmployeeOrderDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<EmployeeOrderDto>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelMyOrder(string orderId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return ResultExtensions.ToErrorActionResult<EmployeeOrderDto>(AuthErrors.InvalidToken, this);
        }

        var result = await _employeeOrderService.CancelMyOrderAsync(userId.Value, orderId, cancellationToken);
        return result.ToActionResult(this, "Order cancelled successfully.");
    }
}
