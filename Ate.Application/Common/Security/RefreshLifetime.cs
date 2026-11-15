using Ate.Domain.Constants;

namespace Ate.Application.Common.Security;

/// <summary>
/// Duración deslizante de la sesión (refresh token) según el rol. El colaborador entra a diario a pedir y
/// solo puede pedir comida: 30 días. Quien maneja datos de otros (RRHH, restaurante, SuperAdmin): 7 días.
/// Con varios roles gana el más corto.
/// </summary>
public static class RefreshLifetime
{
    public static readonly TimeSpan Employee = TimeSpan.FromDays(30);
    public static readonly TimeSpan Privileged = TimeSpan.FromDays(7);

    /// <summary>Tiempo durante el que se acepta un token recién rotado (dos pestañas que refrescan a la vez).</summary>
    public static readonly TimeSpan RotationGrace = TimeSpan.FromSeconds(30);

    public static TimeSpan For(IReadOnlyCollection<string> roles) =>
        roles.Count > 0 && roles.All(role => role == RoleConstants.Employee) ? Employee : Privileged;
}
