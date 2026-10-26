namespace Ate.Application.Features.Auth.DTOs;

public record UserDto(
    Guid Id,
    string Email,
    string FullName,
    string? AvatarUrl,
    IReadOnlyList<string> Roles,
    CompanyDto? Company);
