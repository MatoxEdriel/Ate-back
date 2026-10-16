using System.Security.Claims;
using Ate.Application.Common.Interfaces;
using Ate.Application.Common.Models;
using Ate.Application.Features.Auth.DTOs;
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

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EndpointSummary("Authenticate user with national ID (cédula)")]
    [EndpointDescription("Authenticates person with national ID (cédula / document number) and password. Returns standardized ApiResponse with JWT access token, refresh token, and profile.")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DocumentNumber) || string.IsNullOrWhiteSpace(request.Password))
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(request.DocumentNumber))
            {
                errors.Add("Document number (cédula) is required.");
            }
            if (string.IsNullOrWhiteSpace(request.Password))
            {
                errors.Add("Password is required.");
            }

            return BadRequest(ApiResponse<LoginResponse>.FailureResponse("Validation failed for login payload.", errors));
        }

        var response = await _authService.LoginAsync(request, cancellationToken);
        if (response == null)
        {
            return Unauthorized(ApiResponse<LoginResponse>.FailureResponse("Invalid document number or password."));
        }

        return Ok(ApiResponse<LoginResponse>.SuccessResponse(response, "User authenticated successfully."));
    }

    [HttpGet("me")]
    [Authorize]
    [EndpointSummary("Get current authenticated user profile")]
    [EndpointDescription("Returns authenticated identity profile extracted from the validated JWT claims.")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var fullName = User.FindFirstValue(ClaimTypes.Name);
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<object>.FailureResponse("User identity could not be verified from token."));
        }

        var profile = new
        {
            id = Guid.Parse(userId),
            email = email ?? string.Empty,
            fullName = fullName ?? string.Empty,
            roles
        };

        return Ok(ApiResponse<object>.SuccessResponse(profile, "User profile retrieved successfully."));
    }
}
