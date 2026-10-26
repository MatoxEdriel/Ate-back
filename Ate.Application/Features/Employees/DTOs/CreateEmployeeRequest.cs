namespace Ate.Application.Features.Employees.DTOs;

public record CreateEmployeeRequest(
    string DocumentNumber,
    string FirstName,
    string LastName,
    string Email,
    string EmployeeCode,
    string Department,
    decimal MonthlyAllowance);
