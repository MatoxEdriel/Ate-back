namespace Ate.Api;

public static class RateLimitPolicies
{
    public const string PasswordRecovery = "password-recovery";

    /// <summary>Anonymous sign-in, per client IP (per-document limits live in the login service).</summary>
    public const string Login = "login";

    /// <summary>Endpoints that check a password of a signed-in user (guards against guessing).</summary>
    public const string Credentials = "credentials";
}
