using System.Security.Cryptography;
using System.Text;
using Ate.Application.Common.Interfaces;
using Ate.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Ate.Infrastructure.Services;

/// <summary>
/// Intentos de login guardados en la base (tabla login_attempt_windows). Las claves son SHA-256: nunca
/// se guarda la cédula.
/// </summary>
public sealed class LoginAttemptLimiter : ILoginAttemptLimiter, IScopedDependency
{
    private const string UnknownIp = "unknown";
    private static readonly TimeSpan FirstDocumentBlock = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MaxDocumentBlock = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan StaleAfter = TimeSpan.FromDays(1);

    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly LoginThrottlingOptions _options;

    public LoginAttemptLimiter(IApplicationDbContext context, IDateTimeProvider dateTimeProvider, IOptions<LoginThrottlingOptions> options)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _options = options.Value;
    }

    private TimeSpan Window => TimeSpan.FromMinutes(_options.WindowMinutes);

    public async Task<TimeSpan?> GetRetryAfterAsync(string documentNumber, string? clientIp, CancellationToken cancellationToken = default)
    {
        var now = _dateTimeProvider.UtcNow;
        var (pairKey, documentKey) = Keys(documentNumber, clientIp);
        var windows = await _context.LoginAttemptWindows
            .AsNoTracking()
            .Where(w => w.Key == pairKey || w.Key == documentKey)
            .ToListAsync(cancellationToken);

        TimeSpan? wait = null;

        var pair = windows.FirstOrDefault(w => w.Key == pairKey);
        if (pair != null && pair.Failures >= _options.MaxFailuresPerDocumentAndIp && now < pair.WindowStart + Window)
        {
            wait = pair.WindowStart + Window - now;
        }

        var document = windows.FirstOrDefault(w => w.Key == documentKey);
        if (document?.BlockedUntil is { } blockedUntil && blockedUntil > now)
        {
            var documentWait = blockedUntil - now;
            wait = wait is null || documentWait > wait ? documentWait : wait;
        }

        return wait;
    }

    public async Task RegisterFailureAsync(string documentNumber, string? clientIp, CancellationToken cancellationToken = default)
    {
        var now = _dateTimeProvider.UtcNow;
        var (pairKey, documentKey) = Keys(documentNumber, clientIp);

        var pair = await GetOrCreateAsync(pairKey, now, cancellationToken);
        Increment(pair, now);

        var document = await GetOrCreateAsync(documentKey, now, cancellationToken);
        Increment(document, now);
        var extra = document.Failures - _options.MaxFailuresPerDocument;
        if (extra >= 0)
        {
            // 30 s, 1 min, 2 min, 4 min… con tope de 15 min.
            var seconds = Math.Min(FirstDocumentBlock.TotalSeconds * Math.Pow(2, Math.Min(extra, 10)), MaxDocumentBlock.TotalSeconds);
            document.BlockedUntil = now.AddSeconds(seconds);
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Limpieza ocasional de contadores viejos.
        if (Random.Shared.Next(100) == 0)
        {
            var cutoff = now - StaleAfter;
            await _context.LoginAttemptWindows
                .Where(w => w.WindowStart < cutoff && (w.BlockedUntil == null || w.BlockedUntil < now))
                .ExecuteDeleteAsync(cancellationToken);
        }
    }

    public async Task ResetAsync(string documentNumber, string? clientIp, CancellationToken cancellationToken = default)
    {
        var (pairKey, documentKey) = Keys(documentNumber, clientIp);
        await _context.LoginAttemptWindows
            .Where(w => w.Key == pairKey || w.Key == documentKey)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private async Task<LoginAttemptWindow> GetOrCreateAsync(string key, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var window = await _context.LoginAttemptWindows.FirstOrDefaultAsync(w => w.Key == key, cancellationToken);
        if (window == null)
        {
            window = new LoginAttemptWindow { Key = key, WindowStart = now };
            _context.LoginAttemptWindows.Add(window);
        }

        return window;
    }

    private void Increment(LoginAttemptWindow window, DateTimeOffset now)
    {
        var blocked = window.BlockedUntil is { } until && until > now;
        if (now >= window.WindowStart + Window && !blocked)
        {
            window.WindowStart = now;
            window.Failures = 0;
            window.BlockedUntil = null;
        }

        window.Failures++;
    }

    private static (string PairKey, string DocumentKey) Keys(string documentNumber, string? clientIp)
    {
        var document = documentNumber.Trim();
        var ip = string.IsNullOrWhiteSpace(clientIp) ? UnknownIp : clientIp;
        return (Sha256Hex($"doc-ip:{document}:{ip}"), Sha256Hex($"doc:{document}"));
    }

    private static string Sha256Hex(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
