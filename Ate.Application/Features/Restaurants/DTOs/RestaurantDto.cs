namespace Ate.Application.Features.Restaurants.DTOs;

/// <summary>
/// Restaurante con convenio activo, con la forma que espera el dashboard del colaborador.
/// Cutoff, OrderingOpen y Menu usan como clave el código de comida: "desayuno", "almuerzo", "merienda".
/// OrderingOpen indica, calculado por el servidor en hora de negocio, si todavía se puede pedir esa comida.
/// </summary>
public record RestaurantDto(
    Guid Id,
    string Name,
    string Cuisine,
    int DistanceM,
    int ReadyMinutes,
    string BrandColor,
    string? LogoUrl,
    string? Icon,
    RestaurantLocationDto? Location,
    IReadOnlyDictionary<string, string> Cutoff,
    IReadOnlyDictionary<string, bool> OrderingOpen,
    IReadOnlyDictionary<string, IReadOnlyList<RestaurantDishDto>> Menu);

public record RestaurantLocationDto(decimal Latitude, decimal Longitude);

public record RestaurantDishDto(
    Guid Id,
    string Name,
    string Description,
    string Protein,
    bool HasRice,
    string DefaultDrink,
    decimal Price);
