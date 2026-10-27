namespace Ate.Application.Features.EmployeeOrders.DTOs;

/// <param name="MealType">"desayuno", "almuerzo" o "merienda".</param>
/// <param name="WithRice">Solo aplica si el plato ofrece arroz; si viene null se asume con arroz.</param>
/// <param name="Drink">"jugo" o "agua" ("default" se acepta como jugo por compatibilidad).</param>
/// <param name="DeliveryMode">"local" (comer en el restaurante), "retiro" (para llevar) o "entrega" (en la dirección de la empresa).</param>
public record CreateEmployeeOrderRequest(
    Guid RestaurantId,
    string MealType,
    Guid DishId,
    bool? WithRice = null,
    string Drink = EmployeeOrderDrinks.Juice,
    string? Notes = null,
    string DeliveryMode = EmployeeOrderDeliveryModes.Pickup);

public static class EmployeeOrderDrinks
{
    public const string Juice = "jugo";
    public const string Water = "agua";
    /// <summary>Valor anterior ("bebida del plato"); hoy equivale a jugo.</summary>
    public const string Default = "default";
}

public static class EmployeeOrderDeliveryModes
{
    /// <summary>Come en el restaurante.</summary>
    public const string DineIn = "local";
    /// <summary>Retira en el restaurante para llevar.</summary>
    public const string Pickup = "retiro";
    /// <summary>El restaurante lo lleva a la dirección de la empresa.</summary>
    public const string Delivery = "entrega";

    public static readonly IReadOnlyList<string> All = [DineIn, Pickup, Delivery];

    /// <summary>Normaliza el valor recibido; vacío = retiro (valor por defecto histórico).</summary>
    public static string? Normalize(string? mode)
    {
        if (string.IsNullOrWhiteSpace(mode)) return Pickup;
        var value = mode.Trim().ToLowerInvariant();
        return All.Contains(value) ? value : null;
    }
}
