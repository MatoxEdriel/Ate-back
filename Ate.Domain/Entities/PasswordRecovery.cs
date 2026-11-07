namespace Ate.Domain.Entities;

/// <summary>
/// One password recovery attempt: an OTP code sent by email and, once verified, a single-use reset token.
/// Only hashes are stored. Both secrets carry their own expiration ("time token").
/// </summary>
public class PasswordRecovery
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string CodeHash { get; set; } = string.Empty;
    public DateTimeOffset CodeExpiresAt { get; set; }
    public int Attempts { get; set; }
    public DateTimeOffset? CodeVerifiedAt { get; set; }

    public string? ResetTokenHash { get; set; }
    public DateTimeOffset? ResetTokenExpiresAt { get; set; }

    /// <summary>The password was changed with this recovery.</summary>
    public DateTimeOffset? UsedAt { get; set; }

    /// <summary>Discarded: a newer code was requested, too many attempts or it expired.</summary>
    public DateTimeOffset? InvalidatedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public bool IsClosed => UsedAt.HasValue || InvalidatedAt.HasValue;

    public bool IsCodeExpired(DateTimeOffset now) => now >= CodeExpiresAt;

    public bool IsResetTokenExpired(DateTimeOffset now) =>
        !ResetTokenExpiresAt.HasValue || now >= ResetTokenExpiresAt.Value;
}
