using Ate.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace Ate.Api.Extensions;

public static class InvalidRequestResponse
{
    public const string Code = "Request.InvalidBody";
    private const string Message = "No pudimos leer los datos enviados. Revisa el formulario e inténtalo de nuevo.";

    public static IActionResult Create(ActionContext context)
    {
        var fields = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .Select(entry => entry.Key.TrimStart('$', '.'))
            .Where(field => field.Length > 0)
            .Distinct()
            .ToList();

        return new BadRequestObjectResult(ApiResponse.FailureResponse(Message, fields, Code));
    }
}
