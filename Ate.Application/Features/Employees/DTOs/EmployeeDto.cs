namespace Ate.Application.Features.Employees.DTOs;

public record EmployeeDto(
    Guid Id,
    string DocumentNumber,
    string FirstName,
    string LastName,
    string Email,
    string EmployeeCode,
    string Department,
    decimal MonthlyAllowance,
    bool IsActive,
    DateTimeOffset CreatedAt);
