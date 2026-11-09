using Ate.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Ate.Api.Filters;

/// <summary>
/// Runs the endpoint only in the Development environment; anywhere else it answers 404 as if it did not exist.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class DevelopmentOnlyAttribute : Attribute, IResourceFilter
{
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        var environment = context.HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        if (environment.IsDevelopment())
            return;

        context.Result = new NotFoundObjectResult(
            ApiResponse.FailureResponse("No encontramos lo que buscabas.", null, "Endpoint.NotFound"));
    }

    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }
}
