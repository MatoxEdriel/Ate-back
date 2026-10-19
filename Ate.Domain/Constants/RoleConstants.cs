namespace Ate.Domain.Constants;

public static class RoleConstants
{
    public const string SuperAdmin = "SuperAdmin";
    public const string CompanyAdmin = "CompanyAdmin";
    public const string Employee = "Employee";
    public const string Merchant = "Merchant";

    public static readonly IReadOnlyList<string> All =
    [
        SuperAdmin,
        CompanyAdmin,
        Employee,
        Merchant
    ];
}
