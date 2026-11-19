using System.Diagnostics;
using Ate.Application.Common.Tracing;
using Serilog.Context;

namespace Ate.Api.Middlewares;

public sealed class TraceIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var traceId = Activity.Current?.TraceId.ToHexString() ?? context.TraceIdentifier;
        context.TraceIdentifier = traceId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[RequestTrace.HeaderName] = traceId;
            return Task.CompletedTask;
        });

        using (RequestTrace.Begin(traceId))
        using (LogContext.PushProperty(RequestTrace.LogPropertyName, traceId))
        {
            await next(context);
        }
    }
}
