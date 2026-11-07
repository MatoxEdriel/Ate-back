using Ate.Domain.Enums;

namespace Ate.Application.Features.Restaurants;

/// <summary>Comida y día de servicio vigentes en un instante de hora de negocio.</summary>
public readonly record struct ServiceSlot(MealType MealType, DateOnly ServiceDate, bool IsTomorrow);

/// <summary>
/// Única fuente de verdad de qué comida se muestra, a qué día corresponde y si todavía se puede pedir.
/// Todo se evalúa en hora de negocio (<see cref="Common.Constants.DateTimeConstants.DefaultBusinessTimeZoneId"/>).
/// </summary>
public static class ServiceDay
{
    /// <summary>Desde esta hora el desayuno que se pide corresponde al día siguiente.</summary>
    public static readonly TimeOnly NextDayBreakfastFrom = new(22, 0);

    private static readonly TimeOnly BreakfastFrom = new(5, 0);
    private static readonly TimeOnly LunchFrom = new(10, 30);
    private static readonly TimeOnly DinnerFrom = new(16, 0);

    /// <summary>Comida que se ofrece ahora. De 22:00 a 04:59 se ofrece el desayuno ("mañana").</summary>
    public static ServiceSlot Resolve(DateTimeOffset businessNow)
    {
        var time = TimeOnly.FromDateTime(businessNow.DateTime);
        var today = DateOnly.FromDateTime(businessNow.DateTime);

        if (time >= DinnerFrom && time < NextDayBreakfastFrom)
        {
            return new ServiceSlot(MealType.Dinner, today, false);
        }

        if (time >= LunchFrom && time < DinnerFrom)
        {
            return new ServiceSlot(MealType.Lunch, today, false);
        }

        if (time >= BreakfastFrom && time < LunchFrom)
        {
            return new ServiceSlot(MealType.Breakfast, today, false);
        }

        return new ServiceSlot(MealType.Breakfast, ServiceDateFor(MealType.Breakfast, businessNow), true);
    }

    /// <summary>Día para el que se está pidiendo la comida indicada.</summary>
    public static DateOnly ServiceDateFor(MealType mealType, DateTimeOffset businessNow)
    {
        var today = DateOnly.FromDateTime(businessNow.DateTime);
        var time = TimeOnly.FromDateTime(businessNow.DateTime);
        return mealType == MealType.Breakfast && time >= NextDayBreakfastFrom ? today.AddDays(1) : today;
    }

    /// <summary>
    /// True mientras se puede pedir o cancelar: sin hora límite, desayuno ya pedido para mañana,
    /// o la hora actual no supera la hora límite.
    /// </summary>
    public static bool IsOrderingOpen(MealType mealType, TimeOnly? cutoff, DateTimeOffset businessNow)
    {
        if (!cutoff.HasValue)
        {
            return true;
        }

        var time = TimeOnly.FromDateTime(businessNow.DateTime);
        if (mealType == MealType.Breakfast && time >= NextDayBreakfastFrom)
        {
            return true;
        }

        return time <= cutoff.Value;
    }
}
