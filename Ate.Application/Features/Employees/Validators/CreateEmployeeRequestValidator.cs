using Ate.Application.Features.Employees.DTOs;
using FluentValidation;

namespace Ate.Application.Features.Employees.Validators;

public class CreateEmployeeRequestValidator : AbstractValidator<CreateEmployeeRequest>
{
    public CreateEmployeeRequestValidator()
    {
        RuleFor(x => x.DocumentNumber)
            .NotEmpty()
            .WithMessage("Ingresa la cédula del colaborador.")
            .Matches(@"^\d{10}$")
            .WithMessage("La cédula debe tener 10 dígitos.");

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("Ingresa los nombres del colaborador.")
            .MinimumLength(2)
            .WithMessage("Los nombres deben tener al menos 2 caracteres.")
            .MaximumLength(100)
            .WithMessage("Los nombres no pueden superar 100 caracteres.");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Ingresa los apellidos del colaborador.")
            .MinimumLength(2)
            .WithMessage("Los apellidos deben tener al menos 2 caracteres.")
            .MaximumLength(100)
            .WithMessage("Los apellidos no pueden superar 100 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Ingresa el correo del colaborador.")
            .EmailAddress()
            .WithMessage("Ingresa un correo válido.")
            .MaximumLength(255)
            .WithMessage("El correo no puede superar 255 caracteres.");

        RuleFor(x => x.EmployeeCode)
            .NotEmpty()
            .WithMessage("Ingresa el código del colaborador.")
            .MaximumLength(50)
            .WithMessage("El código no puede superar 50 caracteres.");

        RuleFor(x => x.Department)
            .NotEmpty()
            .WithMessage("Selecciona el departamento.")
            .MaximumLength(100)
            .WithMessage("El departamento no puede superar 100 caracteres.");

        RuleFor(x => x.MonthlyAllowance)
            .GreaterThanOrEqualTo(10)
            .WithMessage("El cupo mínimo es $10.00.")
            .LessThanOrEqualTo(9999999999.99m)
            .WithMessage("El cupo supera el máximo permitido.");
    }
}
