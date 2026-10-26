namespace Ate.Application.Features.Auth.DTOs;

public record CompanyDto(
    Guid Id,
    string Name,
    string? LogoUrl);
