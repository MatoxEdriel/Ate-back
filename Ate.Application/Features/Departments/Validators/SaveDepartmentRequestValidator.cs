using Ate.Application.Features.Departments.DTOs;
using FluentValidation;

namespace Ate.Application.Features.Departments.Validators;

public class SaveDepartmentRequestValidator : AbstractValidator<SaveDepartmentRequest>
{
    public SaveDepartmentRequestValidator()
    {
        RuleFor(x => x.Name)
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Ingresa el nombre del departamento.")
            .Must(name => name == null || name.Trim().Length >= 2)
            .WithMessage("El nombre debe tener al menos 2 caracteres.")
            .MaximumLength(100)
            .WithMessage("El nombre no puede superar 100 caracteres.");
    }
}
