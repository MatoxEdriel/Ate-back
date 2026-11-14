using Ate.Api.Extensions;
using Ate.Application.Common.Interfaces;
using Ate.Application.Common.Models;
using Ate.Application.Features.Payroll.DTOs;
using Ate.Domain.Constants;
using Ate.Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ate.Api.Controllers;

[ApiController]
[Route("api/v1/employee/settlements")]
[Produces("application/json")]
[Tags("Employee Settlements")]
[Authorize(Roles = RoleConstants.Employee)]
public class EmployeeSettlementsController : ControllerBase
{
    private readonly IPayrollSettlementService _payrollService;

    public EmployeeSettlementsController(IPayrollSettlementService payrollService)
    {
        _payrollService = payrollService;
    }

    [HttpGet("current")]
    [EndpointSummary("Spending of the current month (not closed yet)")]
    [ProducesResponseType(typeof(ApiResponse<CurrentSpendingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrent(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return ResultExtensions.ToErrorActionResult<CurrentSpendingDto>(AuthErrors.InvalidToken, this);
        }

        var result = await _payrollService.GetCurrentSpendingAsync(userId.Value, cancellationToken);
        return result.ToActionResult(this, "Current spending retrieved successfully.");
    }

    [HttpGet]
    [EndpointSummary("Payroll settlements of the authenticated employee, newest first")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<EmployeeSettlementDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSettlements(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return ResultExtensions.ToErrorActionResult<IReadOnlyList<EmployeeSettlementDto>>(AuthErrors.InvalidToken, this);
        }

        var result = await _payrollService.GetEmployeeSettlementsAsync(userId.Value, cancellationToken);
        return result.ToActionResult(this, "Settlements retrieved successfully.");
    }

    [HttpGet("{settlementId}/pdf")]
    [EndpointSummary("Download one of the employee's settlements as PDF")]
    [Produces("application/pdf", "application/json")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadPdf(string settlementId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return ResultExtensions.ToErrorActionResult<object>(AuthErrors.InvalidToken, this);
        }

        var result = await _payrollService.GetEmployeeSettlementPdfAsync(userId.Value, settlementId, cancellationToken);
        return result.IsSuccess
            ? File(result.Value.Content, "application/pdf", result.Value.FileName)
            : ResultExtensions.ToErrorActionResult<object>(result.Error, this);
    }
}
