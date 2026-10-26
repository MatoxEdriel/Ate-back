namespace Ate.Application.Features.Employees.DTOs;

public record CreateEmployeeResponse(
    EmployeeDto Employee,
    string TemporaryPassword);
