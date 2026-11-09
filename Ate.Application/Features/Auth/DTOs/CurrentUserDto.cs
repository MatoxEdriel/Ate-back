namespace Ate.Application.Features.Auth.DTOs;

/// <summary>Identity of the signed-in user, read from the validated access token.</summary>
/// <param name="Id">User identifier.</param>
/// <param name="Email">Email of the account (empty when the token has none).</param>
/// <param name="FullName">First and last names.</param>
/// <param name="DocumentNumber">National ID (cédula) as text, leading zeros kept.</param>
/// <param name="Roles">Role names granted to the user.</param>
public record CurrentUserDto(
    Guid Id,
    string Email,
    string FullName,
    string DocumentNumber,
    IReadOnlyList<string> Roles);
