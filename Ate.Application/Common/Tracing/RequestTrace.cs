namespace Ate.Application.Common.Tracing;

public static class RequestTrace
{
    public const string HeaderName = "X-Trace-Id";

    public const string LogPropertyName = "TraceId";

    private static readonly AsyncLocal<string?> Current = new();

    public static string? CurrentId => Current.Value;

    public static IDisposable Begin(string traceId)
    {
        var previous = Current.Value;
        Current.Value = traceId;
        return new Scope(previous);
    }

    private sealed class Scope(string? previous) : IDisposable
    {
        public void Dispose() => Current.Value = previous;
    }
}
