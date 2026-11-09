using Ate.Domain.Common;

namespace Ate.Domain.Errors;

public static class AuthErrors
{
    public static readonly Error UserNotFound =
        Error.Unauthorized("Auth.UserNotFound", "Cédula o contraseña incorrecta.");

    public static readonly Error InvalidCredentials =
        Error.Unauthorized("Auth.InvalidCredentials", "Cédula o contraseña incorrecta.");

    public static readonly Error UserInactive =
        Error.Forbidden("Auth.UserInactive", "Tu cuenta está desactivada o bloqueada. Comunícate con un administrador.");

    public static Error TooManyLoginAttempts(TimeSpan retryAfter)
    {
        var minutes = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalMinutes));
        var unit = minutes == 1 ? "minuto" : "minutos";
        return Error.TooManyRequests(
            "Auth.TooManyLoginAttempts",
            $"Demasiados intentos de inicio de sesión. Espera {minutes} {unit} e inténtalo de nuevo.",
            retryAfter);
    }

    public static readonly Error InvalidToken =
        Error.Unauthorized("Auth.InvalidToken", "No pudimos verificar tu identidad. Vuelve a iniciar sesión.");

    public static readonly Error InvalidCurrentPassword =
        Error.Validation("Auth.InvalidCurrentPassword", "La contraseña actual no es correcta.", details: null);

    public static readonly Error PasswordReused =
        Error.Validation("Auth.PasswordReused", "La nueva contraseña debe ser distinta de la temporal.", details: null);

    public static readonly Error PasswordChangeRequired =
        Error.Forbidden("Auth.PasswordChangeRequired", "Debes cambiar tu contraseña temporal antes de continuar.");

    public static readonly Error MissingCredentials =
        Error.Validation("Auth.MissingCredentials", "Ingresa tu cédula y contraseña.");
}
