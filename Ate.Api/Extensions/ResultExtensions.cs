using Ate.Application.Common.Models;
using Ate.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Ate.Api.Extensions;

public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result, ControllerBase controller, string? successMessage = null)
    {
        return result.Match(
            value => controller.Ok(ApiResponse<T>.SuccessResponse(value, successMessage ?? "Operation completed successfully.")),
            error => ToErrorActionResult<T>(error, controller)
        );
    }

    public static IActionResult ToActionResult(this Result result, ControllerBase controller, string? successMessage = null)
    {
        return result.Match(
            () => controller.Ok(ApiResponse.SuccessResponse(successMessage ?? "Operation completed successfully.")),
            error => ToErrorActionResult(error, controller)
        );
    }

    public static IActionResult ToErrorActionResult<T>(Error error, ControllerBase controller)
    {
        var response = ApiResponse<T>.FailureResponse(error.Description, error.Details);

        return error.Type switch
        {
            ErrorType.NotFound => controller.NotFound(response),
            ErrorType.Validation => controller.BadRequest(response),
            ErrorType.Conflict => controller.Conflict(response),
            ErrorType.Unauthorized => controller.Unauthorized(response),
            ErrorType.Forbidden => controller.StatusCode(StatusCodes.Status403Forbidden, response),
            _ => controller.BadRequest(response)
        };
    }

    public static IActionResult ToErrorActionResult(Error error, ControllerBase controller)
    {
        var response = ApiResponse.FailureResponse(error.Description, error.Details);

        return error.Type switch
        {
            ErrorType.NotFound => controller.NotFound(response),
            ErrorType.Validation => controller.BadRequest(response),
            ErrorType.Conflict => controller.Conflict(response),
            ErrorType.Unauthorized => controller.Unauthorized(response),
            ErrorType.Forbidden => controller.StatusCode(StatusCodes.Status403Forbidden, response),
            _ => controller.BadRequest(response)
        };
    }
}
