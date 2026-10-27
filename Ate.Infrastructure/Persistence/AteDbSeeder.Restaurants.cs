using Ate.Domain.Entities;
using Ate.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ate.Infrastructure.Persistence;

public static partial class AteDbSeeder
{
    private const string MerchantEmail = "comercio@ate.com";
    private const string LunchDrink = "Jugo";
    private const string BreakfastDrink = "Café";
    private const decimal BreakfastPrice = 2.50m;
    private const decimal LunchPrice = 3.50m;
    private const decimal DinnerPrice = 3.00m;

    private sealed record SeedDish(MealType MealType, string Name, string Protein, string Description, bool HasRice);

    private sealed record SeedRestaurant(
        string Code,
        string Name,
        string Cuisine,
        int DistanceM,
        int ReadyMinutes,
        string BrandColor,
        string Icon,
        SeedDish[] Dishes);

    private static readonly SeedRestaurant[] SampleRestaurants =
    [
        new("patio", "El Patio Criollo", "Criolla", 250, 15, "#B5532E", "cubiertos",
        [
            new(MealType.Breakfast, "Bolón de verde con chicharrón", "Cerdo", "Huevo frito y queso fresco.", false),
            new(MealType.Breakfast, "Tigrillo", "Vegetariano", "Verde majado con huevo y queso.", false),
            new(MealType.Lunch, "Seco de pollo", "Pollo", "Sopa de quinua, arroz y maduro.", true),
            new(MealType.Lunch, "Guatita", "Res", "Sopa de quinua, arroz y aguacate.", true),
            new(MealType.Lunch, "Pescado frito", "Pescado", "Sopa de quinua, arroz y ensalada.", true),
            new(MealType.Dinner, "Seco de carne", "Res", "Arroz, maduro y ensalada.", true),
            new(MealType.Dinner, "Pollo a la plancha", "Pollo", "Arroz, menestra y ensalada.", true)
        ]),
        new("mercedes", "Comedor Doña Mercedes", "Casera", 400, 10, "#8A5A2B", "olla",
        [
            new(MealType.Breakfast, "Caldo de gallina", "Pollo", "Con yuca y pan.", false),
            new(MealType.Breakfast, "Humitas", "Vegetariano", "Dos humitas con queso.", false),
            new(MealType.Lunch, "Menestra con carne asada", "Res", "Sopa del día, arroz y patacones.", true),
            new(MealType.Lunch, "Arroz con pollo", "Pollo", "Sopa del día y ensalada.", false),
            new(MealType.Dinner, "Arroz con menestra y carne", "Res", "Patacones y ensalada.", true),
            new(MealType.Dinner, "Caldo de bolas de verde", "Res", "Bolas de verde rellenas y maní.", false)
        ]),
        new("verde", "Verde Bowl", "Saludable", 600, 20, "#2F7A3E", "hoja",
        [
            new(MealType.Breakfast, "Bowl de avena y frutas", "Vegetariano", "Frutas de temporada y granola.", false),
            new(MealType.Breakfast, "Tostadas con aguacate", "Vegetariano", "Pan integral, huevo pochado y aguacate.", false),
            new(MealType.Lunch, "Bowl de pollo y quinoa", "Pollo", "Ensalada fresca y aguacate.", false),
            new(MealType.Lunch, "Bowl vegetariano", "Vegetariano", "Garbanzos, vegetales asados y hummus.", false),
            new(MealType.Dinner, "Wrap de pollo", "Pollo", "Vegetales frescos y salsa de yogur.", false),
            new(MealType.Dinner, "Bowl de quinoa y vegetales", "Vegetariano", "Quinoa, vegetales asados y aguacate.", false)
        ]),
        new("sazon", "La Sazón Manabita", "Manabita", 750, 20, "#1F6F9F", "pescado",
        [
            new(MealType.Breakfast, "Encebollado", "Pescado", "Con yuca, chifles y limón.", false),
            new(MealType.Breakfast, "Bolón mixto", "Cerdo", "Queso, chicharrón y huevo frito.", false),
            new(MealType.Lunch, "Viche de pescado", "Pescado", "Arroz y maduro.", true),
            new(MealType.Lunch, "Seco de chivo", "Chivo", "Arroz amarillo y maduro.", true),
            new(MealType.Dinner, "Corviche", "Pescado", "Con ensalada de cebolla y limón.", false),
            new(MealType.Dinner, "Arroz con menestra y pescado", "Pescado", "Pescado frito, patacones y ensalada.", true)
        ])
    ];

    /// <summary>
    /// Restaurantes aliados de ejemplo, con su menú y convenio con la empresa demo.
    /// Idempotente: solo crea lo que falta (se identifica cada restaurante por su código).
    /// </summary>
    private static async Task SeedRestaurantsAsync(AteDbContext context, Company company)
    {
        var merchantUserId = await context.Users
            .Where(u => u.Email == MerchantEmail)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync();

        var codes = SampleRestaurants.Select(r => r.Code).ToList();
        var existingRestaurants = await context.Restaurants
            .Where(r => codes.Contains(r.Code))
            .ToDictionaryAsync(r => r.Code);

        var existingAgreements = await context.CompanyRestaurantAgreements
            .Where(a => a.CompanyId == company.Id)
            .Select(a => a.RestaurantId)
            .ToListAsync();

        var startsOn = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));

        foreach (var seed in SampleRestaurants)
        {
            if (!existingRestaurants.TryGetValue(seed.Code, out var restaurant))
            {
                restaurant = new Restaurant
                {
                    Id = Guid.CreateVersion7(),
                    MerchantUserId = seed.Code == "patio" ? merchantUserId : null,
                    Code = seed.Code,
                    Name = seed.Name,
                    Cuisine = seed.Cuisine,
                    BrandColor = seed.BrandColor,
                    Icon = seed.Icon,
                    ReadyMinutes = seed.ReadyMinutes,
                    BreakfastCutoff = new TimeOnly(9, 0),
                    LunchCutoff = new TimeOnly(11, 30),
                    DinnerCutoff = new TimeOnly(18, 30),
                    IsActive = true
                };

                var sortOrder = 0;
                foreach (var dish in seed.Dishes)
                {
                    restaurant.Dishes.Add(new RestaurantDish
                    {
                        Id = Guid.CreateVersion7(),
                        RestaurantId = restaurant.Id,
                        MealType = dish.MealType,
                        Name = dish.Name,
                        Description = dish.Description,
                        Protein = dish.Protein,
                        HasRice = dish.HasRice,
                        DefaultDrink = dish.MealType == MealType.Breakfast ? BreakfastDrink : LunchDrink,
                        Price = dish.MealType switch
                        {
                            MealType.Breakfast => BreakfastPrice,
                            MealType.Lunch => LunchPrice,
                            _ => DinnerPrice
                        },
                        IsAvailable = true,
                        SortOrder = sortOrder++
                    });
                }

                context.Restaurants.Add(restaurant);
            }

            if (!existingAgreements.Contains(restaurant.Id))
            {
                context.CompanyRestaurantAgreements.Add(new CompanyRestaurantAgreement
                {
                    Id = Guid.CreateVersion7(),
                    CompanyId = company.Id,
                    RestaurantId = restaurant.Id,
                    DistanceM = seed.DistanceM,
                    StartsOn = startsOn,
                    IsActive = true
                });
            }
        }

        await context.SaveChangesAsync();
    }
}
