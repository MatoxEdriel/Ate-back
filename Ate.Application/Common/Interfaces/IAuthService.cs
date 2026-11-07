using Ate.Application.Features.Auth.DTOs;
using Ate.Domain.Common;

namespace Ate.Application.Common.Interfaces;

public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Changes the password of the signed-in user (also the mandatory change of a temporary password),
    /// revokes every other session and returns a fresh session without the change requirement.
    /// </summary>
    Task<Result<LoginResponse>> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task<Result<LoginResponse>> GenerateDevTokenAsync(DevTokenRequest? request = null, CancellationToken cancellationToken = default);
}
