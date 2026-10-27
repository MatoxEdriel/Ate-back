namespace Ate.Application.Features.Restaurants.DTOs;

/// <summary>
/// Restaurante con convenio activo, con la forma que espera el dashboard del colaborador.
/// Cutoff y Menu usan como clave el código de comida: "desayuno", "almuerzo", "merienda".
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
    IReadOnlyDictionary<string, string> Cutoff,
    IReadOnlyDictionary<string, IReadOnlyList<RestaurantDishDto>> Menu);

public record RestaurantDishDto(
    Guid Id,
    string Name,
    string Description,
    string Protein,
    bool HasRice,
    string DefaultDrink,
    decimal Price);
