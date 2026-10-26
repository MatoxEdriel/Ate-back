using Ate.Api.Extensions;
using Ate.Application.Common.Interfaces;
using Ate.Application.Common.Models;
using Ate.Application.Features.Profile.DTOs;
using Ate.Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ate.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Tags("Profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet]
    [EndpointSummary("Get the authenticated user's full profile")]
    [EndpointDescription("Returns personal information, roles and, when the user belongs to a company, the company details.")]
    [ProducesResponseType(typeof(ApiResponse<ProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProfileDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ProfileDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return ResultExtensions.ToErrorActionResult<ProfileDto>(AuthErrors.InvalidToken, this);
        }

        var result = await _profileService.GetProfileAsync(userId.Value, cancellationToken);
        return result.ToActionResult(this, "Profile retrieved successfully.");
    }
}
