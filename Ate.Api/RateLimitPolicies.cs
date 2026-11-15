namespace Ate.Api;

public static class RateLimitPolicies
{
    public const string PasswordRecovery = "password-recovery";

    /// <summary>Anonymous sign-in, per client IP (per-document limits live in the login service).</summary>
    public const string Login = "login";

    /// <summary>
    /// Renovación de sesión. Una oficina entera comparte IP, así que no se limita por IP sino por cookie de
    /// sesión (un atacante sin cookie cae al límite por IP, más alto que el del login).
    /// </summary>
    public const string Refresh = "refresh";

    /// <summary>Endpoints that check a password of a signed-in user (guards against guessing).</summary>
    public const string Credentials = "credentials";
}
