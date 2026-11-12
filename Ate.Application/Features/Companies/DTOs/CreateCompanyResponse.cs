namespace Ate.Application.Features.Companies.DTOs;

/// <param name="WelcomeEmailSent">
/// Whether the welcome email with the temporary password was sent. The password is never returned:
/// if the email failed, the user recovers access with "Forgot my password".
/// </param>
public record CreateCompanyResponse(
    CompanyDto Company,
    bool WelcomeEmailSent);
