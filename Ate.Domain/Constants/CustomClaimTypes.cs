namespace Ate.Domain.Constants;

public static class CustomClaimTypes
{
    /// <summary>Ya no se emite en el token (dato personal); se conserva para leer tokens antiguos.</summary>
    public const string DocumentNumber = "document_number";

    /// <summary>Sello de seguridad del usuario: cambia al cambiar la contraseña e invalida los tokens anteriores.</summary>
    public const string SecurityStamp = "sstamp";
    public const string PersonId = "person_id";
    public const string CompanyId = "company_id";
    public const string EmployeeId = "employee_id";

    /// <summary>"true" while the account has a temporary password; restricts the API until it is changed.</summary>
    public const string MustChangePassword = "must_change_password";
}
