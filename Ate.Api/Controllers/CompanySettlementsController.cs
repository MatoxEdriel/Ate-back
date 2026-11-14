using Ate.Api.Extensions;
using Ate.Application.Common.Interfaces;
using Ate.Application.Common.Models;
using Ate.Application.Features.Payroll.DTOs;
using Ate.Domain.Constants;
using Ate.Domain.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ate.Api.Controllers;

/// <summary>
/// Nómina para RRHH: totales a descontar por colaborador y uso agregado de la empresa.
/// Nunca expone qué pidió cada colaborador.
/// </summary>
[ApiController]
[Route("api/v1/company")]
[Produces("application/json")]
[Tags("Company Payroll")]
[Authorize(Roles = RoleConstants.CompanyAdmin)]
public class CompanySettlementsController : ControllerBase
{
    private readonly IPayrollSettlementService _payrollService;

    public CompanySettlementsController(IPayrollSettlementService payrollService)
    {
        _payrollService = payrollService;
    }

    [HttpGet("settlements")]
    [EndpointSummary("Payroll settlements of the company for a month (yyyy-MM)")]
    [EndpointDescription("Only name, document number and total per employee. Order details are never returned.")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CompanySettlementDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CompanySettlementDto>>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSettlements([FromQuery] string month, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return ResultExtensions.ToErrorActionResult<IReadOnlyList<CompanySettlementDto>>(AuthErrors.InvalidToken, this);
        }

        var result = await _payrollService.GetCompanySettlementsAsync(userId.Value, month, cancellationToken);
        return result.ToActionResult(this, "Settlements retrieved successfully.");
    }

    [HttpGet("settlements/monthly/{month}/pdf")]
    [EndpointSummary("Download the monthly payroll deductions of the company as PDF")]
    [Produces("application/pdf", "application/json")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadMonthlyPdf(string month, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return ResultExtensions.ToErrorActionResult<object>(AuthErrors.InvalidToken, this);
        }

        return ToFile(await _payrollService.GetCompanyMonthlyPdfAsync(userId.Value, month, cancellationToken));
    }

    [HttpGet("settlements/{settlementId}/pdf")]
    [EndpointSummary("Download a termination settlement (liquidación) of the company as PDF")]
    [Produces("application/pdf", "application/json")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadTerminationPdf(string settlementId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return ResultExtensions.ToErrorActionResult<object>(AuthErrors.InvalidToken, this);
        }

        return ToFile(await _payrollService.GetCompanyTerminationPdfAsync(userId.Value, settlementId, cancellationToken));
    }

    [HttpGet("usage")]
    [EndpointSummary("Aggregate usage of Ate in the whole company for a month (yyyy-MM)")]
    [ProducesResponseType(typeof(ApiResponse<CompanyUsageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CompanyUsageDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUsage([FromQuery] string month, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return ResultExtensions.ToErrorActionResult<CompanyUsageDto>(AuthErrors.InvalidToken, this);
        }

        var result = await _payrollService.GetCompanyUsageAsync(userId.Value, month, cancellationToken);
        return result.ToActionResult(this, "Usage retrieved successfully.");
    }

    private IActionResult ToFile(Ate.Domain.Common.Result<PdfFile> result) =>
        result.IsSuccess
            ? File(result.Value.Content, "application/pdf", result.Value.FileName)
            : ResultExtensions.ToErrorActionResult<object>(result.Error, this);
}
