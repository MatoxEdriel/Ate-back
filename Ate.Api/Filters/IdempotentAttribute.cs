using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Ate.Api.Extensions;
using Ate.Application.Common.Idempotency;
using Ate.Application.Common.Interfaces;
using Ate.Application.Common.Models;
using Ate.Domain.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Ate.Api.Filters;

[AttributeUsage(AttributeTargets.Method)]
public sealed class IdempotentAttribute : TypeFilterAttribute
{
    public const string HeaderName = "Idempotency-Key";
    public const string ReplayedHeader = "Idempotent-Replayed";
    public const int MaxKeyLength = 100;
    public static readonly TimeSpan Lease = TimeSpan.FromSeconds(60);

    public IdempotentAttribute() : base(typeof(IdempotencyFilter))
    {
    }
}

public sealed class IdempotencyFilter : IAsyncActionFilter
{
    private readonly IIdempotencyStore _store;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<IdempotencyFilter> _logger;

    public IdempotencyFilter(
        IIdempotencyStore store,
        IDateTimeProvider dateTimeProvider,
        IOptions<JsonOptions> jsonOptions,
        ILogger<IdempotencyFilter> logger)
    {
        _store = store;
        _dateTimeProvider = dateTimeProvider;
        _jsonOptions = jsonOptions.Value.JsonSerializerOptions;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        if (!http.Request.Headers.TryGetValue(IdempotentAttribute.HeaderName, out var values))
        {
            await next();
            return;
        }

        var controller = (ControllerBase)context.Controller;
        var key = values.ToString().Trim();
        var userId = http.User.GetUserId();
        if (key.Length is 0 or > IdempotentAttribute.MaxKeyLength || values.Count > 1 || userId is null)
        {
            context.Result = ResultExtensions.ToErrorActionResult(IdempotencyErrors.InvalidKey, controller);
            return;
        }

        var id = $"{userId.Value:N}:{key}";
        var requestHash = HashRequest(context);
        var now = _dateTimeProvider.UtcNow;

        if (!await _store.TryReserveAsync(id, requestHash, now, CancellationToken.None))
        {
            var existing = await _store.GetAsync(id, CancellationToken.None);
            var outcome = IdempotencyDecision.Decide(existing, requestHash, now, IdempotentAttribute.Lease);
            var tookOver = outcome == IdempotencyOutcome.Execute
                && await _store.TryTakeOverAsync(id, now - IdempotentAttribute.Lease, now, CancellationToken.None);

            if (!tookOver)
            {
                context.Result = outcome switch
                {
                IdempotencyOutcome.Replay => Replay(http, existing!),
                IdempotencyOutcome.KeyReused => controller.StatusCode(
                    StatusCodes.Status422UnprocessableEntity,
                    ApiResponse.FailureResponse(IdempotencyErrors.KeyReused.Description, null, IdempotencyErrors.KeyReused.Code)),
                    _ => ResultExtensions.ToErrorActionResult(IdempotencyErrors.InProgress, controller),
                };
                return;
            }
        }

        ActionExecutedContext executed;
        try
        {
            executed = await next();
        }
        catch
        {
            await ReleaseQuietlyAsync(id);
            throw;
        }

        if (executed.Exception == null && executed.Result is ObjectResult { StatusCode: < 500 } result)
        {
            var statusCode = result.StatusCode ?? StatusCodes.Status200OK;
            var body = result.Value == null ? "null" : JsonSerializer.Serialize(result.Value, result.Value.GetType(), _jsonOptions);
            try
            {
                await _store.CompleteAsync(id, statusCode, body, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not store the idempotent response for {IdempotencyId}.", id);
                await ReleaseQuietlyAsync(id);
            }
        }
        else
        {
            await ReleaseQuietlyAsync(id);
        }
    }

    private async Task ReleaseQuietlyAsync(string id)
    {
        try
        {
            await _store.ReleaseAsync(id, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not release idempotency ticket {IdempotencyId}; it expires after the lease.", id);
        }
    }

    private static ContentResult Replay(HttpContext http, Domain.Entities.IdempotencyRecord record)
    {
        http.Response.Headers[IdempotentAttribute.ReplayedHeader] = "true";
        return new ContentResult
        {
            StatusCode = record.StatusCode ?? StatusCodes.Status200OK,
            Content = record.Body,
            ContentType = "application/json; charset=utf-8",
        };
    }

    private string HashRequest(ActionExecutingContext context)
    {
        var arguments = context.ActionArguments
            .Where(a => a.Value is not CancellationToken)
            .OrderBy(a => a.Key, StringComparer.Ordinal)
            .ToDictionary(a => a.Key, a => a.Value);
        var request = context.HttpContext.Request;
        var payload = $"{request.Method} {request.Path}\n{JsonSerializer.Serialize(arguments, _jsonOptions)}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }
}
