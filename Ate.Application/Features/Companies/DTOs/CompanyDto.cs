namespace Ate.Application.Features.Companies.DTOs;

public record CompanyDto(
    Guid Id,
    string BusinessName,
    string TradeName,
    string TaxId,
    string? Email,
    string? Phone,
    string? Address,
    string? LogoUrl,
    decimal? Latitude,
    decimal? Longitude,
    bool IsActive,
    int EmployeeCount,
    string? AdminName,
    string? AdminEmail,
    DateTimeOffset CreatedAt);
