using Ate.Domain.Enums;
using Ate.Application.Common.Interfaces;
using Ate.Domain.Common;
using Ate.Domain.Constants;
using Ate.Domain.Errors;
using Microsoft.EntityFrameworkCore;

namespace Ate.Infrastructure.Services;

public class UserAccessValidator : IUserAccessValidator, IScopedDependency
{
    private readonly IApplicationDbContext _context;

    public UserAccessValidator(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Error?> ValidateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId && !u.IsDeleted)
            .Select(u => new
            {
                u.IsActive,
                CompanyIsActive = u.Company != null ? (bool?)u.Company.IsActive : null,
                PersonId = u.Person != null ? (Guid?)u.Person.Id : null,
                IsEmployee = u.UserRoles.Any(ur => ur.Role.Name == RoleConstants.Employee),
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
        {
            return AuthErrors.InvalidToken;
        }

        if (!user.IsActive)
        {
            return AuthErrors.UserInactive;
        }

        if (user.CompanyIsActive == false)
        {
            return AuthErrors.CompanyInactive;
        }

        if (user.IsEmployee)
        {
            // HR deactivates employees on the Employee record, not on the User.
            var employee = await _context.Employees
                .AsNoTracking()
                .Where(e => e.PersonId == user.PersonId)
                .OrderByDescending(e => e.Status == EmployeeStatus.Active)
                .Select(e => new { e.Status, CompanyIsActive = e.Company.IsActive })
                .FirstOrDefaultAsync(cancellationToken);

            if (employee == null || employee.Status != EmployeeStatus.Active)
            {
                return AuthErrors.UserInactive;
            }

            if (!employee.CompanyIsActive)
            {
                return AuthErrors.CompanyInactive;
            }
        }

        return null;
    }
}
