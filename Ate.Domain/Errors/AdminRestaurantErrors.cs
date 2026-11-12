using Ate.Domain.Common;

namespace Ate.Domain.Errors;

public static class AdminRestaurantErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Restaurant.NotFound", "No encontramos este restaurante.");

    public static readonly Error TaxIdAlreadyExists =
        Error.Conflict("Restaurant.TaxIdAlreadyExists", "Ya existe un restaurante registrado con este RUC.");

    public static readonly Error MerchantDocumentAlreadyExists =
        Error.Conflict("Restaurant.MerchantDocumentAlreadyExists", "Ya existe una persona registrada con la cédula del responsable.");

    public static readonly Error MerchantEmailAlreadyExists =
        Error.Conflict("Restaurant.MerchantEmailAlreadyExists", "Ya existe una cuenta registrada con el correo del responsable.");
}
