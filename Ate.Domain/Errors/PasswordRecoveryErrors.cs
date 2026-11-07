using Ate.Domain.Common;

namespace Ate.Domain.Errors;

public static class PasswordRecoveryErrors
{
    public static readonly Error InvalidCode =
        Error.Validation("PasswordRecovery.InvalidCode", "El código no es correcto. Revísalo e inténtalo de nuevo.", details: null);

    public static readonly Error CodeExpired =
        Error.Validation("PasswordRecovery.CodeExpired", "El código expiró. Solicita uno nuevo.", details: null);

    public static readonly Error TooManyAttempts =
        Error.TooManyRequests("PasswordRecovery.TooManyAttempts", "Superaste el número de intentos. Solicita un código nuevo.");

    public static readonly Error InvalidResetToken =
        Error.Validation("PasswordRecovery.InvalidResetToken", "Tu sesión de recuperación no es válida. Solicita un código nuevo.", details: null);

    public static readonly Error ResetTokenExpired =
        Error.Validation("PasswordRecovery.ResetTokenExpired", "Tu sesión de recuperación expiró. Solicita un código nuevo.", details: null);
}
