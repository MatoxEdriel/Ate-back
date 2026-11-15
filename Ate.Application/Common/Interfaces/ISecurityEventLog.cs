namespace Ate.Application.Common.Interfaces;

/// <summary>
/// Registro de auditoría de eventos de seguridad (ISO 27001 A.8.15 / OWASP A09). Nunca recibe contraseñas,
/// tokens, cédulas ni correos: solo el tipo, quién (id), el resultado y un detalle técnico corto.
/// La IP la toma del request actual. Un fallo al registrar nunca rompe la operación.
/// </summary>
public interface ISecurityEventLog
{
    Task RecordAsync(string eventType, Guid? userId, string outcome, string? detail = null, CancellationToken cancellationToken = default);
}
