using Cis.Api.Security;
using Cis.Application.Common.Interfaces;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.UnitRegister;
using Microsoft.AspNetCore.Mvc;

namespace Cis.Api.UnitRegister;

[ApiController]
[Route("api/unit-register")]
public sealed class UnitRegisterController : ControllerBase
{
    private readonly IUnitRegisterService _unitRegisterService;

    public UnitRegisterController(IUnitRegisterService unitRegisterService)
    {
        _unitRegisterService = unitRegisterService;
    }

    [HttpGet("schemes/{schemeId:guid}/classes/{classId:guid}")]
    [RequirePermission(Permissions.UnitRegister.Read)]
    [ProducesResponseType(typeof(ApiResponse<SchemeClassUnitRegisterDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SchemeClassUnitRegisterDto>>> GetSchemeClassRegister(Guid schemeId, Guid classId, CancellationToken cancellationToken)
    {
        var register = await _unitRegisterService.GetSchemeClassRegisterAsync(schemeId, classId, cancellationToken);
        return Ok(ApiResponse<SchemeClassUnitRegisterDto>.Success(register, HttpContext.TraceIdentifier));
    }

    [HttpGet("investors/{investorId:guid}/holdings")]
    [RequirePermission(Permissions.UnitRegister.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<UnitHoldingDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<UnitHoldingDto>>>> GetInvestorHoldings(Guid investorId, CancellationToken cancellationToken)
    {
        var holdings = await _unitRegisterService.GetInvestorHoldingsAsync(investorId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<UnitHoldingDto>>.Success(holdings, HttpContext.TraceIdentifier));
    }

    [HttpGet("investors/{investorId:guid}/historical")]
    [RequirePermission(Permissions.UnitRegister.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<HistoricalHoldingDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<HistoricalHoldingDto>>>> GetInvestorHistoricalHoldings(Guid investorId, [FromQuery] DateOnly date, CancellationToken cancellationToken)
    {
        var holdings = await _unitRegisterService.GetInvestorHistoricalHoldingsAsync(investorId, date, cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<HistoricalHoldingDto>>.Success(holdings, HttpContext.TraceIdentifier));
    }

    [HttpPost("adjustments")]
    [RequirePermission(Permissions.UnitRegister.AdjustmentsCreate)]
    [ProducesResponseType(typeof(ApiResponse<UnitAdjustmentDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<UnitAdjustmentDto>>> CreateAdjustment(
        [FromBody] CreateUnitAdjustmentRequest request,
        [FromHeader(Name = StandardHeaders.IdempotencyKey)] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var adjustment = await _unitRegisterService.CreateAdjustmentAsync(request, idempotencyKey, cancellationToken);
        return CreatedAtAction(nameof(GetInvestorHoldings), new { investorId = adjustment.InvestorId }, ApiResponse<UnitAdjustmentDto>.Success(adjustment, HttpContext.TraceIdentifier));
    }

    [HttpPost("adjustments/{id:guid}/approve")]
    [RequirePermission(Permissions.UnitRegister.AdjustmentsApprove)]
    [ProducesResponseType(typeof(ApiResponse<UnitAdjustmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UnitAdjustmentDto>>> ApproveAdjustment(Guid id, [FromBody] ApproveUnitAdjustmentRequest request, CancellationToken cancellationToken)
    {
        var adjustment = await _unitRegisterService.ApproveAdjustmentAsync(id, request, cancellationToken);
        return Ok(ApiResponse<UnitAdjustmentDto>.Success(adjustment, HttpContext.TraceIdentifier));
    }
}
