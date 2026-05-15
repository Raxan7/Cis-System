using Cis.Api.Security;
using Cis.Api.Common;
using Cis.Application.Common.Interfaces;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.Portfolio;
using Microsoft.AspNetCore.Mvc;

namespace Cis.Api.Portfolio;

[ApiController]
[Route("api/portfolio")]
public sealed class PortfolioController : ControllerBase
{
    private readonly IPortfolioService _portfolioService;

    public PortfolioController(IPortfolioService portfolioService)
    {
        _portfolioService = portfolioService;
    }

    [HttpPost("instruments")]
    [RequirePermission(Permissions.Portfolio.InstrumentsCreate)]
    [ProducesResponseType(typeof(ApiResponse<InstrumentDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<InstrumentDto>>> CreateInstrument(
        [FromBody] CreateInstrumentRequest request,
        CancellationToken cancellationToken)
    {
        var instrument = await _portfolioService.CreateInstrumentAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetInstrument), new { id = instrument.Id }, ApiResponse<InstrumentDto>.Success(instrument, HttpContext.TraceIdentifier));
    }

    [HttpGet("instruments/{id:guid}")]
    [RequirePermission(Permissions.Portfolio.Read)]
    [ProducesResponseType(typeof(ApiResponse<InstrumentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<InstrumentDto>>> GetInstrument(Guid id, CancellationToken cancellationToken)
    {
        var instrument = await _portfolioService.GetInstrumentAsync(id, cancellationToken);
        return Ok(ApiResponse<InstrumentDto>.Success(instrument, HttpContext.TraceIdentifier));
    }

    [HttpPost("counterparties")]
    [RequirePermission(Permissions.Portfolio.CounterpartiesCreate)]
    [ProducesResponseType(typeof(ApiResponse<CounterpartyDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<CounterpartyDto>>> CreateCounterparty(
        [FromBody] CreateCounterpartyRequest request,
        CancellationToken cancellationToken)
    {
        var counterparty = await _portfolioService.CreateCounterpartyAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetCounterparty), new { id = counterparty.Id }, ApiResponse<CounterpartyDto>.Success(counterparty, HttpContext.TraceIdentifier));
    }

    [HttpGet("counterparties/{id:guid}")]
    [RequirePermission(Permissions.Portfolio.Read)]
    [ProducesResponseType(typeof(ApiResponse<CounterpartyDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CounterpartyDto>>> GetCounterparty(Guid id, CancellationToken cancellationToken)
    {
        var counterparty = await _portfolioService.GetCounterpartyAsync(id, cancellationToken);
        return Ok(ApiResponse<CounterpartyDto>.Success(counterparty, HttpContext.TraceIdentifier));
    }

    [HttpPost("placements")]
    [RequirePermission(Permissions.Portfolio.PlacementsCreate)]
    [ProducesResponseType(typeof(ApiResponse<PlacementDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<PlacementDto>>> CreatePlacement(
        [FromBody] CreatePlacementRequest request,
        CancellationToken cancellationToken)
    {
        var placement = await _portfolioService.CreatePlacementAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetPlacement), new { id = placement.Id }, ApiResponse<PlacementDto>.Success(placement, HttpContext.TraceIdentifier));
    }

    [HttpPost("placements/{id:guid}/submit")]
    [RequirePermission(Permissions.Portfolio.PlacementsSubmit)]
    [ProducesResponseType(typeof(ApiResponse<PlacementDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PlacementDto>>> SubmitPlacement(Guid id, [FromBody] PlacementSubmitRequest request, CancellationToken cancellationToken)
    {
        var placement = await _portfolioService.SubmitPlacementAsync(id, request, cancellationToken);
        return Ok(ApiResponse<PlacementDto>.Success(placement, HttpContext.TraceIdentifier));
    }

    [HttpPost("placements/{id:guid}/approve")]
    [RequirePermission(Permissions.Portfolio.PlacementsApprove)]
    [ProducesResponseType(typeof(ApiResponse<PlacementDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PlacementDto>>> ApprovePlacement(Guid id, [FromBody] PlacementApproveRequest request, CancellationToken cancellationToken)
    {
        var placement = await _portfolioService.ApprovePlacementAsync(id, request, cancellationToken);
        return Ok(ApiResponse<PlacementDto>.Success(placement, HttpContext.TraceIdentifier));
    }

    [HttpGet("holdings")]
    [RequirePermission(Permissions.Portfolio.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<PortfolioHoldingDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PortfolioHoldingDto>>>> GetHoldings(
        [FromQuery] Guid schemeId,
        [FromQuery] Guid schemeClassId,
        [FromQuery] PaginationRequest pagination,
        CancellationToken cancellationToken)
    {
        var holdings = await _portfolioService.GetHoldingsAsync(schemeId, schemeClassId, cancellationToken);
        return this.OkPaged(holdings, pagination);
    }

    [HttpGet("maturity-ladder")]
    [RequirePermission(Permissions.Portfolio.Read)]
    [ProducesResponseType(typeof(ApiResponse<MaturityLadderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<MaturityLadderDto>>> GetMaturityLadder(
        [FromQuery] Guid schemeId,
        [FromQuery] Guid schemeClassId,
        CancellationToken cancellationToken)
    {
        var ladder = await _portfolioService.GetMaturityLadderAsync(schemeId, schemeClassId, cancellationToken);
        return Ok(ApiResponse<MaturityLadderDto>.Success(ladder, HttpContext.TraceIdentifier));
    }

    [HttpGet("income-due")]
    [RequirePermission(Permissions.Portfolio.Read)]
    [ProducesResponseType(typeof(ApiResponse<IncomeDueDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IncomeDueDto>>> GetIncomeDue(
        [FromQuery] Guid schemeId,
        [FromQuery] Guid schemeClassId,
        CancellationToken cancellationToken)
    {
        var incomeDue = await _portfolioService.GetIncomeDueAsync(schemeId, schemeClassId, cancellationToken);
        return Ok(ApiResponse<IncomeDueDto>.Success(incomeDue, HttpContext.TraceIdentifier));
    }

    [HttpPost("income-receipts")]
    [RequirePermission(Permissions.Portfolio.IncomeReceiptsCreate)]
    [ProducesResponseType(typeof(ApiResponse<IncomeScheduleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IncomeScheduleDto>>> RecordIncomeReceipt(
        [FromBody] IncomeReceiptRequest request,
        CancellationToken cancellationToken)
    {
        var incomeSchedule = await _portfolioService.RecordIncomeReceiptAsync(request, cancellationToken);
        return Ok(ApiResponse<IncomeScheduleDto>.Success(incomeSchedule, HttpContext.TraceIdentifier));
    }

    [HttpPost("rollovers")]
    [RequirePermission(Permissions.Portfolio.RolloversCreate)]
    [ProducesResponseType(typeof(ApiResponse<RolloverEventDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<RolloverEventDto>>> CreateRollover(
        [FromBody] CreateRolloverRequest request,
        CancellationToken cancellationToken)
    {
        var rollover = await _portfolioService.CreateRolloverAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetPlacement), new { id = rollover.NewPlacementId }, ApiResponse<RolloverEventDto>.Success(rollover, HttpContext.TraceIdentifier));
    }

    [HttpGet("placements/{id:guid}")]
    [RequirePermission(Permissions.Portfolio.Read)]
    [ProducesResponseType(typeof(ApiResponse<PlacementDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PlacementDto>>> GetPlacement(Guid id, CancellationToken cancellationToken)
    {
        var placement = await _portfolioService.GetPlacementAsync(id, cancellationToken);
        return Ok(ApiResponse<PlacementDto>.Success(placement, HttpContext.TraceIdentifier));
    }
}
