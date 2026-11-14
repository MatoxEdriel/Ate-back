namespace Ate.Application.Features.Restaurants.DTOs;

public record AdminRestaurantDto(
    Guid Id,
    string Code,
    string? TaxId,
    string Name,
    string Cuisine,
    string? Address,
    string? LogoUrl,
    decimal? Latitude,
    decimal? Longitude,
    bool IsActive,
    int ActiveAgreements,
    DateTimeOffset CreatedAt);

public record UpdateRestaurantStatusRequest(bool IsActive, string? CurrentPassword = null);
