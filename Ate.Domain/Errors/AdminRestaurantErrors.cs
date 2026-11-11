using Ate.Domain.Common;

namespace Ate.Domain.Errors;

public static class AdminRestaurantErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Restaurant.NotFound", "No encontramos este restaurante.");
}
