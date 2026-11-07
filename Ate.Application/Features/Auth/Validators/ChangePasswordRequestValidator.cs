using Ate.Application.Features.Auth.DTOs;
using FluentValidation;

namespace Ate.Application.Features.Auth.Validators;

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public const int MinPasswordLength = 8;
    public const int MaxPasswordLength = 128;

    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Ingresa tu nueva contraseña.")
            .MinimumLength(MinPasswordLength)
            .WithMessage($"La contraseña debe tener al menos {MinPasswordLength} caracteres.")
            .MaximumLength(MaxPasswordLength)
            .WithMessage($"La contraseña puede tener como máximo {MaxPasswordLength} caracteres.")
            .Must((request, newPassword) => string.IsNullOrEmpty(request.CurrentPassword) || newPassword != request.CurrentPassword)
            .WithMessage("La nueva contraseña debe ser distinta de la actual.");
    }
}
