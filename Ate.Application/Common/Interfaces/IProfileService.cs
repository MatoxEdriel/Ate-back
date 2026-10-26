using Ate.Application.Features.Profile.DTOs;
using Ate.Domain.Common;

namespace Ate.Application.Common.Interfaces;

public interface IProfileService
{
    Task<Result<ProfileDto>> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);
}
