namespace Ate.Application.Features.Profile.DTOs;

/// <summary>
/// Company shown in the profile. Employees only receive the public identity of the company
/// (names, logo and status); tax id and contact details are sent to the company admin only.
/// </summary>
public record ProfileCompanyDto(
    Guid Id,
    string BusinessName,
    string TradeName,
    string? TaxId,
    string? Email,
    string? Phone,
    string? Address,
    string? LogoUrl,
    bool IsActive);
