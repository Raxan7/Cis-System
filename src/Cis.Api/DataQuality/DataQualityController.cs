using Cis.Api.Common;
using Cis.Api.Security;
using Cis.Application.Common.Interfaces;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.DataQuality;
using Microsoft.AspNetCore.Mvc;

namespace Cis.Api.DataQuality;

[ApiController]
[Route("api/data-quality")]
public sealed class DataQualityController : ControllerBase
{
    private readonly IDataQualityService _dataQualityService;

    public DataQualityController(IDataQualityService dataQualityService)
    {
        _dataQualityService = dataQualityService;
    }

    [HttpPost("rules")]
    [RequirePermission(Permissions.DataQuality.RulesManage)]
    public async Task<ActionResult<ApiResponse<DataQualityRuleDto>>> CreateRule([FromBody] CreateDataQualityRuleRequest request, CancellationToken cancellationToken)
    {
        var rule = await _dataQualityService.CreateRuleAsync(request, cancellationToken);
        return Created($"/api/data-quality/rules/{rule.Id}", ApiResponse<DataQualityRuleDto>.Success(rule, HttpContext.TraceIdentifier));
    }

    [HttpPost("check-runs")]
    [RequirePermission(Permissions.DataQuality.CheckRunsCreate)]
    public async Task<ActionResult<ApiResponse<DataQualityCheckRunDto>>> CreateCheckRun(CancellationToken cancellationToken)
    {
        var run = await _dataQualityService.CreateCheckRunAsync(cancellationToken);
        return Created($"/api/data-quality/check-runs/{run.Id}", ApiResponse<DataQualityCheckRunDto>.Success(run, HttpContext.TraceIdentifier));
    }

    [HttpGet("exceptions")]
    [RequirePermission(Permissions.DataQuality.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<DataQualityExceptionDto>>>> GetExceptions([FromQuery] PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var exceptions = await _dataQualityService.GetExceptionsAsync(cancellationToken);
        return this.OkPaged(exceptions, pagination);
    }

    [HttpPost("exceptions/{id:guid}/assign")]
    [RequirePermission(Permissions.DataQuality.ExceptionsManage)]
    public async Task<ActionResult<ApiResponse<DataQualityExceptionDto>>> Assign(Guid id, [FromBody] AssignDataQualityExceptionRequest request, CancellationToken cancellationToken)
    {
        var exception = await _dataQualityService.AssignExceptionAsync(id, request, cancellationToken);
        return Ok(ApiResponse<DataQualityExceptionDto>.Success(exception, HttpContext.TraceIdentifier));
    }

    [HttpPost("exceptions/{id:guid}/resolve")]
    [RequirePermission(Permissions.DataQuality.ExceptionsResolve)]
    public async Task<ActionResult<ApiResponse<DataQualityExceptionDto>>> Resolve(Guid id, [FromBody] ResolveDataQualityExceptionRequest request, CancellationToken cancellationToken)
    {
        var exception = await _dataQualityService.ResolveExceptionAsync(id, request, cancellationToken);
        return Ok(ApiResponse<DataQualityExceptionDto>.Success(exception, HttpContext.TraceIdentifier));
    }

    [HttpGet("dashboard")]
    [RequirePermission(Permissions.DataQuality.Read)]
    public async Task<ActionResult<ApiResponse<DataQualityDashboardDto>>> GetDashboard(CancellationToken cancellationToken)
    {
        var dashboard = await _dataQualityService.GetDashboardAsync(cancellationToken);
        return Ok(ApiResponse<DataQualityDashboardDto>.Success(dashboard, HttpContext.TraceIdentifier));
    }
}
