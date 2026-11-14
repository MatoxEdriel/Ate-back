using Ate.Domain.Common;

namespace Ate.Domain.Errors;

public static class PayrollErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Payroll.NotFound", "No encontramos este corte.");

    public static readonly Error EmployeeNotFound =
        Error.Forbidden("Payroll.EmployeeNotFound", "Tu cuenta no está registrada como colaborador activo.");

    public static readonly Error InvalidMonth =
        Error.Validation("Payroll.InvalidMonth", "El mes debe tener el formato AAAA-MM.");
}
