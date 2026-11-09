using Ate.Api.Filters;
using Ate.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Ate.Api.OpenApi;

/// <summary>
/// For every operation that requires a token: adds the Bearer security requirement and documents the
/// JSON 401 (missing or invalid token) and 403 (role not allowed or temporary password pending).
/// </summary>
public sealed class AuthorizationResponsesTransformer : IOpenApiOperationTransformer
{
    private const string JsonContentType = "application/json";

    public async Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;
        var authorizeData = metadata.OfType<IAuthorizeData>().ToList();
        if (authorizeData.Count == 0 || metadata.OfType<IAllowAnonymous>().Any())
            return;

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(BearerSecuritySchemeTransformer.SchemeName, context.Document)] = []
        });

        var schema = await context.GetOrCreateSchemaAsync(typeof(ApiResponse), null, cancellationToken);
        operation.Responses ??= new OpenApiResponses();

        AddIfMissing(operation.Responses, StatusCodes.Status401Unauthorized,
            "Missing, invalid or expired access token (Auth.InvalidToken).", schema);

        var hasRoles = authorizeData.Any(data => !string.IsNullOrWhiteSpace(data.Roles));
        var blocksPendingPasswordChange = !metadata.OfType<AllowWhilePasswordChangeRequiredAttribute>().Any();
        var forbiddenReasons = new List<string>();
        if (hasRoles) forbiddenReasons.Add("the user's role is not allowed (Auth.Forbidden)");
        if (blocksPendingPasswordChange) forbiddenReasons.Add("a temporary password must be changed first (Auth.PasswordChangeRequired)");

        if (forbiddenReasons.Count > 0)
        {
            AddIfMissing(operation.Responses, StatusCodes.Status403Forbidden,
                $"Forbidden: {string.Join("; or ", forbiddenReasons)}.", schema);
        }
    }

    private static void AddIfMissing(OpenApiResponses responses, int statusCode, string description, IOpenApiSchema schema)
    {
        var key = statusCode.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (responses.TryGetValue(key, out var existing))
        {
            if (existing is OpenApiResponse response && string.IsNullOrWhiteSpace(response.Description))
                response.Description = description;
            return;
        }

        responses[key] = new OpenApiResponse
        {
            Description = description,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                [JsonContentType] = new OpenApiMediaType { Schema = schema }
            }
        };
    }
}
