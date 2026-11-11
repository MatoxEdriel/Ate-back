namespace Ate.Application.Features.Companies.DTOs;

public record CreateCompanyResponse(
    CompanyDto Company,
    string TemporaryPassword);
