using Ate.Application.Common.Interfaces;
using Ate.Application.Common.Models;
using Ate.Application.Common.Models.Emails;
using Ate.Domain.Enums;
using Ate.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Ate.Api.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[Produces("application/json")]
[Tags("Notifications")]
public class NotificationsController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly SmtpOptions _smtpOptions;

    public NotificationsController(IEmailService emailService, IOptions<SmtpOptions> smtpOptions)
    {
        _emailService = emailService;
        _smtpOptions = smtpOptions.Value;
    }

    [HttpPost("test-welcome-email")]
    [AllowAnonymous]
    [EndpointSummary("Send a test welcome email")]
    [EndpointDescription("Sends a welcome email with temporary password using Ate brand design to verify SMTP delivery.")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendTestWelcomeEmail([FromBody] SendTestWelcomeEmailRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ToEmail))
        {
            return BadRequest(ApiResponse<string>.FailureResponse("Recipient email is required.", new[] { "ToEmail cannot be empty." }));
        }

        var recipientName = string.IsNullOrWhiteSpace(request.FullName) ? "Colaborador de Prueba" : request.FullName.Trim();
        var temporaryPassword = string.IsNullOrWhiteSpace(request.TemporaryPassword) ? "TempPass*2026" : request.TemporaryPassword.Trim();
        var loginUrl = $"{_smtpOptions.ClientBaseUrl.TrimEnd('/')}/login";

        var model = new WelcomeUserEmailModel(
            recipientName,
            request.ToEmail.Trim(),
            temporaryPassword,
            loginUrl);

        try
        {
            await _emailService.SendTemplateEmailAsync(
                request.ToEmail.Trim(),
                "¡Bienvenido a Ate! Tu cuenta ha sido creada",
                EmailTemplateType.WelcomeUser,
                model,
                cancellationToken: cancellationToken);

            return Ok(ApiResponse<string>.SuccessResponse(request.ToEmail.Trim(), $"Welcome test email sent successfully to {request.ToEmail.Trim()}."));
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse<string>.FailureResponse("Failed to send email via SMTP.", new[] { ex.Message }));
        }
    }
}

public record SendTestWelcomeEmailRequest(
    string ToEmail,
    string? FullName = null,
    string? TemporaryPassword = null);

