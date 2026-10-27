namespace Ate.Application.Features.EmployeeOrders.DTOs;

/// <param name="MealType">"desayuno", "almuerzo" o "merienda".</param>
/// <param name="WithRice">Solo aplica si el plato ofrece arroz; si viene null se asume con arroz.</param>
/// <param name="Drink">"default" (bebida del plato) o "agua".</param>
public record CreateEmployeeOrderRequest(
    Guid RestaurantId,
    string MealType,
    Guid DishId,
    bool? WithRice = null,
    string Drink = EmployeeOrderDrinks.Default,
    string? Notes = null);

public static class EmployeeOrderDrinks
{
    public const string Default = "default";
    public const string Water = "agua";
}
