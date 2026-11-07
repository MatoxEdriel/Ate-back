namespace Ate.Application.Features.PasswordRecovery.DTOs;

/// <summary>Step 1: ask for a code. Always answers the same, whether or not the email exists.</summary>
public record RequestPasswordRecoveryRequest(string Email);

/// <summary>Step 2: check the OTP code sent by email.</summary>
public record VerifyPasswordRecoveryCodeRequest(string Email, string Code);

/// <summary>Single-use token that authorizes step 3, valid until <see cref="ExpiresAt"/> (UTC).</summary>
public record PasswordResetTokenDto(string ResetToken, DateTimeOffset ExpiresAt);

/// <summary>Step 3: set the new password with the token returned by step 2.</summary>
public record ResetPasswordRequest(string ResetToken, string NewPassword);
