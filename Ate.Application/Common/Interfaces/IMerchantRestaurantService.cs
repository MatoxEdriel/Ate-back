using Ate.Application.Features.MerchantRestaurant.DTOs;
using Ate.Domain.Common;

namespace Ate.Application.Common.Interfaces;

public interface IMerchantRestaurantService
{
    Task<Result<MerchantRestaurantDto>> GetAsync(Guid merchantUserId, CancellationToken cancellationToken = default);
    Task<Result<MerchantRestaurantDto>> UpdateAsync(Guid merchantUserId, UpdateMerchantRestaurantRequest request, CancellationToken cancellationToken = default);
}
