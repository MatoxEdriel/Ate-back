namespace Ate.Application.Features.Employees.DTOs;

public record UpdateEmployeeStatusRequest(bool IsActive);

/// <param name="DepartmentId">null deja al colaborador sin departamento.</param>
public record UpdateEmployeeDepartmentRequest(Guid? DepartmentId);
