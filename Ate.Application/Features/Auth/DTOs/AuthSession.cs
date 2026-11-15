namespace Ate.Application.Features.Auth.DTOs;

/// <summary>
/// Sesión emitida. <see cref="Response"/> va en el cuerpo; el refresh token NUNCA va en el cuerpo: el
/// controlador lo pone en una cookie HttpOnly.
/// </summary>
public record AuthSession(LoginResponse Response, string RefreshToken, DateTimeOffset RefreshExpiresAt);
