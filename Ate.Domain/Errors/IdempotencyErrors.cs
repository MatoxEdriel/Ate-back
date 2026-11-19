using Ate.Domain.Common;

namespace Ate.Domain.Errors;

public static class IdempotencyErrors
{
    public static readonly Error InvalidKey =
        Error.Validation("Idempotency.InvalidKey", "La clave de la operación no es válida.");

    public static readonly Error InProgress =
        Error.Conflict("Idempotency.InProgress", "Tu solicitud anterior todavía se está procesando. Espera un momento.");

    public static readonly Error KeyReused =
        Error.Failure("Idempotency.KeyReused", "Esta clave ya se usó para otra operación.");
}
