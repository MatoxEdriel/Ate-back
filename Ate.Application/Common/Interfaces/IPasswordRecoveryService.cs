using Ate.Application.Features.PasswordRecovery.DTOs;
using Ate.Domain.Common;

namespace Ate.Application.Common.Interfaces;

public interface IPasswordRecoveryService
{
    /// <summary>Sends an OTP code to the email if it belongs to an active account. Never reveals whether it does.</summary>
    Task<Result> RequestCodeAsync(RequestPasswordRecoveryRequest request, CancellationToken cancellationToken = default);

    /// <summary>Checks the code (expiration and attempts) and returns a short-lived, single-use reset token.</summary>
    Task<Result<PasswordResetTokenDto>> VerifyCodeAsync(VerifyPasswordRecoveryCodeRequest request, CancellationToken cancellationToken = default);

    /// <summary>Sets the new password if the reset token is valid and not expired, and signs out every session.</summary>
    Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
}
