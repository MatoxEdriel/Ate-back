using Ate.Application.Features.Employees.DTOs;
using Ate.Domain.Common;

namespace Ate.Application.Common.Interfaces;

public interface IEmployeeService
{
    Task<Result<IReadOnlyList<EmployeeDto>>> GetCompanyEmployeesAsync(Guid adminUserId, CancellationToken cancellationToken = default);
    Task<Result<CreateEmployeeResponse>> CreateAsync(Guid adminUserId, CreateEmployeeRequest request, CancellationToken cancellationToken = default);
    Task<Result<EmployeeDto>> UpdateStatusAsync(Guid adminUserId, Guid employeeId, UpdateEmployeeStatusRequest request, CancellationToken cancellationToken = default);
}
