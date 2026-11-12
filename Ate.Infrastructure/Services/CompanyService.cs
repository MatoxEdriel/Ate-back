using Ate.Application.Common.Interfaces;
using Ate.Application.Common.Models.Emails;
using Ate.Application.Features.Companies.DTOs;
using Ate.Domain.Common;
using Ate.Domain.Constants;
using Ate.Domain.Entities;
using Ate.Domain.Enums;
using Ate.Domain.Errors;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ate.Infrastructure.Services;

public class CompanyService : ICompanyService, IScopedDependency
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IValidator<CreateCompanyRequest> _createValidator;
    private readonly IEmailService _emailService;
    private readonly SmtpOptions _smtpOptions;
    private readonly ILogger<CompanyService> _logger;

    public CompanyService(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IValidator<CreateCompanyRequest> createValidator,
        IEmailService emailService,
        IOptions<SmtpOptions> smtpOptions,
        ILogger<CompanyService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _createValidator = createValidator;
        _emailService = emailService;
        _smtpOptions = smtpOptions.Value;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<CompanyDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var companies = await _context.Companies
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        var companyIds = companies.Select(c => c.Id).ToList();

        var employeeCounts = await _context.Employees
            .AsNoTracking()
            .Where(e => companyIds.Contains(e.CompanyId))
            .GroupBy(e => e.CompanyId)
            .Select(g => new { CompanyId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CompanyId, x => x.Count, cancellationToken);

        var admins = await _context.Users
            .AsNoTracking()
            .Where(u => u.CompanyId != null
                && companyIds.Contains(u.CompanyId.Value)
                && u.UserRoles.Any(ur => ur.Role.Name == RoleConstants.CompanyAdmin))
            .Select(u => new
            {
                CompanyId = u.CompanyId!.Value,
                u.Email,
                FirstName = u.Person != null ? u.Person.FirstName : null,
                LastName = u.Person != null ? u.Person.LastName : null,
                u.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var firstAdminByCompany = admins
            .GroupBy(a => a.CompanyId)
            .ToDictionary(g => g.Key, g => g.OrderBy(a => a.CreatedAt).First());

        IReadOnlyList<CompanyDto> result = companies
            .Select(company =>
            {
                firstAdminByCompany.TryGetValue(company.Id, out var admin);
                var adminName = admin is null
                    ? null
                    : $"{admin.FirstName} {admin.LastName}".Trim();

                return ToDto(
                    company,
                    employeeCounts.GetValueOrDefault(company.Id),
                    string.IsNullOrWhiteSpace(adminName) ? null : adminName,
                    admin?.Email);
            })
            .ToList();

        return Result.Success(result);
    }

    public async Task<Result<CompanyDto>> UpdateStatusAsync(Guid companyId, UpdateCompanyStatusRequest request, CancellationToken cancellationToken = default)
    {
        var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
        if (company == null)
        {
            return Result.Failure<CompanyDto>(CompanyErrors.NotFound);
        }

        company.IsActive = request.IsActive;
        await _context.SaveChangesAsync(cancellationToken);

        var employeeCount = await _context.Employees.CountAsync(e => e.CompanyId == companyId, cancellationToken);
        return Result.Success(ToDto(company, employeeCount, null, null));
    }

    public async Task<Result<CreateCompanyResponse>> CreateAsync(CreateCompanyRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return Result.Failure<CreateCompanyResponse>(Error.Validation("Company.InvalidData", "Revisa los datos de la empresa.", errors));
        }

        var taxId = request.TaxId.Trim();
        var adminDocument = request.AdminDocumentNumber.Trim();
        var adminEmail = request.AdminEmail.Trim().ToLowerInvariant();

        if (await _context.Companies.AnyAsync(c => c.TaxId == taxId, cancellationToken))
        {
            return Result.Failure<CreateCompanyResponse>(CompanyErrors.TaxIdAlreadyExists);
        }

        if (await _context.Persons.AnyAsync(p => p.DocumentType == DocumentType.NationalId && p.DocumentNumber == adminDocument, cancellationToken))
        {
            return Result.Failure<CreateCompanyResponse>(CompanyErrors.AdminDocumentAlreadyExists);
        }

        if (await _context.Users.AnyAsync(u => u.Email == adminEmail, cancellationToken))
        {
            return Result.Failure<CreateCompanyResponse>(CompanyErrors.AdminEmailAlreadyExists);
        }

        var companyAdminRoleId = await _context.Roles
            .Where(r => r.Name == RoleConstants.CompanyAdmin)
            .Select(r => r.Id)
            .FirstAsync(cancellationToken);

        var temporaryPassword = TemporaryPasswordGenerator.Generate();

        var company = new Company
        {
            BusinessName = request.BusinessName.Trim(),
            TradeName = request.TradeName.Trim(),
            TaxId = taxId,
            Email = NullIfBlank(request.Email)?.ToLowerInvariant(),
            Phone = NullIfBlank(request.Phone),
            Address = NullIfBlank(request.Address),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            IsActive = true
        };

        var user = new User
        {
            Email = adminEmail,
            PasswordHash = _passwordHasher.HashPassword(temporaryPassword),
            IsActive = true,
            EmailConfirmed = false,
            MustChangePassword = true,
            CompanyId = company.Id
        };

        var person = new Person
        {
            UserId = user.Id,
            User = user,
            FirstName = request.AdminFirstName.Trim(),
            LastName = request.AdminLastName.Trim(),
            DocumentType = DocumentType.NationalId,
            DocumentNumber = adminDocument
        };

        _context.Companies.Add(company);
        foreach (var departmentName in DefaultDepartments.Names)
        {
            _context.Departments.Add(new Department { CompanyId = company.Id, Name = departmentName });
        }
        _context.Users.Add(user);
        _context.Persons.Add(person);
        _context.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = companyAdminRoleId,
            AssignedAt = DateTimeOffset.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        var adminFullName = $"{person.FirstName} {person.LastName}".Trim();

        var welcomeEmailSent = false;
        try
        {
            var loginUrl = $"{_smtpOptions.ClientBaseUrl.TrimEnd('/')}/login";
            await _emailService.SendTemplateEmailAsync(
                adminEmail,
                "¡Bienvenido a Ate! Tu cuenta de RRHH ha sido creada",
                EmailTemplateType.WelcomeUser,
                new WelcomeUserEmailModel(adminFullName, adminEmail, temporaryPassword, loginUrl),
                cancellationToken: cancellationToken);
            welcomeEmailSent = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to company admin {Email}", adminEmail);
        }

        return Result.Success(new CreateCompanyResponse(
            ToDto(company, 0, adminFullName, adminEmail),
            welcomeEmailSent));
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static CompanyDto ToDto(Company company, int employeeCount, string? adminName, string? adminEmail) =>
        new(
            company.Id,
            company.BusinessName,
            company.TradeName,
            company.TaxId,
            company.Email,
            company.Phone,
            company.Address,
            company.LogoUrl,
            company.Latitude,
            company.Longitude,
            company.IsActive,
            employeeCount,
            adminName,
            adminEmail,
            company.CreatedAt);
}
