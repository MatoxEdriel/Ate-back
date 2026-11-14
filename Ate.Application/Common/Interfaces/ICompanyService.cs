using Ate.Application.Features.Companies.DTOs;
using Ate.Domain.Common;

namespace Ate.Application.Common.Interfaces;

public interface ICompanyService
{
    Task<Result<IReadOnlyList<CompanyDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<CompanyDto>> UpdateStatusAsync(Guid companyId, UpdateCompanyStatusRequest request, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<Result<CreateCompanyResponse>> CreateAsync(CreateCompanyRequest request, CancellationToken cancellationToken = default);
}
