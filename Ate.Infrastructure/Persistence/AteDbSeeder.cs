using Ate.Application.Common.Interfaces;
using Ate.Domain.Constants;
using Ate.Domain.Entities;
using Ate.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ate.Infrastructure.Persistence;

public static class AteDbSeeder
{
    public static async Task SeedAsync(AteDbContext context, IPasswordHasher passwordHasher)
    {
        await SeedRolesAsync(context);
        await SeedCompanyAdminAsync(context, passwordHasher);
        await SeedSuperAdminAsync(context, passwordHasher);
        await SeedDefaultEmployeeAsync(context, passwordHasher);
    }

    private static async Task SeedRolesAsync(AteDbContext context)
    {
        var roles = new[]
        {
            new Role { Id = 1, Name = RoleConstants.SuperAdmin, Description = "Platform super administrator" },
            new Role { Id = 2, Name = RoleConstants.CompanyAdmin, Description = "Company client administrator" },
            new Role { Id = 3, Name = RoleConstants.Employee, Description = "Company employee" },
            new Role { Id = 4, Name = RoleConstants.Merchant, Description = "Affiliated merchant" }
        };

        foreach (var role in roles)
        {
            if (!await context.Roles.AnyAsync(r => r.Id == role.Id))
            {
                context.Roles.Add(role);
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedCompanyAdminAsync(AteDbContext context, IPasswordHasher passwordHasher)
    {
        const string adminEmail = "admin@ate.com";
        const string adminDocumentNumber = "0943676155";

        var existingUser = await context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == adminEmail);

        if (existingUser != null)
        {
            var person = await context.Persons
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.UserId == existingUser.Id);

            if (person != null)
            {
                person.DocumentNumber = adminDocumentNumber;
                person.FirstName = "Admin";
                person.LastName = "Empresa";
            }

            var oldRoles = await context.UserRoles.Where(ur => ur.UserId == existingUser.Id).ToListAsync();
            context.UserRoles.RemoveRange(oldRoles);
            context.UserRoles.Add(new UserRole
            {
                UserId = existingUser.Id,
                RoleId = 2,
                AssignedAt = DateTimeOffset.UtcNow
            });

            await context.SaveChangesAsync();
            return;
        }

        var adminUser = new User
        {
            Id = Guid.CreateVersion7(),
            Email = adminEmail,
            PasswordHash = passwordHasher.HashPassword("Admin123*Ate"),
            IsActive = true,
            EmailConfirmed = true,
            MustChangePassword = false
        };

        var adminPerson = new Person
        {
            Id = Guid.CreateVersion7(),
            UserId = adminUser.Id,
            FirstName = "Admin",
            LastName = "Empresa",
            DocumentType = DocumentType.NationalId,
            DocumentNumber = adminDocumentNumber
        };

        var adminRole = new UserRole
        {
            UserId = adminUser.Id,
            RoleId = 2,
            AssignedAt = DateTimeOffset.UtcNow
        };

        context.Users.Add(adminUser);
        context.Persons.Add(adminPerson);
        context.UserRoles.Add(adminRole);

        await context.SaveChangesAsync();
    }

    private static async Task SeedSuperAdminAsync(AteDbContext context, IPasswordHasher passwordHasher)
    {
        const string superAdminEmail = "superadmin@ate.com";
        const string superAdminDocumentNumber = "0999999999";

        var existingUser = await context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == superAdminEmail);

        if (existingUser != null)
        {
            return;
        }

        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Email = superAdminEmail,
            PasswordHash = passwordHasher.HashPassword("Admin123*Ate"),
            IsActive = true,
            EmailConfirmed = true,
            MustChangePassword = false
        };

        var person = new Person
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            FirstName = "SuperAdmin",
            LastName = "Ate",
            DocumentType = DocumentType.NationalId,
            DocumentNumber = superAdminDocumentNumber
        };

        var role = new UserRole
        {
            UserId = user.Id,
            RoleId = 1,
            AssignedAt = DateTimeOffset.UtcNow
        };

        context.Users.Add(user);
        context.Persons.Add(person);
        context.UserRoles.Add(role);

        await context.SaveChangesAsync();
    }

    private static async Task SeedDefaultEmployeeAsync(AteDbContext context, IPasswordHasher passwordHasher)
    {
        const string employeeEmail = "empleado@ate.com";
        const string employeeDocumentNumber = "0912345678";

        var existingUser = await context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == employeeEmail);

        if (existingUser != null)
        {
            return;
        }

        var company = await context.Companies.FirstOrDefaultAsync(c => c.TaxId == "0990000001001");
        if (company == null)
        {
            company = new Company
            {
                Id = Guid.CreateVersion7(),
                BusinessName = "Acme Corporation S.A.",
                TradeName = "Acme Corp",
                TaxId = "0990000001001",
                Email = "contacto@acme.com",
                Phone = "042000000",
                Address = "Av. Principal 123 y Calle B",
                IsActive = true
            };
            context.Companies.Add(company);
            await context.SaveChangesAsync();
        }

        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Email = employeeEmail,
            PasswordHash = passwordHasher.HashPassword("Admin123*Ate"),
            IsActive = true,
            EmailConfirmed = true,
            MustChangePassword = false
        };

        var person = new Person
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            FirstName = "Carlos",
            LastName = "Mendoza",
            DocumentType = DocumentType.NationalId,
            DocumentNumber = employeeDocumentNumber
        };

        var role = new UserRole
        {
            UserId = user.Id,
            RoleId = 3,
            AssignedAt = DateTimeOffset.UtcNow
        };

        var employee = new Employee
        {
            Id = Guid.CreateVersion7(),
            CompanyId = company.Id,
            PersonId = person.Id,
            EmployeeCode = "EMP-001",
            Department = "Tecnología",
            IsActive = true
        };

        context.Users.Add(user);
        context.Persons.Add(person);
        context.UserRoles.Add(role);
        context.Employees.Add(employee);

        await context.SaveChangesAsync();
    }
}
