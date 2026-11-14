using System.Text;
using Ate.Api.Extensions;
using Ate.Api.Middlewares;
using Ate.Api.OpenApi;
using Ate.Api.Services;
using Ate.Application.Common.Constants;
using System.Threading.RateLimiting;
using Ate.Application.Common.Interfaces;
using Ate.Application.Common.Models;
using Ate.Domain.Common;
using Ate.Domain.Errors;
using Ate.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;
using Microsoft.IdentityModel.Tokens;

namespace Ate.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            options.AddOperationTransformer<AuthorizationResponsesTransformer>();
            options.AddOperationTransformer<OperationIdTransformer>();
        });
        services.AddHttpContextAccessor();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("JwtSettings section is missing or invalid.");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                ClockSkew = TimeSpan.Zero
            };
            options.Events = new JwtBearerEvents
            {
                // A valid token is not enough: the account must still be active on every request.
                OnTokenValidated = async context =>
                {
                    var userId = context.Principal.GetUserId();
                    if (!userId.HasValue)
                    {
                        context.Fail("Token without user id.");
                        return;
                    }

                    var error = await GetAccessErrorAsync(context.HttpContext, userId.Value);
                    if (error is { } blocked)
                    {
                        context.HttpContext.Items[AccessErrorItemKey] = blocked;
                        context.Fail(blocked.Description);
                    }
                },
                OnChallenge = async context =>
                {
                    context.HandleResponse();
                    var error = context.HttpContext.Items[AccessErrorItemKey] is Error blocked ? blocked : AuthErrors.InvalidToken;
                    await WriteAuthErrorAsync(context.Response, StatusCodes.Status401Unauthorized, error);
                },
                OnForbidden = context =>
                    WriteAuthErrorAsync(context.Response, StatusCodes.Status403Forbidden, AuthErrors.Forbidden)
            };
        });

        services.AddMemoryCache();
        services.AddAuthorization();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                var message = "Demasiados intentos. Espera un momento e inténtalo de nuevo.";
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    var seconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
                    context.HttpContext.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
                    message = $"Demasiados intentos. Espera {seconds} segundos e inténtalo de nuevo.";
                }

                await context.HttpContext.Response.WriteAsJsonAsync(
                    ApiResponse.FailureResponse(message, null, "RateLimit.Exceeded"),
                    cancellationToken);
            };

            var loginThrottling = configuration.GetSection(LoginThrottlingOptions.SectionName).Get<LoginThrottlingOptions>()
                ?? new LoginThrottlingOptions();

            options.AddPolicy(RateLimitPolicies.Login, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = loginThrottling.MaxRequestsPerIpPerMinute,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));

            options.AddPolicy(RateLimitPolicies.Credentials, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                        ?? httpContext.Connection.RemoteIpAddress?.ToString()
                        ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));

            options.AddPolicy(RateLimitPolicies.PasswordRecovery, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
        });

        return services;
    }

    private const string AccessErrorItemKey = "Auth.AccessError";

    /// <summary>How long an account status check is reused before asking the database again.</summary>
    private static readonly TimeSpan AccessCheckCacheDuration = TimeSpan.FromSeconds(30);

    private sealed record AccessCheck(Error? Error);

    private static async Task<Error?> GetAccessErrorAsync(HttpContext httpContext, Guid userId)
    {
        var cache = httpContext.RequestServices.GetRequiredService<IMemoryCache>();
        var cacheKey = $"user-access:{userId}";
        if (cache.TryGetValue(cacheKey, out AccessCheck? cached) && cached != null)
        {
            return cached.Error;
        }

        var validator = httpContext.RequestServices.GetRequiredService<IUserAccessValidator>();
        var error = await validator.ValidateAsync(userId, httpContext.RequestAborted);
        cache.Set(cacheKey, new AccessCheck(error), AccessCheckCacheDuration);
        return error;
    }

    private static async Task WriteAuthErrorAsync(HttpResponse response, int statusCode, Error error)
    {
        if (response.HasStarted)
            return;

        response.StatusCode = statusCode;
        await response.WriteAsJsonAsync(ApiResponse.FailureResponse(error.Description, null, error.Code));
    }
}
