using Ate.Domain.Common;

namespace Ate.Domain.Errors;

public static class CompanyErrors
{
    public static readonly Error TaxIdAlreadyExists =
        Error.Conflict("Company.TaxIdAlreadyExists", "Ya existe una empresa registrada con este RUC.");

    public static readonly Error AdminDocumentAlreadyExists =
        Error.Conflict("Company.AdminDocumentAlreadyExists", "Ya existe una persona registrada con la cédula del administrador.");

    public static readonly Error AdminEmailAlreadyExists =
        Error.Conflict("Company.AdminEmailAlreadyExists", "Ya existe una cuenta registrada con el correo del administrador.");

    public static readonly Error NotFound =
        Error.NotFound("Company.NotFound", "No encontramos esta empresa.");
}
