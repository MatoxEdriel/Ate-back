using Ate.Application.Common.Interfaces;
using Ate.Application.Common.Models.Emails;
using Ate.Application.Features.Employees.DTOs;
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

public class EmployeeService : IEmployeeService, IScopedDependency
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IValidator<CreateEmployeeRequest> _createValidator;
    private readonly IEmailService _emailService;
    private readonly SmtpOptions _smtpOptions;
    private readonly ILogger<EmployeeService> _logger;

    public EmployeeService(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IValidator<CreateEmployeeRequest> createValidator,
        IEmailService emailService,
        IOptions<SmtpOptions> smtpOptions,
        ILogger<EmployeeService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _createValidator = createValidator;
        _emailService = emailService;
        _smtpOptions = smtpOptions.Value;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<EmployeeDto>>> GetCompanyEmployeesAsync(Guid adminUserId, CancellationToken cancellationToken = default)
    {
        var companyId = await GetAdminCompanyIdAsync(adminUserId, cancellationToken);
        if (!companyId.HasValue)
        {
            return Result.Failure<IReadOnlyList<EmployeeDto>>(EmployeeErrors.CompanyNotAssigned);
        }

        var employees = await _context.Employees
            .AsNoTracking()
            .Where(e => e.CompanyId == companyId.Value)
            .Include(e => e.Person)
                .ThenInclude(p => p.User)
            .Include(e => e.Department)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        IReadOnlyList<EmployeeDto> result = employees.Select(ToDto).ToList();
        return Result.Success(result);
    }

    public async Task<Result<CreateEmployeeResponse>> CreateAsync(Guid adminUserId, CreateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return Result.Failure<CreateEmployeeResponse>(Error.Validation("Employee.InvalidData", "Revisa los datos del colaborador.", errors));
        }

        var companyId = await GetAdminCompanyIdAsync(adminUserId, cancellationToken);
        if (!companyId.HasValue)
        {
            return Result.Failure<CreateEmployeeResponse>(EmployeeErrors.CompanyNotAssigned);
        }

        var documentNumber = request.DocumentNumber.Trim();
        var email = request.Email.Trim().ToLowerInvariant();
        var employeeCode = request.EmployeeCode.Trim();

        if (await _context.Persons.AnyAsync(p => p.DocumentType == DocumentType.NationalId && p.DocumentNumber == documentNumber, cancellationToken))
        {
            return Result.Failure<CreateEmployeeResponse>(EmployeeErrors.DocumentAlreadyExists);
        }

        if (await _context.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            return Result.Failure<CreateEmployeeResponse>(EmployeeErrors.EmailAlreadyExists);
        }

        if (await _context.Employees.AnyAsync(e => e.CompanyId == companyId.Value && e.EmployeeCode == employeeCode, cancellationToken))
        {
            return Result.Failure<CreateEmployeeResponse>(EmployeeErrors.CodeAlreadyExists);
        }

        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == request.DepartmentId && d.CompanyId == companyId.Value, cancellationToken);
        if (department == null)
        {
            return Result.Failure<CreateEmployeeResponse>(DepartmentErrors.NotFound);
        }

        var employeeRoleId = await _context.Roles
            .Where(r => r.Name == RoleConstants.Employee)
            .Select(r => r.Id)
            .FirstAsync(cancellationToken);

        var temporaryPassword = TemporaryPasswordGenerator.Generate();

        var user = new User
        {
            Email = email,
            PasswordHash = _passwordHasher.HashPassword(temporaryPassword),
            IsActive = true,
            EmailConfirmed = false,
            MustChangePassword = true
        };

        var person = new Person
        {
            UserId = user.Id,
            User = user,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            DocumentType = DocumentType.NationalId,
            DocumentNumber = documentNumber
        };

        var employee = new Employee
        {
            CompanyId = companyId.Value,
            PersonId = person.Id,
            Person = person,
            EmployeeCode = employeeCode,
            DepartmentId = department.Id,
            Department = department,
            MonthlyAllowance = 0m,
            IsIntern = request.IsIntern,
            Status = EmployeeStatus.Active
        };

        _context.Users.Add(user);
        _context.Persons.Add(person);
        _context.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = employeeRoleId,
            AssignedAt = DateTimeOffset.UtcNow
        });
        _context.Employees.Add(employee);

        await _context.SaveChangesAsync(cancellationToken);

        var welcomeEmailSent = false;
        try
        {
            var loginUrl = $"{_smtpOptions.ClientBaseUrl.TrimEnd('/')}/login";
            var fullName = $"{person.FirstName} {person.LastName}".Trim();
            await _emailService.SendTemplateEmailAsync(
                user.Email,
                "¡Bienvenido a Ate! Tu cuenta ha sido creada",
                EmailTemplateType.WelcomeUser,
                new WelcomeUserEmailModel(
                    fullName,
                    user.Email,
                    temporaryPassword,
                    loginUrl),
                cancellationToken: cancellationToken);
            welcomeEmailSent = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to {Email}", user.Email);
        }

        return Result.Success(new CreateEmployeeResponse(ToDto(employee), welcomeEmailSent));
    }

    public async Task<Result<EmployeeDto>> UpdateDepartmentAsync(Guid adminUserId, Guid employeeId, UpdateEmployeeDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = await GetAdminCompanyIdAsync(adminUserId, cancellationToken);
        if (!companyId.HasValue)
        {
            return Result.Failure<EmployeeDto>(EmployeeErrors.CompanyNotAssigned);
        }

        var employee = await _context.Employees
            .Include(e => e.Person)
                .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(e => e.Id == employeeId && e.CompanyId == companyId.Value, cancellationToken);

        if (employee == null)
        {
            return Result.Failure<EmployeeDto>(EmployeeErrors.NotFound);
        }

        Department? department = null;
        if (request.DepartmentId.HasValue)
        {
            department = await _context.Departments
                .FirstOrDefaultAsync(d => d.Id == request.DepartmentId.Value && d.CompanyId == companyId.Value, cancellationToken);
            if (department == null)
            {
                return Result.Failure<EmployeeDto>(DepartmentErrors.NotFound);
            }
        }

        employee.DepartmentId = department?.Id;
        employee.Department = department;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(ToDto(employee));
    }

    public async Task<Result<EmployeeDto>> UpdateStatusAsync(Guid adminUserId, Guid employeeId, UpdateEmployeeStatusRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = await GetAdminCompanyIdAsync(adminUserId, cancellationToken);
        if (!companyId.HasValue)
        {
            return Result.Failure<EmployeeDto>(EmployeeErrors.CompanyNotAssigned);
        }

        var employee = await _context.Employees
            .Include(e => e.Person)
                .ThenInclude(p => p.User)
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == employeeId && e.CompanyId == companyId.Value, cancellationToken);

        if (employee == null)
        {
            return Result.Failure<EmployeeDto>(EmployeeErrors.NotFound);
        }

        // "No vigente" means the person left the company; that change has its own flow.
        if (employee.Status == EmployeeStatus.NotCurrent)
        {
            return Result.Failure<EmployeeDto>(EmployeeErrors.NotCurrent);
        }

        employee.Status = request.IsActive ? EmployeeStatus.Active : EmployeeStatus.Inactive;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(ToDto(employee));
    }

    private async Task<Guid?> GetAdminCompanyIdAsync(Guid adminUserId, CancellationToken cancellationToken)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == adminUserId)
            .Select(u => u.CompanyId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static EmployeeDto ToDto(Employee employee)
    {
        return new EmployeeDto(
            employee.Id,
            employee.Person.DocumentNumber,
            employee.Person.FirstName,
            employee.Person.LastName,
            employee.Person.User?.Email ?? string.Empty,
            employee.EmployeeCode ?? string.Empty,
            employee.DepartmentId,
            employee.Department?.Name ?? string.Empty,
            employee.IsIntern,
            employee.Status == EmployeeStatus.Active,
            employee.Status.ToString(),
            employee.CreatedAt);
    }
}
