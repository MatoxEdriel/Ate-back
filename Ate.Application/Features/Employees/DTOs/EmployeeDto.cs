namespace Ate.Application.Features.Employees.DTOs;

public record EmployeeDto(
    Guid Id,
    string DocumentNumber,
    string FirstName,
    string LastName,
    string Email,
    string EmployeeCode,
    Guid? DepartmentId,
    string Department,
    bool IsIntern,
    bool IsActive,
    DateTimeOffset CreatedAt);
