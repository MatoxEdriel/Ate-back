namespace Ate.Application.Features.Auth.DTOs;

/// <summary>
/// Sesión emitida. <see cref="Response"/> va en el cuerpo; el refresh token NUNCA va en el cuerpo: el
/// controlador lo pone en una cookie HttpOnly. <see cref="RefreshToken"/> es null cuando solo se renovó el
/// access token (repetición dentro de la gracia de rotación): la cookie vigente no se toca.
/// </summary>
public record AuthSession(LoginResponse Response, string? RefreshToken, DateTimeOffset? RefreshExpiresAt);
