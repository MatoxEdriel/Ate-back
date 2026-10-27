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
    bool IsActive);

public record UpdateMerchantRestaurantRequest(
    string Name,
    string Cuisine,
    string? Address,
    string? Phone,
    string BrandColor,
    int ReadyMinutes);
