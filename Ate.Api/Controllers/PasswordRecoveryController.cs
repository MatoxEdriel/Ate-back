using Ate.Api.Extensions;
using Ate.Application.Common.Interfaces;
using Ate.Application.Common.Models;
using Ate.Application.Features.PasswordRecovery.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Ate.Api.Controllers;

/// <summary>
/// Password recovery by email OTP. Anonymous, rate limited per IP and per account,
/// and never reveals whether an email is registered.
/// </summary>
[ApiController]
[Route("api/v1/auth/password-recovery")]
[Produces("application/json")]
[Tags("Password Recovery")]
[AllowAnonymous]
[EnableRateLimiting(RateLimitPolicies.PasswordRecovery)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class PasswordRecoveryController : ControllerBase
{
    private readonly IPasswordRecoveryService _passwordRecoveryService;

    public PasswordRecoveryController(IPasswordRecoveryService passwordRecoveryService)
    {
        _passwordRecoveryService = passwordRecoveryService;
    }

    [HttpPost("request")]
    [EndpointSummary("Send a password recovery code (OTP) by email")]
    [EndpointDescription("Queues an email with a 5-digit code valid for 10 minutes if the address belongs to an active account. Always answers the same, in about the same time, so it does not reveal which emails are registered. Throttled per account (cooldown and hourly cap) and per IP.")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> RequestCode([FromBody] RequestPasswordRecoveryRequest request, CancellationToken cancellationToken)
    {
        var result = await _passwordRecoveryService.RequestCodeAsync(request, cancellationToken);
        return result.ToActionResult(this, "Si el correo está registrado, te enviamos un código de verificación.");
    }

    [HttpPost("verify")]
    [EndpointSummary("Verify the password recovery code")]
    [EndpointDescription("Checks the code against its expiration (10 minutes), the attempt limit and single use. On success returns a single-use reset token with its own expiration (expiresAt, UTC).")]
    [ProducesResponseType(typeof(ApiResponse<PasswordResetTokenDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PasswordResetTokenDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<PasswordResetTokenDto>), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> VerifyCode([FromBody] VerifyPasswordRecoveryCodeRequest request, CancellationToken cancellationToken)
    {
        var result = await _passwordRecoveryService.VerifyCodeAsync(request, cancellationToken);
        return result.ToActionResult(this, "Código verificado.");
    }

    [HttpPost("reset")]
    [EndpointSummary("Set a new password with the reset token")]
    [EndpointDescription("Changes the password if the reset token is valid, unused and not expired, and revokes every refresh token of the account.")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _passwordRecoveryService.ResetPasswordAsync(request, cancellationToken);
        return result.ToActionResult(this, "Contraseña actualizada.");
    }
}
