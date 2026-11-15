using Ate.Application.Features.Auth.DTOs;
using Ate.Domain.Common;

namespace Ate.Application.Common.Interfaces;

public interface IAuthService
{
    /// <summary>
    /// Signs in with a national ID (cédula) and password. A missing document and a wrong password give
    /// the same answer; a deactivated account is only reported once the password is correct.
    /// </summary>
    Task<Result<AuthSession>> LoginAsync(LoginRequest request, string? clientIp = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Changes the password of the signed-in user (also the mandatory change of a temporary password),
    /// revokes every other session and returns a fresh session without the change requirement.
    /// </summary>
    Task<Result<AuthSession>> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
    /// <summary>Identidad del usuario conectado, leída de la base (la cédula ya no viaja en el token).</summary>
    /// <summary>
    /// Rota el refresh token: lo revoca y entrega una sesión nueva. Si llega un token ya usado fuera de la
    /// gracia de 30 s, revoca todas las sesiones del usuario (posible robo).
    /// </summary>
    Task<Result<AuthSession>> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default);
    Task<Result<CurrentUserDto>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<AuthSession>> GenerateDevTokenAsync(DevTokenRequest? request = null, CancellationToken cancellationToken = default);
}
