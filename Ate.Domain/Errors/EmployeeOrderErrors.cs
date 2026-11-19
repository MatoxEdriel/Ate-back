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
        Error.Conflict("EmployeeOrder.AlreadyOrdered", "Ya tienes un pedido en curso para esta comida. Espera a que te lo entreguen o cancélalo si quieres cambiarlo.");

    public static readonly Error DeliveryAddressMissing =
        Error.Validation("EmployeeOrder.DeliveryAddressMissing", "Tu empresa no tiene una dirección registrada para entregas. Elige comer en el local o retirarlo para llevar.");

    public static readonly Error NotFound =
        Error.NotFound("EmployeeOrder.NotFound", "No encontramos este pedido.");

    public static readonly Error NotRatable =
        Error.Validation("EmployeeOrder.NotRatable", "Solo puedes calificar pedidos entregados en los últimos 7 días.");

    public static readonly Error AlreadyRated =
        Error.Conflict("EmployeeOrder.AlreadyRated", "Ya calificaste este pedido.");

    public static readonly Error InvalidRating =
        Error.Validation("EmployeeOrder.InvalidRating", "Responde al menos una pregunta o elige omitir.");

    public static readonly Error CannotCancel =
        Error.Conflict("EmployeeOrder.CannotCancel", "Este pedido ya no se puede cancelar.");
}
