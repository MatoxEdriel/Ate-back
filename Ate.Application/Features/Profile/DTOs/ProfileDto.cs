namespace Ate.Application.Features.Profile.DTOs;

public record ProfileDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string FullName,
    string DocumentType,
    string DocumentNumber,
    string? PhoneNumber,
    string? AvatarUrl,
    IReadOnlyList<string> Roles,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt,
    ProfileCompanyDto? Company);
