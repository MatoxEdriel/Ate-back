using Ate.Application.Features.Achievements.DTOs;
using Ate.Domain.Common;

namespace Ate.Application.Common.Interfaces;

public interface IAchievementService
{
    /// <summary>Recomputes the user's progress from the order history; returns how many trophies were unlocked now.</summary>
    Task<int> EvaluateAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Result<AchievementsOverviewDto>> GetOverviewAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Unlocked trophies whose toast has not been shown yet.</summary>
    Task<Result<IReadOnlyList<AchievementDto>>> GetUnseenAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Result<int>> MarkSeenAsync(Guid userId, MarkAchievementsSeenRequest request, CancellationToken cancellationToken = default);
}
