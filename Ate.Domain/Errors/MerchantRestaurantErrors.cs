using Ate.Domain.Common;

namespace Ate.Domain.Errors;

public static class MerchantRestaurantErrors
{
    /// <summary>Name, phone, address, location or logo changed without sending the password.</summary>
    public static readonly Error PasswordRequired =
        Error.Validation("MerchantRestaurant.PasswordRequired", "Ingresa tu contraseña para guardar los datos del local.", details: null);

    public static readonly Error InvalidPassword =
        Error.Validation("MerchantRestaurant.InvalidPassword", "La contraseña no es correcta.", details: null);
}
