using Ate.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ate.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Person> Persons { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<Company> Companies { get; }
    DbSet<Employee> Employees { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<UserExternalLogin> UserExternalLogins { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
