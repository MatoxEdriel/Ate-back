using Ate.Domain.Common;

namespace Ate.Domain.Errors;

public static class EmployeeErrors
{
    public static readonly Error CompanyNotAssigned =
        Error.Forbidden("Employee.CompanyNotAssigned", "Tu cuenta no está vinculada a ninguna empresa.");

    public static readonly Error NotFound =
        Error.NotFound("Employee.NotFound", "No encontramos a este colaborador en tu empresa.");

    public static readonly Error DocumentAlreadyExists =
        Error.Conflict("Employee.DocumentAlreadyExists", "Ya existe una persona registrada con esta cédula.");

    public static readonly Error EmailAlreadyExists =
        Error.Conflict("Employee.EmailAlreadyExists", "Ya existe una cuenta registrada con este correo.");

    public static readonly Error CodeAlreadyExists =
        Error.Conflict("Employee.CodeAlreadyExists", "Ya existe un colaborador con este código en tu empresa.");
}
