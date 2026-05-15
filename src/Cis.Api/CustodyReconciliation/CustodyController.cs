using Cis.Api.Common;
using Cis.Api.Security;
using Cis.Application.Common.Interfaces;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.CustodyReconciliation;
using Microsoft.AspNetCore.Mvc;

namespace Cis.Api.CustodyReconciliation;

[ApiController]
[Route("api/custody")]
public sealed class CustodyController : ControllerBase
{
    private readonly ICustodyReconciliationService _custodyReconciliationService;

    public CustodyController(ICustodyReconciliationService custodyReconciliationService)
    {
        _custodyReconciliationService = custodyReconciliationService;
    }

    [HttpPost("custodians")]
    [RequirePermission(Permissions.CustodyReconciliation.CustodiansCreate)]
    [ProducesResponseType(typeof(ApiResponse<CustodianDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<CustodianDto>>> CreateCustodian([FromBody] CreateCustodianRequest request, CancellationToken cancellationToken)
    {
        var custodian = await _custodyReconciliationService.CreateCustodianAsync(request, cancellationToken);
        return Created($"/api/custody/custodians/{custodian.Id}", ApiResponse<CustodianDto>.Success(custodian, HttpContext.TraceIdentifier));
    }

    [HttpPost("statements/holdings/import")]
    [RequirePermission(Permissions.CustodyReconciliation.StatementsImport)]
    [ProducesResponseType(typeof(ApiResponse<CustodianStatementImportDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<CustodianStatementImportDto>>> ImportHoldings([FromHeader(Name = StandardHeaders.IdempotencyKey)] string? idempotencyKey, [FromBody] ImportCustodianHoldingsRequest request, CancellationToken cancellationToken)
    {
        var import = await _custodyReconciliationService.ImportHoldingsAsync(request, idempotencyKey, cancellationToken);
        return Created($"/api/custody/statements/holdings/import/{import.Id}", ApiResponse<CustodianStatementImportDto>.Success(import, HttpContext.TraceIdentifier));
    }

    [HttpPost("statements/cash/import")]
    [RequirePermission(Permissions.CustodyReconciliation.StatementsImport)]
    [ProducesResponseType(typeof(ApiResponse<CustodianStatementImportDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<CustodianStatementImportDto>>> ImportCash([FromHeader(Name = StandardHeaders.IdempotencyKey)] string? idempotencyKey, [FromBody] ImportCustodianCashRequest request, CancellationToken cancellationToken)
    {
        var import = await _custodyReconciliationService.ImportCashAsync(request, idempotencyKey, cancellationToken);
        return Created($"/api/custody/statements/cash/import/{import.Id}", ApiResponse<CustodianStatementImportDto>.Success(import, HttpContext.TraceIdentifier));
    }

    [HttpPost("reconciliation-runs")]
    [RequirePermission(Permissions.CustodyReconciliation.ReconciliationRunsCreate)]
    [ProducesResponseType(typeof(ApiResponse<CustodyReconciliationRunDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<CustodyReconciliationRunDto>>> CreateReconciliationRun([FromBody] CreateCustodyReconciliationRunRequest request, CancellationToken cancellationToken)
    {
        var run = await _custodyReconciliationService.CreateReconciliationRunAsync(request, cancellationToken);
        return Created($"/api/custody/reconciliation-runs/{run.Id}", ApiResponse<CustodyReconciliationRunDto>.Success(run, HttpContext.TraceIdentifier));
    }

    [HttpGet("reconciliation-breaks")]
    [RequirePermission(Permissions.CustodyReconciliation.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<CustodyReconciliationBreakDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<CustodyReconciliationBreakDto>>>> GetBreaks([FromQuery] string? status, [FromQuery] PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var breaks = await _custodyReconciliationService.GetBreaksAsync(status, cancellationToken);
        return this.OkPaged(breaks, pagination);
    }

    [HttpPost("reconciliation-breaks/{id:guid}/assign")]
    [RequirePermission(Permissions.CustodyReconciliation.BreaksManage)]
    [ProducesResponseType(typeof(ApiResponse<CustodyReconciliationBreakDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CustodyReconciliationBreakDto>>> AssignBreak(Guid id, [FromBody] AssignCustodyBreakRequest request, CancellationToken cancellationToken)
    {
        var reconciliationBreak = await _custodyReconciliationService.AssignBreakAsync(id, request, cancellationToken);
        return Ok(ApiResponse<CustodyReconciliationBreakDto>.Success(reconciliationBreak, HttpContext.TraceIdentifier));
    }

    [HttpPost("reconciliation-breaks/{id:guid}/resolve")]
    [RequirePermission(Permissions.CustodyReconciliation.BreaksResolve)]
    [ProducesResponseType(typeof(ApiResponse<CustodyReconciliationBreakDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CustodyReconciliationBreakDto>>> ResolveBreak(Guid id, [FromBody] ResolveCustodyBreakRequest request, CancellationToken cancellationToken)
    {
        var reconciliationBreak = await _custodyReconciliationService.ResolveBreakAsync(id, request, cancellationToken);
        return Ok(ApiResponse<CustodyReconciliationBreakDto>.Success(reconciliationBreak, HttpContext.TraceIdentifier));
    }

    [HttpPost("safekeeping-confirmations")]
    [RequirePermission(Permissions.CustodyReconciliation.SafekeepingConfirmationsCreate)]
    [ProducesResponseType(typeof(ApiResponse<SafekeepingConfirmationDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<SafekeepingConfirmationDto>>> CreateSafekeepingConfirmation([FromBody] CreateSafekeepingConfirmationRequest request, CancellationToken cancellationToken)
    {
        var confirmation = await _custodyReconciliationService.CreateSafekeepingConfirmationAsync(request, cancellationToken);
        return Created($"/api/custody/safekeeping-confirmations/{confirmation.Id}", ApiResponse<SafekeepingConfirmationDto>.Success(confirmation, HttpContext.TraceIdentifier));
    }
}
