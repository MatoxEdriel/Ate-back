using Ate.Application.Common.Interfaces;
using System.Globalization;
using Ate.Application.Features.MerchantRestaurant.DTOs;
using Ate.Application.Features.Restaurants;
using Ate.Domain.Common;
using Ate.Domain.Entities;
using Ate.Domain.Enums;
using Ate.Domain.Errors;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Ate.Infrastructure.Services;

public class MerchantRestaurantService : IMerchantRestaurantService, IScopedDependency
{
    private readonly IApplicationDbContext _context;
    private readonly IValidator<UpdateMerchantRestaurantRequest> _updateValidator;
    private readonly IPasswordHasher _passwordHasher;

    public MerchantRestaurantService(
        IApplicationDbContext context,
        IValidator<UpdateMerchantRestaurantRequest> updateValidator,
        IPasswordHasher passwordHasher)
    {
        _context = context;
        _updateValidator = updateValidator;
        _passwordHasher = passwordHasher;
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

    /// <remarks>
    /// TEMPORAL: el logo se guarda tal cual como data URL base64; con el servidor de imágenes aquí se
    /// subirá el archivo y se guardará su URL.
    /// </remarks>
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

        if (ChangesSensitiveData(restaurant, request))
        {
            if (string.IsNullOrEmpty(request.CurrentPassword))
            {
                return Result.Failure<MerchantRestaurantDto>(MerchantRestaurantErrors.PasswordRequired);
            }

            var passwordHash = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == merchantUserId)
                .Select(u => u.PasswordHash)
                .FirstOrDefaultAsync(cancellationToken);

            if (string.IsNullOrEmpty(passwordHash) || !_passwordHasher.VerifyPassword(request.CurrentPassword, passwordHash))
            {
                return Result.Failure<MerchantRestaurantDto>(MerchantRestaurantErrors.InvalidPassword);
            }
        }

        restaurant.Name = request.Name.Trim();
        restaurant.Cuisine = request.Cuisine.Trim();
        restaurant.Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();
        restaurant.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        restaurant.BrandColor = request.BrandColor.Trim().ToUpperInvariant();
        restaurant.ReadyMinutes = request.ReadyMinutes;
        if (request.Logo != null)
        {
            restaurant.LogoUrl = request.Logo.Length == 0 ? null : request.Logo;
        }
        ApplyCutoff(restaurant, request.Cutoff);
        restaurant.Latitude = request.Latitude.HasValue ? Math.Round(request.Latitude.Value, 6) : null;
        restaurant.Longitude = request.Longitude.HasValue ? Math.Round(request.Longitude.Value, 6) : null;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(ToDto(restaurant));
    }

    /// <summary>
    /// What employees use to find the venue (name, phone, address, map location and logo):
    /// changing it requires the merchant's password. Cuisine, color, times and cutoff do not.
    /// </summary>
    private static bool ChangesSensitiveData(Restaurant restaurant, UpdateMerchantRestaurantRequest request)
    {
        static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        static decimal? Round(decimal? value) => value.HasValue ? Math.Round(value.Value, 6) : null;

        return request.Name.Trim() != restaurant.Name
            || Normalize(request.Address) != restaurant.Address
            || Normalize(request.Phone) != restaurant.Phone
            || Round(request.Latitude) != restaurant.Latitude
            || Round(request.Longitude) != restaurant.Longitude
            || (request.Logo != null && (request.Logo.Length == 0 ? null : request.Logo) != restaurant.LogoUrl);
    }

    private static void ApplyCutoff(Restaurant restaurant, IReadOnlyDictionary<string, string>? cutoff)
    {
        if (cutoff == null)
        {
            return;
        }

        foreach (var (code, value) in cutoff)
        {
            MealTypeCodes.TryParse(code, out var mealType);
            var time = TimeOnly.ParseExact(value.Trim(), "HH:mm", CultureInfo.InvariantCulture);

            switch (mealType)
            {
                case MealType.Breakfast:
                    restaurant.BreakfastCutoff = time;
                    break;
                case MealType.Lunch:
                    restaurant.LunchCutoff = time;
                    break;
                default:
                    restaurant.DinnerCutoff = time;
                    break;
            }
        }
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
        restaurant.IsActive,
        restaurant.Latitude,
        restaurant.Longitude,
        BuildCutoff(restaurant));

    private static Dictionary<string, string> BuildCutoff(Restaurant restaurant)
    {
        var cutoff = new Dictionary<string, string>();
        foreach (var mealType in Enum.GetValues<MealType>())
        {
            var time = restaurant.GetCutoff(mealType);
            if (time.HasValue)
            {
                cutoff[MealTypeCodes.ToCode(mealType)] = time.Value.ToString("HH:mm", CultureInfo.InvariantCulture);
            }
        }

        return cutoff;
    }
}
