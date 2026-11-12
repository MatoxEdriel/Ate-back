namespace Ate.Application.Features.Employees.DTOs;

public record CreateEmployeeRequest(
    string DocumentNumber,
    string FirstName,
    string LastName,
    string Email,
    string EmployeeCode,
    Guid DepartmentId,
    bool IsIntern = false);
