using Ate.Api.Extensions;
using Ate.Application.Common.Interfaces;
using Ate.Application.Common.Models;
using Ate.Application.Features.Employees.DTOs;
using Ate.Domain.Constants;
using Ate.Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ate.Api.Controllers;

[ApiController]
[Route("api/v1/company/employees")]
[Produces("application/json")]
[Tags("Company Employees")]
[Authorize(Roles = RoleConstants.CompanyAdmin)]
public class CompanyEmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public CompanyEmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    [HttpGet]
    [EndpointSummary("List the employees of the administrator's company")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<EmployeeDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<EmployeeDto>>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetEmployees(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return ResultExtensions.ToErrorActionResult<IReadOnlyList<EmployeeDto>>(AuthErrors.InvalidToken, this);
        }

        var result = await _employeeService.GetCompanyEmployeesAsync(userId.Value, cancellationToken);
        return result.ToActionResult(this, "Employees retrieved successfully.");
    }

    [HttpPost]
    [EndpointSummary("Register a new employee in the administrator's company")]
    [EndpointDescription("Creates the user account, person profile, Employee role and employee record in a single transaction. Returns a one-time temporary password.")]
    [ProducesResponseType(typeof(ApiResponse<CreateEmployeeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CreateEmployeeResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<CreateEmployeeResponse>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return ResultExtensions.ToErrorActionResult<CreateEmployeeResponse>(AuthErrors.InvalidToken, this);
        }

        var result = await _employeeService.CreateAsync(userId.Value, request, cancellationToken);
        return result.ToActionResult(this, "Employee created successfully.");
    }

    [HttpPatch("{employeeId:guid}/status")]
    [EndpointSummary("Activate or deactivate an employee")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EmployeeDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(Guid employeeId, [FromBody] UpdateEmployeeStatusRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return ResultExtensions.ToErrorActionResult<EmployeeDto>(AuthErrors.InvalidToken, this);
        }

        var result = await _employeeService.UpdateStatusAsync(userId.Value, employeeId, request, cancellationToken);
        return result.ToActionResult(this, "Employee status updated successfully.");
    }
}
