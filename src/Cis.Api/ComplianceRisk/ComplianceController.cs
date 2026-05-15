using Cis.Api.Common;
using Cis.Api.Security;
using Cis.Application.Common.Interfaces;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.ComplianceRisk;
using Microsoft.AspNetCore.Mvc;

namespace Cis.Api.ComplianceRisk;

[ApiController]
[Route("api/compliance")]
public sealed class ComplianceController : ControllerBase
{
    private readonly IComplianceRiskService _complianceRiskService;

    public ComplianceController(IComplianceRiskService complianceRiskService)
    {
        _complianceRiskService = complianceRiskService;
    }

    [HttpPost("limit-runs")]
    [RequirePermission(Permissions.ComplianceRisk.LimitRunsCreate)]
    [ProducesResponseType(typeof(ApiResponse<LimitCheckRunDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<LimitCheckRunDto>>> CreateLimitRun([FromBody] CreateLimitCheckRunRequest request, CancellationToken cancellationToken)
    {
        var run = await _complianceRiskService.CreateLimitCheckRunAsync(request, cancellationToken);
        return Created($"/api/compliance/limit-runs/{run.Id}", ApiResponse<LimitCheckRunDto>.Success(run, HttpContext.TraceIdentifier));
    }

    [HttpGet("breaches")]
    [RequirePermission(Permissions.ComplianceRisk.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<LimitBreachDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<LimitBreachDto>>>> GetBreaches([FromQuery] string? status, [FromQuery] PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var breaches = await _complianceRiskService.GetBreachesAsync(status, cancellationToken);
        return this.OkPaged(breaches, pagination);
    }

    [HttpPost("breaches/{id:guid}/assign")]
    [RequirePermission(Permissions.ComplianceRisk.BreachesManage)]
    [ProducesResponseType(typeof(ApiResponse<LimitBreachDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<LimitBreachDto>>> AssignBreach(Guid id, [FromBody] AssignBreachRequest request, CancellationToken cancellationToken)
    {
        var breach = await _complianceRiskService.AssignBreachAsync(id, request, cancellationToken);
        return Ok(ApiResponse<LimitBreachDto>.Success(breach, HttpContext.TraceIdentifier));
    }

    [HttpPost("breaches/{id:guid}/remediate")]
    [RequirePermission(Permissions.ComplianceRisk.BreachesManage)]
    [ProducesResponseType(typeof(ApiResponse<LimitBreachDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<LimitBreachDto>>> RemediateBreach(Guid id, [FromBody] RemediateBreachRequest request, CancellationToken cancellationToken)
    {
        var breach = await _complianceRiskService.RemediateBreachAsync(id, request, cancellationToken);
        return Ok(ApiResponse<LimitBreachDto>.Success(breach, HttpContext.TraceIdentifier));
    }

    [HttpPost("breaches/{id:guid}/close")]
    [RequirePermission(Permissions.ComplianceRisk.BreachesClose)]
    [ProducesResponseType(typeof(ApiResponse<LimitBreachDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<LimitBreachDto>>> CloseBreach(Guid id, [FromBody] CloseBreachRequest request, CancellationToken cancellationToken)
    {
        var breach = await _complianceRiskService.CloseBreachAsync(id, request, cancellationToken);
        return Ok(ApiResponse<LimitBreachDto>.Success(breach, HttpContext.TraceIdentifier));
    }
}
