namespace Ate.Application.Features.ServiceContext.DTOs;

/// <summary>
/// Reloj y día de servicio de la plataforma, para que los clientes no decidan con su propio reloj.
/// Now es un instante UTC (ISO-8601); ServiceDate es yyyy-MM-dd; MealType es "desayuno" | "almuerzo" | "merienda".
/// </summary>
public record ServiceContextDto(
    string TimeZone,
    DateTimeOffset Now,
    string ServiceDate,
    string MealType,
    bool IsTomorrow);
