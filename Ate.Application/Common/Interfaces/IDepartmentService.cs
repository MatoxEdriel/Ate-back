using Ate.Application.Features.Departments.DTOs;
using Ate.Domain.Common;

namespace Ate.Application.Common.Interfaces;

public interface IDepartmentService
{
    Task<Result<IReadOnlyList<DepartmentDto>>> GetCompanyDepartmentsAsync(Guid adminUserId, CancellationToken cancellationToken = default);
    Task<Result<DepartmentDto>> CreateAsync(Guid adminUserId, SaveDepartmentRequest request, CancellationToken cancellationToken = default);
    Task<Result<DepartmentDto>> RenameAsync(Guid adminUserId, Guid departmentId, SaveDepartmentRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid adminUserId, Guid departmentId, CancellationToken cancellationToken = default);
}
