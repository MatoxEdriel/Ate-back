namespace Ate.Application.Features.Companies.DTOs;

public record CreateCompanyRequest(
    string BusinessName,
    string TradeName,
    string TaxId,
    string? Email,
    string? Phone,
    string? Address,
    string AdminDocumentNumber,
    string AdminFirstName,
    string AdminLastName,
    string AdminEmail,
    decimal? Latitude = null,
    decimal? Longitude = null,
    string? Logo = null);
