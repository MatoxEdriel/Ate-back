namespace Ate.Application.Features.RestaurantBadges.DTOs;

public record RestaurantBadgeDto(string Code, string Name, string Description, string Icon);

public record MerchantBadgeSummaryDto(
    string Code,
    string Name,
    string Description,
    string Icon,
    bool IsHeld,
    DateTimeOffset? EarnedAt,
    int Positive90d,
    int Negative90d);
