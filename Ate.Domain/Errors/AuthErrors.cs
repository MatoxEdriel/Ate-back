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

    public static readonly Error InvalidToken =
        Error.Unauthorized("Auth.InvalidToken", "No pudimos verificar tu identidad. Vuelve a iniciar sesión.");

    public static readonly Error MissingCredentials =
        Error.Validation("Auth.MissingCredentials", "Ingresa tu cédula y contraseña.");
}
