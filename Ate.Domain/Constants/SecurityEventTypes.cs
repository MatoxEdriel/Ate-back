namespace Ate.Domain.Constants;

/// <summary>Tipos de evento de seguridad (tabla security_events).</summary>
public static class SecurityEventTypes
{
    public const string Login = "login";
    public const string LoginBlocked = "login_blocked";
    public const string Logout = "logout";
    public const string PasswordChanged = "password_changed";
    public const string PasswordResetRequested = "password_reset_requested";
    public const string PasswordResetCompleted = "password_reset_completed";
    public const string RefreshReuse = "refresh_reuse";
    public const string CompanyStatus = "company_status";
    public const string RestaurantStatus = "restaurant_status";
    public const string EmployeeStatus = "employee_status";
    public const string PayrollPdf = "payroll_pdf";
}

public static class SecurityEventOutcomes
{
    public const string Success = "success";
    public const string Failure = "failure";
}
