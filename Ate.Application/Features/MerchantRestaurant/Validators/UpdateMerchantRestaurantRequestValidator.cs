using Ate.Application.Features.MerchantRestaurant.DTOs;
using FluentValidation;

namespace Ate.Application.Features.MerchantRestaurant.Validators;

public class UpdateMerchantRestaurantRequestValidator : AbstractValidator<UpdateMerchantRestaurantRequest>
{
    public UpdateMerchantRestaurantRequestValidator()
    {
        RuleFor(x => x.Name)
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Ingresa el nombre comercial del restaurante.")
            .Must(name => name == null || name.Trim().Length >= 2)
            .WithMessage("El nombre debe tener al menos 2 caracteres.")
            .MaximumLength(150)
            .WithMessage("El nombre no puede superar 150 caracteres.");

        RuleFor(x => x.Cuisine)
            .Must(cuisine => !string.IsNullOrWhiteSpace(cuisine))
            .WithMessage("Ingresa la especialidad o tipo de cocina.")
            .MaximumLength(80)
            .WithMessage("La especialidad no puede superar 80 caracteres.");

        RuleFor(x => x.Address)
            .MaximumLength(300)
            .WithMessage("La dirección no puede superar 300 caracteres.");

        RuleFor(x => x.Phone)
            .MaximumLength(30)
            .WithMessage("El teléfono no puede superar 30 caracteres.")
            .Matches(@"^[0-9+\-\s()]*$")
            .WithMessage("El teléfono solo puede tener números, espacios, +, - y paréntesis.");

        RuleFor(x => x.BrandColor)
            .Matches(@"^#[0-9A-Fa-f]{6}$")
            .WithMessage("El color debe tener el formato #RRGGBB.");

        RuleFor(x => x.ReadyMinutes)
            .InclusiveBetween(5, 120)
            .WithMessage("El tiempo de preparación debe estar entre 5 y 120 minutos.");
    }
}
