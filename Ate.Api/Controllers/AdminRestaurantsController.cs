using Ate.Api.Extensions;
using Ate.Application.Common.Interfaces;
using Ate.Application.Common.Models;
using Ate.Application.Features.Restaurants.DTOs;
using Ate.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ate.Api.Controllers;

[ApiController]
[Route("api/v1/admin/restaurants")]
[Produces("application/json")]
[Tags("Admin Restaurants")]
[Authorize(Roles = RoleConstants.SuperAdmin)]
public class AdminRestaurantsController : ControllerBase
{
    private readonly IAdminRestaurantService _restaurantService;

    public AdminRestaurantsController(IAdminRestaurantService restaurantService)
    {
        _restaurantService = restaurantService;
    }

    [HttpGet]
    [EndpointSummary("List all the restaurants of the platform")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AdminRestaurantDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRestaurants(CancellationToken cancellationToken)
    {
        var result = await _restaurantService.GetAllAsync(cancellationToken);
        return result.ToActionResult(this, "Restaurants retrieved successfully.");
    }

    [HttpPatch("{restaurantId:guid}/status")]
    [EndpointSummary("Activate or deactivate a restaurant")]
    [EndpointDescription("A deactivated restaurant stops appearing for every company and cannot receive orders. Agreements are kept.")]
    [ProducesResponseType(typeof(ApiResponse<AdminRestaurantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AdminRestaurantDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(Guid restaurantId, [FromBody] UpdateRestaurantStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await _restaurantService.UpdateStatusAsync(restaurantId, request, cancellationToken);
        return result.ToActionResult(this, "Restaurant status updated successfully.");
    }
}
