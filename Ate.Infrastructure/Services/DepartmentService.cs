using Ate.Application.Common.Interfaces;
using Ate.Application.Features.Departments.DTOs;
using Ate.Domain.Common;
using Ate.Domain.Entities;
using Ate.Domain.Errors;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Ate.Infrastructure.Services;

public class DepartmentService : IDepartmentService, IScopedDependency
{
    private readonly IApplicationDbContext _context;
    private readonly IValidator<SaveDepartmentRequest> _validator;

    public DepartmentService(IApplicationDbContext context, IValidator<SaveDepartmentRequest> validator)
    {
        _context = context;
        _validator = validator;
    }

    public async Task<Result<IReadOnlyList<DepartmentDto>>> GetCompanyDepartmentsAsync(Guid adminUserId, CancellationToken cancellationToken = default)
    {
        var companyId = await GetAdminCompanyIdAsync(adminUserId, cancellationToken);
        if (!companyId.HasValue)
        {
            return Result.Failure<IReadOnlyList<DepartmentDto>>(EmployeeErrors.CompanyNotAssigned);
        }

        IReadOnlyList<DepartmentDto> departments = await _context.Departments
            .AsNoTracking()
            .Where(d => d.CompanyId == companyId.Value)
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentDto(d.Id, d.Name, d.Employees.Count(), d.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(departments);
    }

    public async Task<Result<DepartmentDto>> CreateAsync(Guid adminUserId, SaveDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(request, cancellationToken);
        if (validation is { } validationError)
        {
            return Result.Failure<DepartmentDto>(validationError);
        }

        var companyId = await GetAdminCompanyIdAsync(adminUserId, cancellationToken);
        if (!companyId.HasValue)
        {
            return Result.Failure<DepartmentDto>(EmployeeErrors.CompanyNotAssigned);
        }

        var name = NormalizeName(request.Name);
        if (await NameTakenAsync(companyId.Value, name, null, cancellationToken))
        {
            return Result.Failure<DepartmentDto>(DepartmentErrors.NameAlreadyExists);
        }

        var department = new Department { CompanyId = companyId.Value, Name = name };
        _context.Departments.Add(department);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(new DepartmentDto(department.Id, department.Name, 0, department.CreatedAt));
    }

    public async Task<Result<DepartmentDto>> RenameAsync(Guid adminUserId, Guid departmentId, SaveDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(request, cancellationToken);
        if (validation is { } validationError)
        {
            return Result.Failure<DepartmentDto>(validationError);
        }

        var companyId = await GetAdminCompanyIdAsync(adminUserId, cancellationToken);
        if (!companyId.HasValue)
        {
            return Result.Failure<DepartmentDto>(EmployeeErrors.CompanyNotAssigned);
        }

        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == departmentId && d.CompanyId == companyId.Value, cancellationToken);
        if (department == null)
        {
            return Result.Failure<DepartmentDto>(DepartmentErrors.NotFound);
        }

        var name = NormalizeName(request.Name);
        if (await NameTakenAsync(companyId.Value, name, departmentId, cancellationToken))
        {
            return Result.Failure<DepartmentDto>(DepartmentErrors.NameAlreadyExists);
        }

        department.Name = name;
        await _context.SaveChangesAsync(cancellationToken);

        var employeeCount = await _context.Employees.CountAsync(e => e.DepartmentId == departmentId, cancellationToken);
        return Result.Success(new DepartmentDto(department.Id, department.Name, employeeCount, department.CreatedAt));
    }

    /// <summary>
    /// Elimina el departamento; sus colaboradores quedan "Sin departamento" hasta que RRHH los reasigne.
    /// Los pedidos ya hechos conservan el nombre que tenían en ese momento.
    /// </summary>
    public async Task<Result> DeleteAsync(Guid adminUserId, Guid departmentId, CancellationToken cancellationToken = default)
    {
        var companyId = await GetAdminCompanyIdAsync(adminUserId, cancellationToken);
        if (!companyId.HasValue)
        {
            return Result.Failure(EmployeeErrors.CompanyNotAssigned);
        }

        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == departmentId && d.CompanyId == companyId.Value, cancellationToken);
        if (department == null)
        {
            return Result.Failure(DepartmentErrors.NotFound);
        }

        var employees = await _context.Employees
            .Where(e => e.DepartmentId == departmentId)
            .ToListAsync(cancellationToken);
        foreach (var employee in employees)
        {
            employee.DepartmentId = null;
        }

        _context.Departments.Remove(department);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private async Task<Error?> ValidateAsync(SaveDepartmentRequest request, CancellationToken cancellationToken)
    {
        var result = await _validator.ValidateAsync(request, cancellationToken);
        if (result.IsValid)
        {
            return null;
        }

        var errors = result.Errors.Select(e => e.ErrorMessage).ToList();
        return Error.Validation("Department.InvalidData", "Revisa el nombre del departamento.", errors);
    }

    private Task<bool> NameTakenAsync(Guid companyId, string name, Guid? exceptId, CancellationToken cancellationToken)
    {
        var lowered = name.ToLower();
        return _context.Departments.AnyAsync(
            d => d.CompanyId == companyId && d.Id != exceptId && d.Name.ToLower() == lowered,
            cancellationToken);
    }

    private static string NormalizeName(string name) =>
        string.Join(' ', name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    private async Task<Guid?> GetAdminCompanyIdAsync(Guid adminUserId, CancellationToken cancellationToken)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == adminUserId)
            .Select(u => u.CompanyId)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
