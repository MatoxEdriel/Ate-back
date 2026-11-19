using Ate.Domain.Common;

namespace Ate.Domain.Errors;

public static class FileErrors
{
    public static readonly Error InvalidImage =
        Error.Validation("File.InvalidImage", "La imagen debe ser un archivo PNG, JPG o WebP válido.");
}
