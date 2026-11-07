using Ate.Application.Features.PasswordRecovery.DTOs;
using FluentValidation;

namespace Ate.Application.Features.PasswordRecovery.Validators;

internal static class PasswordRecoveryRules
{
    /// <summary>Same format rule as the client: local@domain.tld, no spaces, at most 254 characters.</summary>
    public const string EmailPattern = @"^[^\s@]+@[^\s@]+\.[^\s@]{2,}$";

    /// <summary>Bounded evaluation time: user input never runs an unbounded regex.</summary>
    private static readonly System.Text.RegularExpressions.Regex EmailRegex = new(
        EmailPattern,
        System.Text.RegularExpressions.RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    private static bool MatchesEmail(string email)
    {
        try
        {
            return EmailRegex.IsMatch(email);
        }
        catch (System.Text.RegularExpressions.RegexMatchTimeoutException)
        {
            return false;
        }
    }

    public static IRuleBuilderOptions<T, string> ValidRecoveryEmail<T>(this IRuleBuilderInitial<T, string> rule) =>
        rule.Cascade(CascadeMode.Stop)
            .Must(email => !string.IsNullOrWhiteSpace(email))
            .WithMessage("Ingresa tu correo electrónico.")
            .Must(email => email.Trim().Length <= PasswordRecoveryConstants.MaxEmailLength)
            .WithMessage("Ingresa un correo electrónico válido.")
            .Must(email => MatchesEmail(email.Trim()))
            .WithMessage("Ingresa un correo electrónico válido.");
}

public class RequestPasswordRecoveryRequestValidator : AbstractValidator<RequestPasswordRecoveryRequest>
{
    public RequestPasswordRecoveryRequestValidator()
    {
        RuleFor(x => x.Email).ValidRecoveryEmail();
    }
}

public class VerifyPasswordRecoveryCodeRequestValidator : AbstractValidator<VerifyPasswordRecoveryCodeRequest>
{
    public VerifyPasswordRecoveryCodeRequestValidator()
    {
        RuleFor(x => x.Email).ValidRecoveryEmail();

        RuleFor(x => x.Code)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage($"Ingresa los {PasswordRecoveryConstants.CodeLength} dígitos del código.")
            .Matches($"^\\d{{{PasswordRecoveryConstants.CodeLength}}}$")
            .WithMessage($"El código tiene {PasswordRecoveryConstants.CodeLength} dígitos.");
    }
}

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.ResetToken)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Tu sesión de recuperación no es válida. Solicita un código nuevo.")
            .MaximumLength(128)
            .WithMessage("Tu sesión de recuperación no es válida. Solicita un código nuevo.");

        RuleFor(x => x.NewPassword)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Ingresa tu nueva contraseña.")
            .MinimumLength(PasswordRecoveryConstants.MinPasswordLength)
            .WithMessage($"La contraseña debe tener al menos {PasswordRecoveryConstants.MinPasswordLength} caracteres.")
            .MaximumLength(PasswordRecoveryConstants.MaxPasswordLength)
            .WithMessage($"La contraseña puede tener como máximo {PasswordRecoveryConstants.MaxPasswordLength} caracteres.");
    }
}
