using Ate.Application.Common.Models;
using Microsoft.AspNetCore.Diagnostics;

namespace Ate.Api.Middlewares;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception occurred while processing request on {Path}", httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/json";

        var errorMessage = _environment.IsDevelopment()
            ? exception.Message
            : "An unexpected server error occurred. Please try again later.";

        var errors = _environment.IsDevelopment() && exception.StackTrace != null
            ? new[] { exception.GetType().Name }
            : null;

        var response = ApiResponse<object>.FailureResponse(errorMessage, errors);

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

        return true;
    }
}
