using Ate.Api.Extensions;
using Ate.Application.Common.Interfaces;
using Ate.Application.Common.Models;
using Ate.Application.Features.MerchantOrders.DTOs;
using Ate.Domain.Constants;
using Ate.Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ate.Api.Controllers;

[ApiController]
[Route("api/v1/merchant/orders")]
[Produces("application/json")]
[Tags("Merchant Orders")]
[Authorize(Roles = RoleConstants.Merchant)]
public class MerchantOrdersController : ControllerBase
{
    private readonly IMerchantOrderService _merchantOrderService;

    public MerchantOrdersController(IMerchantOrderService merchantOrderService)
    {
        _merchantOrderService = merchantOrderService;
    }

    [HttpGet]
    [EndpointSummary("List the orders of the merchant's restaurant for a service day")]
    [EndpointDescription("Defaults to today (business time, America/Guayaquil). Orders come sorted by the time they were placed, oldest first.")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<MerchantOrderDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<MerchantOrderDto>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<MerchantOrderDto>>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetOrders([FromQuery] string? date, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return ResultExtensions.ToErrorActionResult<IReadOnlyList<MerchantOrderDto>>(AuthErrors.InvalidToken, this);
        }

        var result = await _merchantOrderService.GetOrdersAsync(userId.Value, date, cancellationToken);
        return result.ToActionResult(this, "Orders retrieved successfully.");
    }

    [HttpPatch("{orderId}/status")]
    [EndpointSummary("Move an order forward (Confirmed, Ready, Delivered) or cancel it")]
    [ProducesResponseType(typeof(ApiResponse<MerchantOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<MerchantOrderDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<MerchantOrderDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<MerchantOrderDto>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateStatus(string orderId, [FromBody] UpdateMerchantOrderStatusRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return ResultExtensions.ToErrorActionResult<MerchantOrderDto>(AuthErrors.InvalidToken, this);
        }

        var result = await _merchantOrderService.UpdateStatusAsync(userId.Value, orderId, request, cancellationToken);
        return result.ToActionResult(this, "Order status updated successfully.");
    }
}
