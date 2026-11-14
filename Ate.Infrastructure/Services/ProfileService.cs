using Ate.Domain.Enums;
using Ate.Application.Common.Interfaces;
using Ate.Application.Features.Profile.DTOs;
using Ate.Domain.Common;
using Ate.Domain.Constants;
using Ate.Domain.Entities;
using Ate.Domain.Errors;
using Microsoft.EntityFrameworkCore;

namespace Ate.Infrastructure.Services;

public class ProfileService : IProfileService, IScopedDependency
{
    private readonly IApplicationDbContext _context;

    public ProfileService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<ProfileDto>> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.Person)
            .Include(u => u.Company)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);

        if (user?.Person == null)
        {
            return Result.Failure<ProfileDto>(ProfileErrors.NotFound);
        }

        var roles = user.UserRoles
            .Where(ur => ur.Role != null && !string.IsNullOrWhiteSpace(ur.Role.Name))
            .Select(ur => ur.Role.Name)
            .Distinct()
            .ToList();

        var employee = await _context.Employees
            .AsNoTracking()
            .Include(e => e.Company)
            .Include(e => e.Department)
            .Where(e => e.PersonId == user.Person.Id && e.Status == EmployeeStatus.Active)
            .FirstOrDefaultAsync(cancellationToken);

        var company = user.Company ?? employee?.Company;

        // Only the company admin sees the tax id and contact details of the company.
        var includeCompanyDetails = roles.Contains(RoleConstants.CompanyAdmin) || roles.Contains(RoleConstants.SuperAdmin);

        var employment = employee == null
            ? null
            : new ProfileEmploymentDto(employee.EmployeeCode, employee.Department?.Name, employee.IsIntern);

        var profile = new ProfileDto(
            user.Id,
            user.Email ?? string.Empty,
            user.Person.FirstName,
            user.Person.LastName,
            $"{user.Person.FirstName} {user.Person.LastName}".Trim(),
            user.Person.DocumentType.ToString(),
            user.Person.DocumentNumber,
            user.Person.PhoneNumber,
            user.Person.AvatarUrl,
            roles,
            user.LastLoginAt,
            user.CreatedAt,
            ToCompanyDto(company, includeCompanyDetails),
            employment);

        return Result.Success(profile);
    }

    private static ProfileCompanyDto? ToCompanyDto(Company? company, bool includeDetails)
    {
        return company == null
            ? null
            : new ProfileCompanyDto(
                company.Id,
                company.BusinessName,
                company.TradeName,
                includeDetails ? company.TaxId : null,
                includeDetails ? company.Email : null,
                includeDetails ? company.Phone : null,
                includeDetails ? company.Address : null,
                company.LogoUrl,
                company.IsActive);
    }
}
