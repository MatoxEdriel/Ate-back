namespace Ate.Application.Features.Auth.DTOs;

/// <summary>Change the password of the signed-in user (also used for the mandatory first change).</summary>
/// <param name="CurrentPassword">
/// Required for a regular change. Not needed for the mandatory change of a temporary password:
/// the session was just opened with it, and the new one may not be the same.
/// </param>
public record ChangePasswordRequest(string NewPassword, string? CurrentPassword = null);
