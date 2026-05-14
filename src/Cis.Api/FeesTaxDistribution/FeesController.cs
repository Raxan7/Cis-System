using Cis.Api.Security;
using Cis.Application.Common.Interfaces;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.FeesTaxDistribution;
using Microsoft.AspNetCore.Mvc;

namespace Cis.Api.FeesTaxDistribution;

[ApiController]
[Route("api/fees")]
public sealed class FeesController : ControllerBase
{
    private readonly IFeesTaxDistributionService _feesTaxDistributionService;

    public FeesController(IFeesTaxDistributionService feesTaxDistributionService)
    {
        _feesTaxDistributionService = feesTaxDistributionService;
    }

    [HttpPost("accrual-runs")]
    [RequirePermission(Permissions.FeesTaxDistribution.AccrualRunsCreate)]
    [ProducesResponseType(typeof(ApiResponse<FeeAccrualRunDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<FeeAccrualRunDto>>> CreateAccrualRun([FromBody] CreateFeeAccrualRunRequest request, CancellationToken cancellationToken)
    {
        var run = await _feesTaxDistributionService.CreateFeeAccrualRunAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetAccrualRun), new { id = run.Id }, ApiResponse<FeeAccrualRunDto>.Success(run, HttpContext.TraceIdentifier));
    }

    [HttpGet("accrual-runs/{id:guid}")]
    [RequirePermission(Permissions.FeesTaxDistribution.Read)]
    [ProducesResponseType(typeof(ApiResponse<FeeAccrualRunDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<FeeAccrualRunDto>>> GetAccrualRun(Guid id, CancellationToken cancellationToken)
    {
        var run = await _feesTaxDistributionService.GetFeeAccrualRunAsync(id, cancellationToken);
        return Ok(ApiResponse<FeeAccrualRunDto>.Success(run, HttpContext.TraceIdentifier));
    }

    [HttpPost("waivers")]
    [RequirePermission(Permissions.FeesTaxDistribution.WaiversCreate)]
    [ProducesResponseType(typeof(ApiResponse<FeeWaiverRequestDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<FeeWaiverRequestDto>>> CreateWaiver([FromBody] CreateFeeWaiverRequest request, CancellationToken cancellationToken)
    {
        var waiver = await _feesTaxDistributionService.CreateFeeWaiverAsync(request, cancellationToken);
        return Created($"/api/fees/waivers/{waiver.Id}", ApiResponse<FeeWaiverRequestDto>.Success(waiver, HttpContext.TraceIdentifier));
    }

    [HttpPost("waivers/{id:guid}/approve")]
    [RequirePermission(Permissions.FeesTaxDistribution.WaiversApprove)]
    [ProducesResponseType(typeof(ApiResponse<FeeWaiverRequestDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<FeeWaiverRequestDto>>> ApproveWaiver(Guid id, [FromBody] ApproveFeeWaiverRequest request, CancellationToken cancellationToken)
    {
        var waiver = await _feesTaxDistributionService.ApproveFeeWaiverAsync(id, request, cancellationToken);
        return Ok(ApiResponse<FeeWaiverRequestDto>.Success(waiver, HttpContext.TraceIdentifier));
    }
}
