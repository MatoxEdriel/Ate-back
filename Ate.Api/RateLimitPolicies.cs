namespace Ate.Api;

public static class RateLimitPolicies
{
    public const string PasswordRecovery = "password-recovery";

    /// <summary>Endpoints that check a password of a signed-in user (guards against guessing).</summary>
    public const string Credentials = "credentials";
}
