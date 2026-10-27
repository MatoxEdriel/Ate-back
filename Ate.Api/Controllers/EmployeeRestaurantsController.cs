using Ate.Api.Extensions;
using Ate.Application.Common.Interfaces;
using Ate.Application.Common.Models;
using Ate.Application.Features.Restaurants.DTOs;
using Ate.Domain.Constants;
using Ate.Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ate.Api.Controllers;

[ApiController]
[Route("api/v1/employee/restaurants")]
[Produces("application/json")]
[Tags("Employee Restaurants")]
[Authorize(Roles = RoleConstants.Employee)]
public class EmployeeRestaurantsController : ControllerBase
{
    private readonly IRestaurantService _restaurantService;

    public EmployeeRestaurantsController(IRestaurantService restaurantService)
    {
        _restaurantService = restaurantService;
    }

    [HttpGet]
    [EndpointSummary("List the restaurants available to the authenticated employee")]
    [EndpointDescription("Returns only restaurants with an active agreement with the employee's company, with their cutoff times and available menu per meal (desayuno, almuerzo, merienda).")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RestaurantDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RestaurantDto>>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRestaurants(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return ResultExtensions.ToErrorActionResult<IReadOnlyList<RestaurantDto>>(AuthErrors.InvalidToken, this);
        }

        var result = await _restaurantService.GetForEmployeeAsync(userId.Value, cancellationToken);
        return result.ToActionResult(this, "Restaurants retrieved successfully.");
    }
}
