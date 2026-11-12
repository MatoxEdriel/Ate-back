using Ate.Application.Common.Validation;
using Ate.Application.Features.Restaurants;
using Ate.Application.Features.Restaurants.DTOs;
using FluentValidation;

namespace Ate.Application.Features.Restaurants.Validators;

public class CreateRestaurantRequestValidator : AbstractValidator<CreateRestaurantRequest>
{
    public CreateRestaurantRequestValidator()
    {
        RuleFor(x => x.TaxId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Ingresa el RUC del restaurante.")
            .Matches(@"^\d{13}$").WithMessage("El RUC debe tener 13 dígitos.")
            .ValidEcuadorianRuc().WithMessage("El RUC ingresado no es válido.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Ingresa el nombre comercial del restaurante.")
            .MinimumLength(2).WithMessage("El nombre debe tener al menos 2 caracteres.")
            .MaximumLength(150).WithMessage("El nombre no puede superar 150 caracteres.");

        RuleFor(x => x.Cuisine)
            .NotEmpty().WithMessage("Ingresa la especialidad o tipo de cocina.")
            .MaximumLength(80).WithMessage("La especialidad no puede superar 80 caracteres.");

        RuleFor(x => x.Phone)
            .MaximumLength(30).WithMessage("El teléfono no puede superar 30 caracteres.")
            .Matches(@"^[0-9+\-\s()]*$").WithMessage("El teléfono solo puede tener números, espacios, +, - y paréntesis.");

        RuleFor(x => x.Address)
            .MaximumLength(300).WithMessage("La dirección no puede superar 300 caracteres.");

        RuleFor(x => x.BrandColor)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Elige el color de la marca.")
            .Matches(@"^#[0-9A-Fa-f]{6}$").WithMessage("El color debe tener el formato #RRGGBB.");

        RuleFor(x => x.ReadyMinutes)
            .InclusiveBetween(5, 120)
            .WithMessage("El tiempo de preparación debe estar entre 5 y 120 minutos.");

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

        RuleFor(x => x.MerchantDocumentNumber)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Ingresa la cédula del responsable del restaurante.")
            .Matches(@"^\d{10}$").WithMessage("La cédula debe tener 10 dígitos.")
            .ValidEcuadorianNationalId().WithMessage("La cédula ingresada no es válida.");

        RuleFor(x => x.MerchantFirstName)
            .NotEmpty().WithMessage("Ingresa los nombres del responsable del restaurante.")
            .MinimumLength(2).WithMessage("Los nombres deben tener al menos 2 caracteres.")
            .MaximumLength(100).WithMessage("Los nombres no pueden superar 100 caracteres.");

        RuleFor(x => x.MerchantLastName)
            .NotEmpty().WithMessage("Ingresa los apellidos del responsable del restaurante.")
            .MinimumLength(2).WithMessage("Los apellidos deben tener al menos 2 caracteres.")
            .MaximumLength(100).WithMessage("Los apellidos no pueden superar 100 caracteres.");

        RuleFor(x => x.MerchantEmail)
            .NotEmpty().WithMessage("Ingresa el correo del responsable del restaurante.")
            .EmailAddress().WithMessage("Ingresa un correo válido.")
            .MaximumLength(255).WithMessage("El correo no puede superar 255 caracteres.");
    }
}
