namespace Ate.Application.Features.Profile.DTOs;

/// <summary>Work data of an employee inside their company.</summary>
public record ProfileEmploymentDto(
    string? EmployeeCode,
    string? DepartmentName,
    bool IsIntern);
