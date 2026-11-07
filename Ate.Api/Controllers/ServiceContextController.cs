using Ate.Api.Extensions;
using Ate.Application.Common.Interfaces;
using Ate.Application.Common.Models;
using Ate.Application.Features.ServiceContext.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ate.Api.Controllers;

[ApiController]
[Route("api/v1/service-context")]
[Produces("application/json")]
[Tags("Service Context")]
[Authorize]
public class ServiceContextController : ControllerBase
{
    private readonly IServiceContextService _serviceContextService;

    public ServiceContextController(IServiceContextService serviceContextService)
    {
        _serviceContextService = serviceContextService;
    }

    [HttpGet]
    [EndpointSummary("Get the platform clock and the current service day")]
    [EndpointDescription("Returns the business time zone, the server time (UTC instant), the service date (yyyy-MM-dd), the meal on offer (desayuno, almuerzo, merienda) and whether it is for tomorrow. Clients use it instead of their own clock.")]
    [ProducesResponseType(typeof(ApiResponse<ServiceContextDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ServiceContextDto>), StatusCodes.Status401Unauthorized)]
    public IActionResult GetServiceContext()
    {
        var result = _serviceContextService.GetCurrent();
        return result.ToActionResult(this, "Service context retrieved successfully.");
    }
}
