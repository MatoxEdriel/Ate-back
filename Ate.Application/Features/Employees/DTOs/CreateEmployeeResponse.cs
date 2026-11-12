namespace Ate.Application.Features.Employees.DTOs;

/// <param name="WelcomeEmailSent">
/// Whether the welcome email with the temporary password was sent. The password is never returned.
/// </param>
public record CreateEmployeeResponse(
    EmployeeDto Employee,
    bool WelcomeEmailSent);
