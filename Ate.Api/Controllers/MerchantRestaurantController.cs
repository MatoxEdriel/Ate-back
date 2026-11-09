using Ate.Api.Extensions;
using Ate.Application.Common.Interfaces;
using Ate.Application.Common.Models;
using Ate.Application.Features.MerchantRestaurant.DTOs;
using Ate.Domain.Constants;
using Ate.Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ate.Api.Controllers;

[ApiController]
[Route("api/v1/merchant/restaurant")]
[Produces("application/json")]
[Tags("Merchant Restaurant")]
[Authorize(Roles = RoleConstants.Merchant)]
public class MerchantRestaurantController : ControllerBase
{
    private readonly IMerchantRestaurantService _merchantRestaurantService;

    public MerchantRestaurantController(IMerchantRestaurantService merchantRestaurantService)
    {
        _merchantRestaurantService = merchantRestaurantService;
    }

    [HttpGet]
    [EndpointSummary("Get the profile of the merchant's restaurant")]
    [ProducesResponseType(typeof(ApiResponse<MerchantRestaurantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<MerchantRestaurantDto>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return ResultExtensions.ToErrorActionResult<MerchantRestaurantDto>(AuthErrors.InvalidToken, this);
        }

        var result = await _merchantRestaurantService.GetAsync(userId.Value, cancellationToken);
        return result.ToActionResult(this, "Restaurant retrieved successfully.");
    }

    [HttpPut]
    [EndpointSummary("Update the profile of the merchant's restaurant")]
    [EndpointDescription("Name, cuisine, address, phone, brand color, preparation time, map location, logo and order cutoff times. logo is temporary: a base64 data URL (png/webp/jpeg, up to 300k chars) until the image server exists; null keeps it and an empty string removes it. Changing name, phone, address, location or logo requires currentPassword (400 MerchantRestaurant.PasswordRequired / InvalidPassword otherwise). cutoff is optional and only the meals present (desayuno, almuerzo, merienda, as HH:mm) are changed. The new name and cutoff show up for employees right away.")]
    [ProducesResponseType(typeof(ApiResponse<MerchantRestaurantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<MerchantRestaurantDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<MerchantRestaurantDto>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update([FromBody] UpdateMerchantRestaurantRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return ResultExtensions.ToErrorActionResult<MerchantRestaurantDto>(AuthErrors.InvalidToken, this);
        }

        var result = await _merchantRestaurantService.UpdateAsync(userId.Value, request, cancellationToken);
        return result.ToActionResult(this, "Restaurant updated successfully.");
    }
}
