using System.Globalization;
using System.Text.RegularExpressions;
using Ate.Application.Features.MerchantRestaurant.DTOs;
using Ate.Application.Features.Restaurants;
using FluentValidation;

namespace Ate.Application.Features.MerchantRestaurant.Validators;

public class UpdateMerchantRestaurantRequestValidator : AbstractValidator<UpdateMerchantRestaurantRequest>
{
    /// <summary>
    /// TEMPORAL: el logo llega serializado en base64 porque aún no hay servidor de imágenes.
    /// El front lo reduce a 256 px; ~300 mil caracteres (~220 KB) sobra para eso.
    /// </summary>
    public const int MaxLogoLength = 300_000;

    private static readonly Regex LogoDataUrl = new(
        @"^data:image/(png|webp|jpeg);base64,[A-Za-z0-9+/]+={0,2}$",
        RegexOptions.Compiled);

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

        RuleFor(x => x.Cutoff)
            .Must(cutoff => cutoff == null || cutoff.Keys.All(code => MealTypeCodes.TryParse(code, out _)))
            .WithMessage("Las horas límite solo pueden ser de desayuno, almuerzo o merienda.")
            .Must(cutoff => cutoff == null || cutoff.Values.All(BeValidTime))
            .WithMessage("Cada hora límite debe tener el formato HH:mm (por ejemplo 11:30).");

        RuleFor(x => x.Logo)
            .MaximumLength(MaxLogoLength)
            .WithMessage("El logo es demasiado pesado. Usa una imagen más pequeña.")
            .Must(logo => LogoDataUrl.IsMatch(logo!))
            .WithMessage("El logo debe ser una imagen PNG, WebP o JPG.")
            .When(x => !string.IsNullOrEmpty(x.Logo));

        RuleFor(x => x.ReadyMinutes)
            .InclusiveBetween(5, 120)
            .WithMessage("El tiempo de preparación debe estar entre 5 y 120 minutos.");
    }

    private static bool BeValidTime(string? time) =>
        TimeOnly.TryParseExact(time?.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
}
