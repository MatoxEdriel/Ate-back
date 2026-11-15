using Ate.Application.Features.Auth.DTOs;
using Ate.Application.Common.Security;
using FluentValidation;

namespace Ate.Application.Features.Auth.Validators;

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Ingresa tu nueva contraseña.")
            .MinimumLength(PasswordPolicy.MinLength)
            .WithMessage(PasswordPolicy.TooShortMessage)
            .MaximumLength(PasswordPolicy.MaxLength)
            .WithMessage(PasswordPolicy.TooLongMessage)
            .Must(password => !PasswordPolicy.IsCommon(password))
            .WithMessage(PasswordPolicy.CommonMessage)
            .Must((request, newPassword) => string.IsNullOrEmpty(request.CurrentPassword) || newPassword != request.CurrentPassword)
            .WithMessage("La nueva contraseña debe ser distinta de la actual.");
    }
}
