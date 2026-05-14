using Cis.Api.Security;
using Cis.Application.Common.Interfaces;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.NAV;
using Microsoft.AspNetCore.Mvc;

namespace Cis.Api.NAV;

[ApiController]
[Route("api/nav")]
public sealed class NavController : ControllerBase
{
    private readonly INavService _navService;

    public NavController(INavService navService)
    {
        _navService = navService;
    }

    [HttpPost("valuation-runs")]
    [RequirePermission(Permissions.Nav.ValuationRunsCreate)]
    [ProducesResponseType(typeof(ApiResponse<ValuationRunDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<ValuationRunDto>>> CreateValuationRun(
        [FromBody] CreateValuationRunRequest request,
        CancellationToken cancellationToken)
    {
        var run = await _navService.CreateValuationRunAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetHistory), new { schemeId = run.SchemeId, schemeClassId = run.SchemeClassId }, ApiResponse<ValuationRunDto>.Success(run, HttpContext.TraceIdentifier));
    }

    [HttpPost("valuation-runs/{id:guid}/calculate")]
    [RequirePermission(Permissions.Nav.ValuationRunsCalculate)]
    [ProducesResponseType(typeof(ApiResponse<ValuationRunDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ValuationRunDto>>> CalculateValuationRun(Guid id, CancellationToken cancellationToken)
    {
        var run = await _navService.CalculateValuationRunAsync(id, cancellationToken);
        return Ok(ApiResponse<ValuationRunDto>.Success(run, HttpContext.TraceIdentifier));
    }

    [HttpPost("valuation-runs/{id:guid}/submit")]
    [RequirePermission(Permissions.Nav.ValuationRunsSubmit)]
    [ProducesResponseType(typeof(ApiResponse<ValuationRunDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ValuationRunDto>>> SubmitValuationRun(
        Guid id,
        [FromBody] NavWorkflowActionRequest request,
        CancellationToken cancellationToken)
    {
        var run = await _navService.SubmitValuationRunAsync(id, request, cancellationToken);
        return Ok(ApiResponse<ValuationRunDto>.Success(run, HttpContext.TraceIdentifier));
    }

    [HttpPost("valuation-runs/{id:guid}/check")]
    [RequirePermission(Permissions.Nav.ValuationRunsCheck)]
    [ProducesResponseType(typeof(ApiResponse<ValuationRunDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ValuationRunDto>>> CheckValuationRun(
        Guid id,
        [FromBody] NavWorkflowActionRequest request,
        CancellationToken cancellationToken)
    {
        var run = await _navService.CheckValuationRunAsync(id, request, cancellationToken);
        return Ok(ApiResponse<ValuationRunDto>.Success(run, HttpContext.TraceIdentifier));
    }

    [HttpPost("valuation-runs/{id:guid}/approve")]
    [RequirePermission(Permissions.Nav.ValuationRunsApprove)]
    [ProducesResponseType(typeof(ApiResponse<ValuationRunDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ValuationRunDto>>> ApproveValuationRun(
        Guid id,
        [FromBody] NavWorkflowActionRequest request,
        CancellationToken cancellationToken)
    {
        var run = await _navService.ApproveValuationRunAsync(id, request, cancellationToken);
        return Ok(ApiResponse<ValuationRunDto>.Success(run, HttpContext.TraceIdentifier));
    }

    [HttpPost("valuation-runs/{id:guid}/publish")]
    [RequirePermission(Permissions.Nav.ValuationRunsPublish)]
    [ProducesResponseType(typeof(ApiResponse<NavPublicationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<NavPublicationDto>>> PublishValuationRun(
        Guid id,
        [FromBody] NavWorkflowActionRequest request,
        CancellationToken cancellationToken)
    {
        var publication = await _navService.PublishValuationRunAsync(id, request, cancellationToken);
        return Ok(ApiResponse<NavPublicationDto>.Success(publication, HttpContext.TraceIdentifier));
    }

    [HttpPost("overrides")]
    [RequirePermission(Permissions.Nav.OverridesCreate)]
    [ProducesResponseType(typeof(ApiResponse<ManualValuationOverrideDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<ManualValuationOverrideDto>>> CreateOverride(
        [FromBody] CreateManualValuationOverrideRequest request,
        CancellationToken cancellationToken)
    {
        var manualOverride = await _navService.CreateManualOverrideAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetHistory), new { schemeId = manualOverride.SchemeId, schemeClassId = manualOverride.SchemeClassId }, ApiResponse<ManualValuationOverrideDto>.Success(manualOverride, HttpContext.TraceIdentifier));
    }

    [HttpPost("overrides/{id:guid}/approve")]
    [RequirePermission(Permissions.Nav.OverridesApprove)]
    [ProducesResponseType(typeof(ApiResponse<ManualValuationOverrideDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ManualValuationOverrideDto>>> ApproveOverride(
        Guid id,
        [FromBody] ApproveManualValuationOverrideRequest request,
        CancellationToken cancellationToken)
    {
        var manualOverride = await _navService.ApproveManualOverrideAsync(id, request, cancellationToken);
        return Ok(ApiResponse<ManualValuationOverrideDto>.Success(manualOverride, HttpContext.TraceIdentifier));
    }

    [HttpPost("restatements")]
    [RequirePermission(Permissions.Nav.RestatementsCreate)]
    [ProducesResponseType(typeof(ApiResponse<NavRestatementDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<NavRestatementDto>>> CreateRestatement(
        [FromBody] CreateNavRestatementRequest request,
        CancellationToken cancellationToken)
    {
        var restatement = await _navService.CreateRestatementAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetHistory), new { schemeId = restatement.SchemeId, schemeClassId = restatement.SchemeClassId }, ApiResponse<NavRestatementDto>.Success(restatement, HttpContext.TraceIdentifier));
    }

    [HttpGet("history")]
    [RequirePermission(Permissions.Nav.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<NavPublicationDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<NavPublicationDto>>>> GetHistory(
        [FromQuery] Guid? schemeId,
        [FromQuery] Guid? schemeClassId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        CancellationToken cancellationToken)
    {
        var history = await _navService.GetHistoryAsync(schemeId, schemeClassId, fromDate, toDate, cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<NavPublicationDto>>.Success(history, HttpContext.TraceIdentifier));
    }

    [HttpGet("reconstruct")]
    [RequirePermission(Permissions.Nav.Read)]
    [ProducesResponseType(typeof(ApiResponse<NavReconstructionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<NavReconstructionDto>>> Reconstruct(
        [FromQuery] Guid schemeId,
        [FromQuery] DateOnly valuationDate,
        CancellationToken cancellationToken)
    {
        var reconstruction = await _navService.ReconstructAsync(schemeId, valuationDate, cancellationToken);
        return Ok(ApiResponse<NavReconstructionDto>.Success(reconstruction, HttpContext.TraceIdentifier));
    }
}
