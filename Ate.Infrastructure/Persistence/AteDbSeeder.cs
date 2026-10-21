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
        await SeedDefaultAdminAsync(context, passwordHasher);
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

    private static async Task SeedDefaultAdminAsync(AteDbContext context, IPasswordHasher passwordHasher)
    {
        const string adminEmail = "admin@ate.com";
        const string adminDocumentNumber = "0943676155";
        const string legacyAdminDocumentNumber = "0000000000";

        var existingUser = await context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == adminEmail);

        if (existingUser != null)
        {
            var legacyAdminPerson = await context.Persons
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.UserId == existingUser.Id && p.DocumentNumber == legacyAdminDocumentNumber);

            if (legacyAdminPerson != null)
            {
                legacyAdminPerson.DocumentNumber = adminDocumentNumber;
                await context.SaveChangesAsync();
            }

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
            FirstName = "SuperAdmin",
            LastName = "Ate",
            DocumentType = DocumentType.NationalId,
            DocumentNumber = adminDocumentNumber
        };

        var adminRole = new UserRole
        {
            UserId = adminUser.Id,
            RoleId = 1,
            AssignedAt = DateTimeOffset.UtcNow
        };

        context.Users.Add(adminUser);
        context.Persons.Add(adminPerson);
        context.UserRoles.Add(adminRole);

        await context.SaveChangesAsync();
    }
}
