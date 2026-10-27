using Ate.Application.Common.Interfaces;
using Ate.Application.Features.MerchantRestaurant.DTOs;
using Ate.Domain.Common;
using Ate.Domain.Entities;
using Ate.Domain.Errors;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Ate.Infrastructure.Services;

public class MerchantRestaurantService : IMerchantRestaurantService, IScopedDependency
{
    private readonly IApplicationDbContext _context;
    private readonly IValidator<UpdateMerchantRestaurantRequest> _updateValidator;

    public MerchantRestaurantService(IApplicationDbContext context, IValidator<UpdateMerchantRestaurantRequest> updateValidator)
    {
        _context = context;
        _updateValidator = updateValidator;
    }

    public async Task<Result<MerchantRestaurantDto>> GetAsync(Guid merchantUserId, CancellationToken cancellationToken = default)
    {
        var restaurant = await _context.Restaurants
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.MerchantUserId == merchantUserId, cancellationToken);

        return restaurant == null
            ? Result.Failure<MerchantRestaurantDto>(MerchantOrderErrors.RestaurantNotFound)
            : Result.Success(ToDto(restaurant));
    }

    public async Task<Result<MerchantRestaurantDto>> UpdateAsync(Guid merchantUserId, UpdateMerchantRestaurantRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return Result.Failure<MerchantRestaurantDto>(Error.Validation("MerchantRestaurant.InvalidData", "Revisa los datos del restaurante.", errors));
        }

        var restaurant = await _context.Restaurants
            .FirstOrDefaultAsync(r => r.MerchantUserId == merchantUserId, cancellationToken);

        if (restaurant == null)
        {
            return Result.Failure<MerchantRestaurantDto>(MerchantOrderErrors.RestaurantNotFound);
        }

        restaurant.Name = request.Name.Trim();
        restaurant.Cuisine = request.Cuisine.Trim();
        restaurant.Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();
        restaurant.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        restaurant.BrandColor = request.BrandColor.Trim().ToUpperInvariant();
        restaurant.ReadyMinutes = request.ReadyMinutes;

        // The DishCatalogSyncInterceptor copies the new name to MongoDB (dish_catalog) on save.
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(ToDto(restaurant));
    }

    private static MerchantRestaurantDto ToDto(Restaurant restaurant) => new(
        restaurant.Id,
        restaurant.Name,
        restaurant.Cuisine,
        restaurant.Address,
        restaurant.Phone,
        restaurant.BrandColor,
        restaurant.LogoUrl,
        restaurant.ReadyMinutes,
        restaurant.IsActive);
}
