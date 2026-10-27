using Ate.Application.Features.EmployeeOrders.DTOs;
using Ate.Application.Features.Restaurants;
using FluentValidation;

namespace Ate.Application.Features.EmployeeOrders.Validators;

public class CreateEmployeeOrderRequestValidator : AbstractValidator<CreateEmployeeOrderRequest>
{
    public CreateEmployeeOrderRequestValidator()
    {
        RuleFor(x => x.RestaurantId)
            .NotEmpty()
            .WithMessage("Selecciona un restaurante.");

        RuleFor(x => x.DishId)
            .NotEmpty()
            .WithMessage("Selecciona un plato.");

        RuleFor(x => x.MealType)
            .Must(mealType => MealTypeCodes.TryParse(mealType, out _))
            .WithMessage("El tipo de comida debe ser desayuno, almuerzo o merienda.");

        RuleFor(x => x.Drink)
            .Must(drink => string.IsNullOrWhiteSpace(drink)
                || drink.Trim().ToLowerInvariant() is EmployeeOrderDrinks.Juice or EmployeeOrderDrinks.Water or EmployeeOrderDrinks.Default)
            .WithMessage("La bebida debe ser jugo o agua.");

        RuleFor(x => x.DeliveryMode)
            .Must(mode => EmployeeOrderDeliveryModes.Normalize(mode) != null)
            .WithMessage("Elige si comes en el local, lo retiras para llevar o te lo llevan a tu trabajo.");

        RuleFor(x => x.Notes)
            .MaximumLength(250)
            .WithMessage("Las notas no pueden superar 250 caracteres.");
    }
}
