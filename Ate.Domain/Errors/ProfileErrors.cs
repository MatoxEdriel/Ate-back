using Ate.Domain.Common;

namespace Ate.Domain.Errors;

public static class ProfileErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Profile.NotFound", "No encontramos la información de tu perfil.");
}
