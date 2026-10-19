using Ate.Application.Features.Auth.DTOs;
using FluentValidation;

namespace Ate.Application.Features.Auth.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.DocumentNumber)
            .NotEmpty()
            .WithMessage("Document number (cédula) is required.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required.");
    }
}
