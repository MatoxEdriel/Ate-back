namespace Ate.Application.Common.Interfaces;

/// <summary>
/// Counts failed sign-ins per document number and client IP inside a temporary window.
/// Blocks are never permanent: they end when the window expires.
/// </summary>
public interface ILoginAttemptLimiter
{
    /// <summary>Time left before the document can be tried again from this IP, or null when allowed.</summary>
    TimeSpan? GetRetryAfter(string documentNumber, string? clientIp);

    void RegisterFailure(string documentNumber, string? clientIp);

    void Reset(string documentNumber, string? clientIp);
}
