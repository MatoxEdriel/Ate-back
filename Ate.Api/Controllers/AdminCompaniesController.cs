using Ate.Application.Common.Interfaces;
using Ate.Application.Common.Models;
using Ate.Application.Features.Companies.DTOs;
using Ate.Api.Extensions;
using Ate.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ate.Api.Controllers;

[ApiController]
[Route("api/v1/admin/companies")]
[Produces("application/json")]
[Tags("Admin Companies")]
[Authorize(Roles = RoleConstants.SuperAdmin)]
public class AdminCompaniesController : ControllerBase
{
    private readonly ICompanyService _companyService;

    public AdminCompaniesController(ICompanyService companyService)
    {
        _companyService = companyService;
    }

    [HttpGet]
    [EndpointSummary("List all the companies of the platform")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CompanyDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCompanies(CancellationToken cancellationToken)
    {
        var result = await _companyService.GetAllAsync(cancellationToken);
        return result.ToActionResult(this, "Companies retrieved successfully.");
    }

    [HttpPost]
    [EndpointSummary("Create a company together with its HR administrator")]
    [EndpointDescription("Creates the company, the CompanyAdmin account, its person profile and role in a single transaction, and emails the temporary password only to the new account (it is never returned in the response).")]
    [ProducesResponseType(typeof(ApiResponse<CreateCompanyResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CreateCompanyResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<CreateCompanyResponse>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCompany([FromBody] CreateCompanyRequest request, CancellationToken cancellationToken)
    {
        var result = await _companyService.CreateAsync(request, cancellationToken);
        return result.ToActionResult(this, "Company created successfully.");
    }

    [HttpPatch("{companyId:guid}/status")]
    [EndpointSummary("Activate or deactivate a company")]
    [EndpointDescription("A deactivated company can no longer sign in (HR admin and employees) nor order food. Its data is kept.")]
    [ProducesResponseType(typeof(ApiResponse<CompanyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CompanyDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(Guid companyId, [FromBody] UpdateCompanyStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await _companyService.UpdateStatusAsync(companyId, request, cancellationToken);
        return result.ToActionResult(this, "Company status updated successfully.");
    }
}
