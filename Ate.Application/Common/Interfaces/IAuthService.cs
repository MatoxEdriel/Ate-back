using Ate.Application.Features.Auth.DTOs;
using Ate.Domain.Common;

namespace Ate.Application.Common.Interfaces;

public interface IAuthService
{
    /// <summary>
    /// Signs in with a national ID (cédula) and password. A missing document and a wrong password give
    /// the same answer; a deactivated account is only reported once the password is correct.
    /// </summary>
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, string? clientIp = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Changes the password of the signed-in user (also the mandatory change of a temporary password),
    /// revokes every other session and returns a fresh session without the change requirement.
    /// </summary>
    Task<Result<LoginResponse>> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task<Result<LoginResponse>> GenerateDevTokenAsync(DevTokenRequest? request = null, CancellationToken cancellationToken = default);
}
