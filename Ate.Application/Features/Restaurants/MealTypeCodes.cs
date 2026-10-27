using Ate.Domain.Enums;

namespace Ate.Application.Features.Restaurants;

/// <summary>
/// Códigos de comida que usa la API (y el frontend). En Ecuador "merienda" es la cena.
/// </summary>
public static class MealTypeCodes
{
    public const string Breakfast = "desayuno";
    public const string Lunch = "almuerzo";
    public const string Dinner = "merienda";

    public static readonly IReadOnlyList<string> All = [Breakfast, Lunch, Dinner];

    public static string ToCode(MealType mealType) => mealType switch
    {
        MealType.Breakfast => Breakfast,
        MealType.Lunch => Lunch,
        MealType.Dinner => Dinner,
        _ => throw new ArgumentOutOfRangeException(nameof(mealType), mealType, null)
    };

    public static bool TryParse(string? code, out MealType mealType)
    {
        switch (code?.Trim().ToLowerInvariant())
        {
            case Breakfast:
                mealType = MealType.Breakfast;
                return true;
            case Lunch:
                mealType = MealType.Lunch;
                return true;
            case Dinner:
                mealType = MealType.Dinner;
                return true;
            default:
                mealType = default;
                return false;
        }
    }
}
