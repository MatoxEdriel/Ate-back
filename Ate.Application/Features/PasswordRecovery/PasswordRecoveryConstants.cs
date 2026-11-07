namespace Ate.Application.Features.PasswordRecovery;

public static class PasswordRecoveryConstants
{
    /// <summary>Digits of the OTP code (the client renders this many boxes).</summary>
    public const int CodeLength = 5;

    public const int MinPasswordLength = 8;
    public const int MaxPasswordLength = 128;
    public const int MaxEmailLength = 254;
}
