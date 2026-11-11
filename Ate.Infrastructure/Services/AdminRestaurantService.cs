using Ate.Application.Common.Interfaces;
using Ate.Application.Features.Restaurants.DTOs;
using Ate.Domain.Common;
using Ate.Domain.Errors;
using Microsoft.EntityFrameworkCore;

namespace Ate.Infrastructure.Services;

public class AdminRestaurantService : IAdminRestaurantService, IScopedDependency
{
    private readonly IApplicationDbContext _context;

    public AdminRestaurantService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<IReadOnlyList<AdminRestaurantDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var agreementCounts = await _context.CompanyRestaurantAgreements
            .AsNoTracking()
            .Where(a => a.IsActive)
            .GroupBy(a => a.RestaurantId)
            .Select(g => new { RestaurantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RestaurantId, x => x.Count, cancellationToken);

        var restaurants = await _context.Restaurants
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new { r.Id, r.Code, r.Name, r.Cuisine, r.Address, r.LogoUrl, r.Latitude, r.Longitude, r.IsActive, r.CreatedAt })
            .ToListAsync(cancellationToken);

        IReadOnlyList<AdminRestaurantDto> result = restaurants
            .Select(r => new AdminRestaurantDto(
                r.Id, r.Code, r.Name, r.Cuisine, r.Address, r.LogoUrl, r.Latitude, r.Longitude, r.IsActive,
                agreementCounts.GetValueOrDefault(r.Id), r.CreatedAt))
            .ToList();

        return Result.Success(result);
    }

    public async Task<Result<AdminRestaurantDto>> UpdateStatusAsync(Guid restaurantId, UpdateRestaurantStatusRequest request, CancellationToken cancellationToken = default)
    {
        var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.Id == restaurantId, cancellationToken);
        if (restaurant == null)
        {
            return Result.Failure<AdminRestaurantDto>(AdminRestaurantErrors.NotFound);
        }

        restaurant.IsActive = request.IsActive;
        await _context.SaveChangesAsync(cancellationToken);

        var agreements = await _context.CompanyRestaurantAgreements
            .CountAsync(a => a.RestaurantId == restaurantId && a.IsActive, cancellationToken);

        return Result.Success(new AdminRestaurantDto(
            restaurant.Id, restaurant.Code, restaurant.Name, restaurant.Cuisine, restaurant.Address,
            restaurant.LogoUrl, restaurant.Latitude, restaurant.Longitude, restaurant.IsActive, agreements, restaurant.CreatedAt));
    }
}
