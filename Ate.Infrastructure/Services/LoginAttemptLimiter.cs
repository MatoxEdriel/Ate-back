using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Ate.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace Ate.Infrastructure.Services;

/// <summary>
/// In-memory fixed windows that start at the first failure. Keys hold a SHA-256 of the document,
/// never the document itself.
/// </summary>
public sealed class LoginAttemptLimiter : ILoginAttemptLimiter, ISingletonDependency
{
    private const int CleanupThreshold = 10_000;
    private const string UnknownIp = "unknown";

    private readonly ConcurrentDictionary<string, AttemptWindow> _windows = new();
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly LoginThrottlingOptions _options;

    public LoginAttemptLimiter(IDateTimeProvider dateTimeProvider, IOptions<LoginThrottlingOptions> options)
    {
        _dateTimeProvider = dateTimeProvider;
        _options = options.Value;
    }

    private TimeSpan Window => TimeSpan.FromMinutes(_options.WindowMinutes);

    public TimeSpan? GetRetryAfter(string documentNumber, string? clientIp)
    {
        var now = _dateTimeProvider.UtcNow;
        var (pairKey, documentKey) = Keys(documentNumber, clientIp);

        var pairWait = RemainingBlock(pairKey, _options.MaxFailuresPerDocumentAndIp, now);
        var documentWait = RemainingBlock(documentKey, _options.MaxFailuresPerDocument, now);

        if (pairWait is null) return documentWait;
        if (documentWait is null) return pairWait;
        return pairWait > documentWait ? pairWait : documentWait;
    }

    public void RegisterFailure(string documentNumber, string? clientIp)
    {
        var now = _dateTimeProvider.UtcNow;
        var (pairKey, documentKey) = Keys(documentNumber, clientIp);

        Increment(pairKey, now);
        Increment(documentKey, now);

        if (_windows.Count > CleanupThreshold)
            RemoveExpired(now);
    }

    public void Reset(string documentNumber, string? clientIp)
    {
        var (pairKey, documentKey) = Keys(documentNumber, clientIp);
        _windows.TryRemove(pairKey, out _);
        _windows.TryRemove(documentKey, out _);
    }

    private TimeSpan? RemainingBlock(string key, int limit, DateTimeOffset now)
    {
        if (!_windows.TryGetValue(key, out var window))
            return null;

        lock (window)
        {
            var endsAt = window.StartedAt + Window;
            if (now >= endsAt || window.Failures < limit)
                return null;
            return endsAt - now;
        }
    }

    private void Increment(string key, DateTimeOffset now)
    {
        var window = _windows.GetOrAdd(key, _ => new AttemptWindow(now));
        lock (window)
        {
            if (now >= window.StartedAt + Window)
            {
                window.StartedAt = now;
                window.Failures = 0;
            }
            window.Failures++;
        }
    }

    private void RemoveExpired(DateTimeOffset now)
    {
        foreach (var (key, window) in _windows)
        {
            bool expired;
            lock (window)
            {
                expired = now >= window.StartedAt + Window;
            }
            if (expired)
                _windows.TryRemove(key, out _);
        }
    }

    private static (string PairKey, string DocumentKey) Keys(string documentNumber, string? clientIp)
    {
        var documentHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(documentNumber.Trim())));
        var ip = string.IsNullOrWhiteSpace(clientIp) ? UnknownIp : clientIp;
        return ($"doc-ip:{documentHash}:{ip}", $"doc:{documentHash}");
    }

    private sealed class AttemptWindow(DateTimeOffset startedAt)
    {
        public DateTimeOffset StartedAt { get; set; } = startedAt;
        public int Failures { get; set; }
    }
}
