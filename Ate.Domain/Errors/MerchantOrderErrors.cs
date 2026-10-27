using Ate.Domain.Common;

namespace Ate.Domain.Errors;

public static class MerchantOrderErrors
{
    public static readonly Error RestaurantNotFound =
        Error.Forbidden("MerchantOrder.RestaurantNotFound", "Tu cuenta no tiene un restaurante activo asociado.");

    public static readonly Error InvalidDate =
        Error.Validation("MerchantOrder.InvalidDate", "La fecha debe tener el formato yyyy-MM-dd.");

    public static readonly Error InvalidStatus =
        Error.Validation("MerchantOrder.InvalidStatus", "El estado debe ser Confirmed, Ready, Delivered o Cancelled.");

    public static readonly Error InvalidTransition =
        Error.Conflict("MerchantOrder.InvalidTransition", "Este pedido ya no puede pasar a ese estado. Actualiza la lista.");
}
