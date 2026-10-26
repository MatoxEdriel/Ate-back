using Ate.Application.Common.Interfaces;
using Ate.Domain.Constants;
using Ate.Domain.Entities;
using Ate.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ate.Infrastructure.Persistence;

public static class AteDbSeeder
{
    private static readonly (string FirstName, string LastName, string DocumentNumber, string Email, string Code, string Department, decimal Allowance, bool IsActive)[] SampleEmployees =
    [
        ("María Belén", "Cevallos Ruiz", "0923456781", "maria.cevallos@acme.com", "EMP-002", "Talento Humano", 150m, true),
        ("José Andrés", "Paredes Vera", "0934567812", "jose.paredes@acme.com", "EMP-003", "Tecnología", 120m, true),
        ("Ana Lucía", "Torres Mendoza", "0945678123", "ana.torres@acme.com", "EMP-004", "Finanzas", 180m, true),
        ("Luis Fernando", "Zambrano Ortiz", "0956781234", "luis.zambrano@acme.com", "EMP-005", "Operaciones", 100m, true),
        ("Gabriela", "Salazar Moreira", "0967812345", "gabriela.salazar@acme.com", "EMP-006", "Ventas", 130m, true),
        ("Diego Alejandro", "Herrera Castro", "0978123456", "diego.herrera@acme.com", "EMP-007", "Tecnología", 160m, true),
        ("Valeria", "Montalvo Quiñónez", "0989234567", "valeria.montalvo@acme.com", "EMP-008", "Administración", 110m, false),
        ("Carlos Eduardo", "Villacís Romero", "0991345678", "carlos.villacis@acme.com", "EMP-009", "Operaciones", 100m, true),
        ("Daniela", "Aguirre Pazmiño", "0902456789", "daniela.aguirre@acme.com", "EMP-010", "Ventas", 140m, true),
        ("Miguel Ángel", "Rivadeneira León", "0913567890", "miguel.rivadeneira@acme.com", "EMP-011", "Finanzas", 200m, true),
        ("Sofía", "Benítez Andrade", "0924678901", "sofia.benitez@acme.com", "EMP-012", "Talento Humano", 120m, false),
        ("Andrés Sebastián", "Jaramillo Cruz", "0935789012", "andres.jaramillo@acme.com", "EMP-013", "Tecnología", 175m, true),
        ("Paola Estefanía", "Navarrete Solís", "0946890123", "paola.navarrete@acme.com", "EMP-014", "Administración", 115m, true),
        ("Ricardo", "Espinoza Guerrero", "0957901234", "ricardo.espinoza@acme.com", "EMP-015", "Operaciones", 105m, true)
    ];

    public static async Task SeedAsync(AteDbContext context, IPasswordHasher passwordHasher)
    {
        await SeedRolesAsync(context);
        var company = await SeedDefaultCompanyAsync(context);
        await SeedCompanyAdminAsync(context, passwordHasher, company);
        await SeedSuperAdminAsync(context, passwordHasher);
        await SeedDefaultEmployeeAsync(context, passwordHasher, company);
        await SeedSampleEmployeesAsync(context, passwordHasher, company);
    }

    private static async Task<Company> SeedDefaultCompanyAsync(AteDbContext context)
    {
        const string companyTaxId = "0990000001001";
        const string companyLogoUrl = "/assets/companies/acme-logo.svg";

        var company = await context.Companies.FirstOrDefaultAsync(c => c.TaxId == companyTaxId);
        if (company != null)
        {
            if (string.IsNullOrWhiteSpace(company.LogoUrl))
            {
                company.LogoUrl = companyLogoUrl;
                await context.SaveChangesAsync();
            }

            return company;
        }

        company = new Company
        {
            Id = Guid.CreateVersion7(),
            BusinessName = "Acme Corporation S.A.",
            TradeName = "Acme Corp",
            TaxId = companyTaxId,
            Email = "contacto@acme.com",
            Phone = "042000000",
            Address = "Av. Principal 123 y Calle B",
            LogoUrl = companyLogoUrl,
            IsActive = true
        };

        context.Companies.Add(company);
        await context.SaveChangesAsync();

        return company;
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

    private static async Task SeedCompanyAdminAsync(AteDbContext context, IPasswordHasher passwordHasher, Company company)
    {
        const string adminEmail = "admin@ate.com";
        const string adminDocumentNumber = "0943676155";

        var existingUser = await context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == adminEmail);

        if (existingUser != null)
        {
            existingUser.CompanyId ??= company.Id;

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
            MustChangePassword = false,
            CompanyId = company.Id
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

    private static async Task SeedDefaultEmployeeAsync(AteDbContext context, IPasswordHasher passwordHasher, Company company)
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
            MonthlyAllowance = 120m,
            IsActive = true
        };

        context.Users.Add(user);
        context.Persons.Add(person);
        context.UserRoles.Add(role);
        context.Employees.Add(employee);

        await context.SaveChangesAsync();
    }

    private static async Task SeedSampleEmployeesAsync(AteDbContext context, IPasswordHasher passwordHasher, Company company)
    {
        var documentNumbers = SampleEmployees.Select(s => s.DocumentNumber).ToList();
        var existingDocuments = await context.Persons
            .IgnoreQueryFilters()
            .Where(p => documentNumbers.Contains(p.DocumentNumber))
            .Select(p => p.DocumentNumber)
            .ToListAsync();

        var pending = SampleEmployees.Where(s => !existingDocuments.Contains(s.DocumentNumber)).ToList();
        if (pending.Count == 0)
        {
            return;
        }

        var passwordHash = passwordHasher.HashPassword("Admin123*Ate");

        foreach (var sample in pending)
        {
            var user = new User
            {
                Email = sample.Email,
                PasswordHash = passwordHash,
                IsActive = true,
                EmailConfirmed = true,
                MustChangePassword = false
            };

            var person = new Person
            {
                UserId = user.Id,
                FirstName = sample.FirstName,
                LastName = sample.LastName,
                DocumentType = DocumentType.NationalId,
                DocumentNumber = sample.DocumentNumber
            };

            context.Users.Add(user);
            context.Persons.Add(person);
            context.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = 3,
                AssignedAt = DateTimeOffset.UtcNow
            });
            context.Employees.Add(new Employee
            {
                CompanyId = company.Id,
                PersonId = person.Id,
                EmployeeCode = sample.Code,
                Department = sample.Department,
                MonthlyAllowance = sample.Allowance,
                IsActive = sample.IsActive
            });
        }

        await context.SaveChangesAsync();
    }
}
