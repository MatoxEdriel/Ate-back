namespace Ate.Domain.Entities;

/// <summary>
/// Contador de intentos fallidos de login. La clave es un SHA-256 (de la cédula, o de la cédula + IP):
/// nunca se guarda la cédula. Vive en la base para sobrevivir a reinicios y compartirse entre instancias.
/// </summary>
public class LoginAttemptWindow
{
    public string Key { get; set; } = string.Empty;
    public int Failures { get; set; }
    public DateTimeOffset WindowStart { get; set; }
    /// <summary>Bloqueo progresivo por cédula (null si no está bloqueada).</summary>
    public DateTimeOffset? BlockedUntil { get; set; }
}
