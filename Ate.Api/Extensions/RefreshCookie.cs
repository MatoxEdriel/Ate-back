using Ate.Application.Features.Auth.DTOs;

namespace Ate.Api.Extensions;

/// <summary>
/// Cookie del refresh token: HttpOnly (el JavaScript no la lee, así un XSS no se la lleva), Secure,
/// SameSite=Strict y solo enviada a /api/v1/auth.
/// </summary>
public static class RefreshCookie
{
    public const string Name = "ate_rt";
    private const string CookiePath = "/api/v1/auth";

    public static void Set(HttpResponse response, AuthSession session)
    {
        // Sin refresh token nuevo (repetición dentro de la gracia): se deja la cookie que ya tiene el navegador.
        if (session.RefreshToken is { } token && session.RefreshExpiresAt is { } expires)
        {
            response.Cookies.Append(Name, token, Options(expires));
        }
    }

    public static string? Read(HttpRequest request) =>
        request.Cookies.TryGetValue(Name, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

    public static void Clear(HttpResponse response) =>
        response.Cookies.Delete(Name, Options(DateTimeOffset.UnixEpoch));

    private static CookieOptions Options(DateTimeOffset expires) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = CookiePath,
        Expires = expires,
        IsEssential = true,
    };
}
