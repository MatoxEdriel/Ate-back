namespace Ate.Application.Features.MerchantRestaurant.DTOs;

/// <summary>Ficha del restaurante tal como la administra el comercio aliado.</summary>
public record MerchantRestaurantDto(
    Guid Id,
    string Name,
    string Cuisine,
    string? Address,
    string? Phone,
    string BrandColor,
    string? LogoUrl,
    int ReadyMinutes,
    bool IsActive,
    decimal? Latitude,
    decimal? Longitude,
    IReadOnlyDictionary<string, string> Cutoff);

public record UpdateMerchantRestaurantRequest(
    string Name,
    string Cuisine,
    string? Address,
    string? Phone,
    string BrandColor,
    int ReadyMinutes,
    decimal? Latitude = null,
    decimal? Longitude = null,
    IReadOnlyDictionary<string, string>? Cutoff = null,
    // TEMPORAL: logo serializado como data URL base64 (png/webp/jpeg) hasta tener el servidor de imágenes.
    // null = no cambia, "" = quita el logo, data URL = lo reemplaza.
    string? Logo = null,
    // Required only when name, phone, address, location or logo change (sensitive data of the venue).
    string? CurrentPassword = null);
