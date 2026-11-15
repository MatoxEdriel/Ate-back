namespace Ate.Application.Common.Interfaces;

/// <summary>
/// Límite de intentos de login, persistente. Por cédula + IP: ventana fija. Por cédula sola: bloqueo
/// progresivo (30 s, 1 min, 2 min… hasta 15 min) para frenar ataques sin dejar a alguien fuera mucho rato.
/// </summary>
public interface ILoginAttemptLimiter
{
    /// <summary>Tiempo que falta para poder volver a intentar, o null si se permite.</summary>
    Task<TimeSpan?> GetRetryAfterAsync(string documentNumber, string? clientIp, CancellationToken cancellationToken = default);

    Task RegisterFailureAsync(string documentNumber, string? clientIp, CancellationToken cancellationToken = default);

    Task ResetAsync(string documentNumber, string? clientIp, CancellationToken cancellationToken = default);
}
