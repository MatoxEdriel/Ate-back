namespace Ate.Domain.Constants;

/// <summary>Por qué se revocó un refresh token (columna revocation_reason).</summary>
public static class RevocationReasons
{
    public const string Rotated = "rotated";
    public const string Logout = "logout";
    public const string Password = "password";
    public const string Reuse = "reuse";
}
