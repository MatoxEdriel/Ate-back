using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Ate.Api.OpenApi;

/// <summary>Gives every controller operation a stable operationId: "{Controller}_{Action}".</summary>
public sealed class OperationIdTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(operation.OperationId) &&
            context.Description.ActionDescriptor is ControllerActionDescriptor action)
        {
            operation.OperationId = $"{action.ControllerName}_{action.ActionName}";
        }
        return Task.CompletedTask;
    }
}
