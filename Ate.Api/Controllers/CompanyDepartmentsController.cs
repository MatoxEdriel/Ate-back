using Ate.Api.Extensions;
using Ate.Application.Common.Interfaces;
using Ate.Application.Common.Models;
using Ate.Application.Features.Departments.DTOs;
using Ate.Domain.Constants;
using Ate.Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ate.Api.Controllers;

[ApiController]
[Route("api/v1/company/departments")]
[Produces("application/json")]
[Tags("Company Departments")]
[Authorize(Roles = RoleConstants.CompanyAdmin)]
public class CompanyDepartmentsController : ControllerBase
{
    private readonly IDepartmentService _departmentService;

    public CompanyDepartmentsController(IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    [HttpGet]
    [EndpointSummary("List the departments of the administrator's company")]
    [EndpointDescription("Each company defines its own departments. Includes how many employees belong to each one.")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DepartmentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDepartments(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return ResultExtensions.ToErrorActionResult<IReadOnlyList<DepartmentDto>>(AuthErrors.InvalidToken, this);
        }

        var result = await _departmentService.GetCompanyDepartmentsAsync(userId.Value, cancellationToken);
        return result.ToActionResult(this, "Departments retrieved successfully.");
    }

    [HttpPost]
    [EndpointSummary("Create a department in the administrator's company")]
    [ProducesResponseType(typeof(ApiResponse<DepartmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DepartmentDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<DepartmentDto>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateDepartment([FromBody] SaveDepartmentRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return ResultExtensions.ToErrorActionResult<DepartmentDto>(AuthErrors.InvalidToken, this);
        }

        var result = await _departmentService.CreateAsync(userId.Value, request, cancellationToken);
        return result.ToActionResult(this, "Department created successfully.");
    }

    [HttpPut("{departmentId:guid}")]
    [EndpointSummary("Rename a department")]
    [ProducesResponseType(typeof(ApiResponse<DepartmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DepartmentDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<DepartmentDto>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RenameDepartment(Guid departmentId, [FromBody] SaveDepartmentRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return ResultExtensions.ToErrorActionResult<DepartmentDto>(AuthErrors.InvalidToken, this);
        }

        var result = await _departmentService.RenameAsync(userId.Value, departmentId, request, cancellationToken);
        return result.ToActionResult(this, "Department renamed successfully.");
    }

    [HttpDelete("{departmentId:guid}")]
    [EndpointSummary("Delete a department")]
    [EndpointDescription("Its employees are left without department until HR reassigns them. Past orders keep the department name they had.")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDepartment(Guid departmentId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return ResultExtensions.ToErrorActionResult<object>(AuthErrors.InvalidToken, this);
        }

        var result = await _departmentService.DeleteAsync(userId.Value, departmentId, cancellationToken);
        return result.ToActionResult(this, "Department deleted successfully.");
    }
}
