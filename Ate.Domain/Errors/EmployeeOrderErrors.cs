using Ate.Domain.Common;

namespace Ate.Domain.Errors;

public static class EmployeeOrderErrors
{
    public static readonly Error EmployeeNotFound =
        Error.Forbidden("EmployeeOrder.EmployeeNotFound", "Tu cuenta no está registrada como colaborador activo.");

    public static readonly Error RestaurantNotAvailable =
        Error.NotFound("EmployeeOrder.RestaurantNotAvailable", "Este restaurante no tiene convenio activo con tu empresa.");

    public static readonly Error DishNotAvailable =
        Error.NotFound("EmployeeOrder.DishNotAvailable", "Este plato ya no está disponible para este horario.");

    public static readonly Error OrderingClosed =
        Error.Validation("EmployeeOrder.OrderingClosed", "Ya pasó la hora de corte para pedir en este restaurante.");

    public static readonly Error AlreadyOrdered =
        Error.Conflict("EmployeeOrder.AlreadyOrdered", "Ya tienes un pedido registrado para esta comida. Cancélalo si quieres cambiarlo.");

    public static readonly Error NotFound =
        Error.NotFound("EmployeeOrder.NotFound", "No encontramos este pedido.");

    public static readonly Error CannotCancel =
        Error.Conflict("EmployeeOrder.CannotCancel", "Este pedido ya no se puede cancelar.");
}
