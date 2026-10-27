using Ate.Application.Features.Restaurants.DTOs;
using Ate.Domain.Common;

namespace Ate.Application.Common.Interfaces;

public interface IRestaurantService
{
    Task<Result<IReadOnlyList<RestaurantDto>>> GetForEmployeeAsync(Guid userId, CancellationToken cancellationToken = default);
}
