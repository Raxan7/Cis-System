using Cis.Api.Security;
using Cis.Application.Common.Interfaces;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.Cases;
using Microsoft.AspNetCore.Mvc;

namespace Cis.Api.Cases;

[ApiController]
[Route("api/cases")]
public sealed class CasesController : ControllerBase
{
    private readonly ICaseManagementService _caseManagementService;

    public CasesController(ICaseManagementService caseManagementService)
    {
        _caseManagementService = caseManagementService;
    }

    [HttpPost]
    [RequirePermission(Permissions.Cases.Create)]
    public async Task<ActionResult<ApiResponse<ServiceCaseDto>>> Create([FromBody] CreateCaseRequest request, CancellationToken cancellationToken)
    {
        var serviceCase = await _caseManagementService.CreateCaseAsync(request, cancellationToken);
        return Created($"/api/cases/{serviceCase.Id}", ApiResponse<ServiceCaseDto>.Success(serviceCase, HttpContext.TraceIdentifier));
    }

    [HttpGet]
    [RequirePermission(Permissions.Cases.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ServiceCaseDto>>>> Get(CancellationToken cancellationToken)
    {
        var cases = await _caseManagementService.GetCasesAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<ServiceCaseDto>>.Success(cases, HttpContext.TraceIdentifier));
    }

    [HttpGet("dashboard")]
    [RequirePermission(Permissions.Cases.Read)]
    public async Task<ActionResult<ApiResponse<CaseDashboardDto>>> GetDashboard(CancellationToken cancellationToken)
    {
        var dashboard = await _caseManagementService.GetDashboardAsync(cancellationToken);
        return Ok(ApiResponse<CaseDashboardDto>.Success(dashboard, HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.Cases.Read)]
    public async Task<ActionResult<ApiResponse<ServiceCaseDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var serviceCase = await _caseManagementService.GetCaseAsync(id, cancellationToken);
        return Ok(ApiResponse<ServiceCaseDto>.Success(serviceCase, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/assign")]
    [RequirePermission(Permissions.Cases.Assign)]
    public async Task<ActionResult<ApiResponse<ServiceCaseDto>>> Assign(Guid id, [FromBody] AssignCaseRequest request, CancellationToken cancellationToken)
    {
        var serviceCase = await _caseManagementService.AssignCaseAsync(id, request, cancellationToken);
        return Ok(ApiResponse<ServiceCaseDto>.Success(serviceCase, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/actions")]
    [RequirePermission(Permissions.Cases.ActionsAdd)]
    public async Task<ActionResult<ApiResponse<ServiceCaseDto>>> AddAction(Guid id, [FromBody] AddCaseActionRequest request, CancellationToken cancellationToken)
    {
        var serviceCase = await _caseManagementService.AddActionAsync(id, request, cancellationToken);
        return Ok(ApiResponse<ServiceCaseDto>.Success(serviceCase, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/escalate")]
    [RequirePermission(Permissions.Cases.Escalate)]
    public async Task<ActionResult<ApiResponse<ServiceCaseDto>>> Escalate(Guid id, [FromBody] EscalateCaseRequest request, CancellationToken cancellationToken)
    {
        var serviceCase = await _caseManagementService.EscalateCaseAsync(id, request, cancellationToken);
        return Ok(ApiResponse<ServiceCaseDto>.Success(serviceCase, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/resolve")]
    [RequirePermission(Permissions.Cases.Resolve)]
    public async Task<ActionResult<ApiResponse<ServiceCaseDto>>> Resolve(Guid id, [FromBody] ResolveCaseRequest request, CancellationToken cancellationToken)
    {
        var serviceCase = await _caseManagementService.ResolveCaseAsync(id, request, cancellationToken);
        return Ok(ApiResponse<ServiceCaseDto>.Success(serviceCase, HttpContext.TraceIdentifier));
    }
}
