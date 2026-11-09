using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Ate.Api.OpenApi;

/// <summary>Declares the JWT bearer scheme so Scalar can send the token on protected operations.</summary>
public sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public const string SchemeName = "Bearer";

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Access token returned by POST /api/v1/Auth/login (data.accessToken)."
        };
        return Task.CompletedTask;
    }
}
