using System.Security.Claims;
using Ate.Api.Extensions;
using Ate.Application.Common.Interfaces;
using Ate.Application.Common.Models;
using Ate.Application.Features.Auth.DTOs;
using Ate.Api.Filters;
using Ate.Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Ate.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Tags("Authentication")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IWebHostEnvironment _environment;

    public AuthController(IAuthService authService, IWebHostEnvironment environment)
    {
        _authService = authService;
        _environment = environment;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [EndpointSummary("Authenticate user with national ID (cédula)")]
    [EndpointDescription("Authenticates a person by national ID (cédula, document type NationalId) and password; outer spaces are trimmed and leading zeros kept. An unknown cédula and a wrong password both answer 401 Auth.InvalidCredentials; 403 Auth.UserInactive is only returned when the password is correct. Failed attempts are limited per cédula and IP (429 with Retry-After).")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _authService.LoginAsync(request, clientIp, cancellationToken);
        return result.ToActionResult(this, "User authenticated successfully.");
    }

    [HttpGet("me")]
    [Authorize]
    [AllowWhilePasswordChangeRequired]
    [EndpointSummary("Get current authenticated user profile")]
    [EndpointDescription("Returns authenticated identity profile extracted from the validated JWT claims.")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public IActionResult GetCurrentUser()
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return ResultExtensions.ToErrorActionResult<object>(AuthErrors.InvalidToken, this);
        }

        var profile = new
        {
            id = userId.Value,
            email = User.GetEmail(),
            fullName = User.GetFullName(),
            documentNumber = User.GetDocumentNumber(),
            roles = User.GetRoles()
        };

        return Ok(ApiResponse<object>.SuccessResponse(profile, "User profile retrieved successfully."));
    }

    [HttpPost("change-password")]
    [Authorize]
    [AllowWhilePasswordChangeRequired]
    [EnableRateLimiting(RateLimitPolicies.Credentials)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [EndpointSummary("Change the password of the signed-in user")]
    [EndpointDescription("Sets the new password (8-128 characters). For the mandatory change of a temporary password (user.mustChangePassword) only newPassword is needed and it must differ from the temporary one; otherwise currentPassword is required. Clears the flag, revokes every other session and returns a fresh session. It is the only endpoint (with /me) allowed while the change is pending.")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return ResultExtensions.ToErrorActionResult<LoginResponse>(AuthErrors.InvalidToken, this);
        }

        var result = await _authService.ChangePasswordAsync(userId.Value, request, cancellationToken);
        return result.ToActionResult(this, "Contraseña actualizada.");
    }

    [HttpPost("dev-token")]
    [AllowAnonymous]
    [EndpointSummary("Generate development bypass token")]
    [EndpointDescription("Generates a valid JWT access token for local development and endpoint testing without requiring credentials.")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GenerateDevToken([FromBody] DevTokenRequest? request, CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<LoginResponse>.FailureResponse("Development bypass token generation is only allowed in Development environment."));
        }

        var result = await _authService.GenerateDevTokenAsync(request, cancellationToken);
        return result.ToActionResult(this, "Development token generated successfully.");
    }

    [HttpGet("dev-token")]
    [AllowAnonymous]
    [EndpointSummary("Generate development bypass token (quick GET)")]
    [EndpointDescription("Quick GET endpoint to retrieve a valid development JWT access token with default or specified role.")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDevToken([FromQuery] string? role, [FromQuery] string? email, CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<LoginResponse>.FailureResponse("Development bypass token generation is only allowed in Development environment."));
        }

        var request = new DevTokenRequest(role, email);
        var result = await _authService.GenerateDevTokenAsync(request, cancellationToken);
        return result.ToActionResult(this, "Development token generated successfully.");
    }
}
