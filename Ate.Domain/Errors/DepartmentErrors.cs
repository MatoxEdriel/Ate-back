using Ate.Domain.Common;

namespace Ate.Domain.Errors;

public static class DepartmentErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Department.NotFound", "No encontramos este departamento en tu empresa.");

    public static readonly Error NameAlreadyExists =
        Error.Conflict("Department.NameAlreadyExists", "Ya existe un departamento con este nombre en tu empresa.");
}
