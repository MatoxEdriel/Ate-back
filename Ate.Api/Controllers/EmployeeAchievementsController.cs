using Ate.Api.Extensions;
using Ate.Application.Common.Interfaces;
using Ate.Application.Common.Models;
using Ate.Application.Features.Achievements.DTOs;
using Ate.Domain.Constants;
using Ate.Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ate.Api.Controllers;

[ApiController]
[Route("api/v1/employee/achievements")]
[Produces("application/json")]
[Tags("Employee Achievements")]
[Authorize(Roles = RoleConstants.Employee)]
public class EmployeeAchievementsController : ControllerBase
{
    private readonly IAchievementService _achievementService;

    public EmployeeAchievementsController(IAchievementService achievementService)
    {
        _achievementService = achievementService;
    }

    [HttpGet]
    [EndpointSummary("Trophy room of the authenticated employee")]
    [EndpointDescription("Every active trophy with its progress, plus the overall and per-tier completion. Progress is recomputed from the delivered orders on every call, so new trophies apply retroactively. Hidden trophies come as ??? until unlocked.")]
    [ProducesResponseType(typeof(ApiResponse<AchievementsOverviewDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverview(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return ResultExtensions.ToErrorActionResult<AchievementsOverviewDto>(AuthErrors.InvalidToken, this);
        }

        var result = await _achievementService.GetOverviewAsync(userId.Value, cancellationToken);
        return result.ToActionResult(this, "Achievements retrieved successfully.");
    }

    [HttpGet("unseen")]
    [EndpointSummary("Unlocked trophies whose unlock toast has not been shown")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AchievementDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnseen(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return ResultExtensions.ToErrorActionResult<IReadOnlyList<AchievementDto>>(AuthErrors.InvalidToken, this);
        }

        var result = await _achievementService.GetUnseenAsync(userId.Value, cancellationToken);
        return result.ToActionResult(this, "Unseen achievements retrieved successfully.");
    }

    [HttpPost("seen")]
    [EndpointSummary("Mark unlocked trophies as seen")]
    [EndpointDescription("Called after the unlock toast is shown, so it is not shown again. Returns how many were marked.")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkSeen([FromBody] MarkAchievementsSeenRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return ResultExtensions.ToErrorActionResult<int>(AuthErrors.InvalidToken, this);
        }

        var result = await _achievementService.MarkSeenAsync(userId.Value, request, cancellationToken);
        return result.ToActionResult(this, "Achievements marked as seen.");
    }
}
