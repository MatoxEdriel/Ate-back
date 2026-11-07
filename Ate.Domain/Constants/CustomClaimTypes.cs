namespace Ate.Domain.Constants;

public static class CustomClaimTypes
{
    public const string DocumentNumber = "document_number";
    public const string PersonId = "person_id";
    public const string CompanyId = "company_id";
    public const string EmployeeId = "employee_id";

    /// <summary>"true" while the account has a temporary password; restricts the API until it is changed.</summary>
    public const string MustChangePassword = "must_change_password";
}
