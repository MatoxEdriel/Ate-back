namespace Ate.Application.Common.Security;

/// <summary>
/// Reglas de contraseña (OWASP ASVS V2.1 / ISO 27001 A.5.17): largo mínimo de 12, sin contraseñas comunes,
/// y las temporales vencen a las 72 horas.
/// </summary>
public static class PasswordPolicy
{
    public const int MinLength = 12;
    public const int MaxLength = 128;

    public static readonly TimeSpan TemporaryPasswordLifetime = TimeSpan.FromHours(72);

    public const string TooShortMessage = "La contraseña debe tener al menos 12 caracteres.";
    public const string TooLongMessage = "La contraseña puede tener como máximo 128 caracteres.";
    public const string CommonMessage = "Esa contraseña es muy común. Elige otra.";

    public static bool IsCommon(string? password) =>
        !string.IsNullOrEmpty(password) && CommonPasswords.Contains(password.Trim());
}
