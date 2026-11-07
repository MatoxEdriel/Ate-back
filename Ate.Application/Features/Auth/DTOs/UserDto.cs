namespace Ate.Application.Features.Auth.DTOs;

/// <param name="MustChangePassword">
/// True when the account still has a temporary password: the client must ask for a new one
/// before anything else (the API answers 403 "Auth.PasswordChangeRequired" meanwhile).
/// </param>
public record UserDto(
    Guid Id,
    string Email,
    string FullName,
    string? AvatarUrl,
    IReadOnlyList<string> Roles,
    CompanyDto? Company,
    bool MustChangePassword);
