namespace Ate.Application.Features.Companies.DTOs;

public record UpdateCompanyStatusRequest(bool IsActive, string? CurrentPassword = null);
