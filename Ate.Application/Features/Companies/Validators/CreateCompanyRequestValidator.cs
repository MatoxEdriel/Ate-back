using Ate.Application.Common.Validation;
using Ate.Application.Features.Companies.DTOs;
using Ate.Application.Features.Restaurants;
using FluentValidation;

namespace Ate.Application.Features.Companies.Validators;

public class CreateCompanyRequestValidator : AbstractValidator<CreateCompanyRequest>
{
    public CreateCompanyRequestValidator()
    {
        RuleFor(x => x.BusinessName)
            .NotEmpty().WithMessage("Ingresa la razón social.")
            .MinimumLength(2).WithMessage("La razón social debe tener al menos 2 caracteres.")
            .MaximumLength(150).WithMessage("La razón social no puede superar 150 caracteres.");

        RuleFor(x => x.TradeName)
            .NotEmpty().WithMessage("Ingresa el nombre comercial.")
            .MaximumLength(150).WithMessage("El nombre comercial no puede superar 150 caracteres.");

        RuleFor(x => x.TaxId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Ingresa el RUC de la empresa.")
            .Matches(@"^\d{13}$").WithMessage("El RUC debe tener 13 dígitos.")
            .ValidEcuadorianRuc().WithMessage("El RUC ingresado no es válido.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Ingresa un correo de contacto válido.")
            .MaximumLength(100).WithMessage("El correo de contacto no puede superar 100 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Phone)
            .MaximumLength(30).WithMessage("El teléfono no puede superar 30 caracteres.");

        RuleFor(x => x.Address)
            .MaximumLength(300).WithMessage("La dirección no puede superar 300 caracteres.");

        RuleFor(x => x)
            .Must(x => x.Latitude.HasValue == x.Longitude.HasValue)
            .WithName("Location")
            .WithMessage("Ingresa la latitud y la longitud juntas, o deja ambas vacías.");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90m, 90m)
            .When(x => x.Latitude.HasValue)
            .WithMessage("La latitud debe estar entre -90 y 90.");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180m, 180m)
            .When(x => x.Longitude.HasValue)
            .WithMessage("La longitud debe estar entre -180 y 180.");

        RuleFor(x => x.Logo)
            .MaximumLength(RestaurantLogo.MaxLength)
            .WithMessage("El logo es demasiado pesado. Usa una imagen más pequeña.")
            .Must(RestaurantLogo.IsValidDataUrl)
            .WithMessage("El logo debe ser una imagen PNG, WebP o JPG.")
            .When(x => !string.IsNullOrEmpty(x.Logo));

        RuleFor(x => x.AdminDocumentNumber)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Ingresa la cédula del administrador de RRHH.")
            .Matches(@"^\d{10}$").WithMessage("La cédula debe tener 10 dígitos.")
            .ValidEcuadorianNationalId().WithMessage("La cédula ingresada no es válida.");

        RuleFor(x => x.AdminFirstName)
            .NotEmpty().WithMessage("Ingresa los nombres del administrador de RRHH.")
            .MinimumLength(2).WithMessage("Los nombres deben tener al menos 2 caracteres.")
            .MaximumLength(100).WithMessage("Los nombres no pueden superar 100 caracteres.");

        RuleFor(x => x.AdminLastName)
            .NotEmpty().WithMessage("Ingresa los apellidos del administrador de RRHH.")
            .MinimumLength(2).WithMessage("Los apellidos deben tener al menos 2 caracteres.")
            .MaximumLength(100).WithMessage("Los apellidos no pueden superar 100 caracteres.");

        RuleFor(x => x.AdminEmail)
            .NotEmpty().WithMessage("Ingresa el correo del administrador de RRHH.")
            .EmailAddress().WithMessage("Ingresa un correo válido.")
            .MaximumLength(255).WithMessage("El correo no puede superar 255 caracteres.");
    }
}
