using Ate.Application.Features.Auth.DTOs;
using Ate.Domain.Common;

namespace Ate.Application.Common.Interfaces;

public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<Result<LoginResponse>> GenerateDevTokenAsync(DevTokenRequest? request = null, CancellationToken cancellationToken = default);
}
