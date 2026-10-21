using System.Security.Claims;
using Ate.Api.Extensions;
using Ate.Application.Common.Interfaces;
using Ate.Application.Common.Models;
using Ate.Application.Features.Auth.DTOs;
using Ate.Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    [EndpointSummary("Authenticate user with national ID (cédula)")]
    [EndpointDescription("Authenticates person with national ID (cédula / document number) and password. Returns standardized ApiResponse with JWT access token, refresh token, and profile.")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);
        return result.ToActionResult(this, "User authenticated successfully.");
    }

    [HttpGet("me")]
    [Authorize]
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
