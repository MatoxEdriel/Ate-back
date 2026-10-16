using Ate.Domain.Common;

namespace Ate.Domain.Errors;

public static class AuthErrors
{
    public static readonly Error UserNotFound =
        Error.Unauthorized("Auth.UserNotFound", "Invalid document number or password.");

    public static readonly Error InvalidCredentials =
        Error.Unauthorized("Auth.InvalidCredentials", "Invalid document number or password.");

    public static readonly Error UserInactive =
        Error.Forbidden("Auth.UserInactive", "Your account is deactivated or blocked. Please contact an administrator.");

    public static readonly Error InvalidToken =
        Error.Unauthorized("Auth.InvalidToken", "User identity could not be verified from the provided token.");

    public static readonly Error MissingCredentials =
        Error.Validation("Auth.MissingCredentials", "Document number and password are required.");
}
