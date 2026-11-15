namespace Ate.Application.Features.Auth.DTOs;

/// <summary>Access token de vida corta (15 min). El refresh token viaja solo en la cookie HttpOnly.</summary>
public record LoginResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    UserDto User);
