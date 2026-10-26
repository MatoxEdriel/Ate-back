namespace Ate.Application.Features.Profile.DTOs;

public record ProfileCompanyDto(
    Guid Id,
    string BusinessName,
    string TradeName,
    string TaxId,
    string? Email,
    string? Phone,
    string? Address,
    string? LogoUrl,
    bool IsActive);
