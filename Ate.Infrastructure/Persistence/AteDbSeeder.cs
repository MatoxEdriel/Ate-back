using Ate.Application.Common.Interfaces;
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
            new Role { Id = 1, Name = "SuperAdmin", Description = "Platform super administrator" },
            new Role { Id = 2, Name = "CompanyAdmin", Description = "Company client administrator" },
            new Role { Id = 3, Name = "Employee", Description = "Company employee" },
            new Role { Id = 4, Name = "Merchant", Description = "Affiliated merchant" }
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

        var existingUser = await context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == adminEmail);

        if (existingUser != null)
        {
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
            DocumentNumber = "0000000000"
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
