using Ate.Application.Features.Restaurants.DTOs;
using Ate.Domain.Common;

namespace Ate.Application.Common.Interfaces;

public interface IAdminRestaurantService
{
    Task<Result<IReadOnlyList<AdminRestaurantDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<AdminRestaurantDto>> UpdateStatusAsync(Guid restaurantId, UpdateRestaurantStatusRequest request, CancellationToken cancellationToken = default);
}
