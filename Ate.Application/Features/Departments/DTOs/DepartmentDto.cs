namespace Ate.Application.Features.Departments.DTOs;

public record DepartmentDto(
    Guid Id,
    string Name,
    int EmployeeCount,
    DateTimeOffset CreatedAt);

public record SaveDepartmentRequest(string Name);
