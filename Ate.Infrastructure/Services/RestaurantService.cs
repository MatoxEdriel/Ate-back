using Ate.Application.Common.Interfaces;
using Ate.Application.Features.Restaurants;
using Ate.Application.Features.Restaurants.DTOs;
using Ate.Domain.Common;
using Ate.Domain.Entities;
using Ate.Domain.Enums;
using Ate.Domain.Errors;
using Microsoft.EntityFrameworkCore;

namespace Ate.Infrastructure.Services;

public class RestaurantService : IRestaurantService, IScopedDependency
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RestaurantService(IApplicationDbContext context, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<IReadOnlyList<RestaurantDto>>> GetForEmployeeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var companyId = await _context.Employees
            .AsNoTracking()
            .Where(e => e.Person.UserId == userId && e.IsActive)
            .Select(e => (Guid?)e.CompanyId)
            .FirstOrDefaultAsync(cancellationToken);

        if (!companyId.HasValue)
        {
            return Result.Failure<IReadOnlyList<RestaurantDto>>(EmployeeOrderErrors.EmployeeNotFound);
        }

        var today = DateOnly.FromDateTime(_dateTimeProvider.BusinessNow.DateTime);

        var agreements = await _context.CompanyRestaurantAgreements
            .AsNoTracking()
            .Where(a => a.CompanyId == companyId.Value
                && a.IsActive
                && a.StartsOn <= today
                && (a.EndsOn == null || a.EndsOn >= today)
                && a.Restaurant.IsActive)
            .Include(a => a.Restaurant)
                .ThenInclude(r => r.Dishes.Where(d => d.IsAvailable).OrderBy(d => d.SortOrder).ThenBy(d => d.Name))
            .OrderBy(a => a.DistanceM)
            .ThenBy(a => a.Restaurant.Name)
            .ToListAsync(cancellationToken);

        IReadOnlyList<RestaurantDto> result = agreements.Select(ToDto).ToList();
        return Result.Success(result);
    }

    private static RestaurantDto ToDto(CompanyRestaurantAgreement agreement)
    {
        var restaurant = agreement.Restaurant;

        var cutoff = new Dictionary<string, string>();
        foreach (var mealType in Enum.GetValues<MealType>())
        {
            var time = restaurant.GetCutoff(mealType);
            if (time.HasValue)
            {
                cutoff[MealTypeCodes.ToCode(mealType)] = time.Value.ToString("HH:mm");
            }
        }

        var menu = restaurant.Dishes
            .GroupBy(d => d.MealType)
            .OrderBy(g => g.Key)
            .ToDictionary(
                g => MealTypeCodes.ToCode(g.Key),
                g => (IReadOnlyList<RestaurantDishDto>)g
                    .Select(d => new RestaurantDishDto(d.Id, d.Name, d.Description, d.Protein, d.HasRice, d.DefaultDrink, d.Price))
                    .ToList());

        return new RestaurantDto(
            restaurant.Id,
            restaurant.Name,
            restaurant.Cuisine,
            agreement.DistanceM,
            restaurant.ReadyMinutes,
            restaurant.BrandColor,
            restaurant.LogoUrl,
            restaurant.Icon,
            cutoff,
            menu);
    }
}
