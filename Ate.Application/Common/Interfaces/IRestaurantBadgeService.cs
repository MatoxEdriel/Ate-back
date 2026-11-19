using Ate.Application.Features.RestaurantBadges.DTOs;
using Ate.Domain.Common;

namespace Ate.Application.Common.Interfaces;

public interface IRestaurantBadgeService
{
    Task RecalculateAsync(Guid restaurantId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, IReadOnlyList<RestaurantBadgeDto>>> GetHeldAsync(
        IReadOnlyCollection<Guid> restaurantIds, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<MerchantBadgeSummaryDto>>> GetMerchantSummaryAsync(
        Guid merchantUserId, CancellationToken cancellationToken = default);
}
