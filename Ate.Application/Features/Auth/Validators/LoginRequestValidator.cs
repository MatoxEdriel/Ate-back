using Ate.Application.Features.Auth.DTOs;
using FluentValidation;

namespace Ate.Application.Features.Auth.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.DocumentNumber)
            .NotEmpty()
            .WithMessage("Ingresa tu cédula o pasaporte.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Ingresa tu contraseña.");
    }
}
