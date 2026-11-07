using Ate.Application.Common.Models;
using Ate.Domain.Constants;
using Ate.Domain.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Ate.Api.Filters;

/// <summary>
/// Marks the endpoints a user may call while they still must replace a temporary password.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AllowWhilePasswordChangeRequiredAttribute : Attribute;

/// <summary>
/// Server-side enforcement of the mandatory password change: a token with the
/// <c>must_change_password</c> claim gets 403 "Auth.PasswordChangeRequired" everywhere except
/// endpoints marked with <see cref="AllowWhilePasswordChangeRequiredAttribute"/>.
/// The client screen is only the UX; this is what actually blocks the API.
/// </summary>
public sealed class PasswordChangeRequiredFilter : IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true ||
            !string.Equals(user.FindFirst(CustomClaimTypes.MustChangePassword)?.Value, "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var allowed = context.ActionDescriptor.EndpointMetadata.OfType<AllowWhilePasswordChangeRequiredAttribute>().Any();
        if (allowed)
        {
            return;
        }

        var error = AuthErrors.PasswordChangeRequired;
        context.Result = new ObjectResult(ApiResponse.FailureResponse(error.Description, null, error.Code))
        {
            StatusCode = StatusCodes.Status403Forbidden
        };
    }
}
