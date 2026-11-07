using System.ComponentModel.DataAnnotations;
using Ate.Application.Common.Constants;

namespace Ate.Infrastructure.Services;

public class PasswordRecoveryOptions
{
    public const string SectionName = ConfigurationConstants.PasswordRecoverySettings.SectionName;

    /// <summary>How long the emailed OTP code is valid.</summary>
    [Range(1, 60)]
    public int CodeLifetimeMinutes { get; set; } = 10;

    /// <summary>How long the reset token (issued after verifying the code) is valid.</summary>
    [Range(1, 60)]
    public int ResetTokenLifetimeMinutes { get; set; } = 10;

    /// <summary>Wrong codes allowed before the code is discarded.</summary>
    [Range(1, 20)]
    public int MaxAttempts { get; set; } = 5;

    /// <summary>Minimum time between two codes for the same account.</summary>
    [Range(0, 3600)]
    public int ResendCooldownSeconds { get; set; } = 30;

    /// <summary>Codes allowed per account in a rolling hour.</summary>
    [Range(1, 100)]
    public int MaxCodesPerHour { get; set; } = 5;
}
