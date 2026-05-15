using Cis.Api.Common;
using Cis.Api.Security;
using Cis.Application.Common.Interfaces;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.Cash;
using Microsoft.AspNetCore.Mvc;

namespace Cis.Api.Cash;

[ApiController]
[Route("api/cash")]
public sealed class CashController : ControllerBase
{
    private readonly ICashService _cashService;

    public CashController(ICashService cashService)
    {
        _cashService = cashService;
    }

    [HttpPost("bank-statements/import")]
    [RequirePermission(Permissions.Cash.BankStatementsImport)]
    [ProducesResponseType(typeof(ApiResponse<BankStatementImportDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<BankStatementImportDto>>> ImportBankStatement(
        [FromBody] ImportBankStatementRequest request,
        [FromHeader(Name = StandardHeaders.IdempotencyKey)] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var import = await _cashService.ImportBankStatementAsync(request, idempotencyKey, cancellationToken);
        return CreatedAtAction(nameof(GetBankStatementImport), new { id = import.Id }, ApiResponse<BankStatementImportDto>.Success(import, HttpContext.TraceIdentifier));
    }

    [HttpGet("bank-statements/{id:guid}")]
    [RequirePermission(Permissions.Cash.Read)]
    [ProducesResponseType(typeof(ApiResponse<BankStatementImportDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<BankStatementImportDto>>> GetBankStatementImport(Guid id, CancellationToken cancellationToken)
    {
        var import = await _cashService.GetBankStatementImportAsync(id, cancellationToken);
        return Ok(ApiResponse<BankStatementImportDto>.Success(import, HttpContext.TraceIdentifier));
    }

    [HttpPost("reconciliation-runs")]
    [RequirePermission(Permissions.Cash.ReconciliationRunsCreate)]
    [ProducesResponseType(typeof(ApiResponse<ReconciliationRunDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<ReconciliationRunDto>>> CreateReconciliationRun(
        [FromBody] ReconciliationRunRequest request,
        CancellationToken cancellationToken)
    {
        var run = await _cashService.CreateReconciliationRunAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetReconciliationRun), new { id = run.Id }, ApiResponse<ReconciliationRunDto>.Success(run, HttpContext.TraceIdentifier));
    }

    [HttpGet("reconciliation-runs/{id:guid}")]
    [RequirePermission(Permissions.Cash.Read)]
    [ProducesResponseType(typeof(ApiResponse<ReconciliationRunDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ReconciliationRunDto>>> GetReconciliationRun(Guid id, CancellationToken cancellationToken)
    {
        var run = await _cashService.GetReconciliationRunAsync(id, cancellationToken);
        return Ok(ApiResponse<ReconciliationRunDto>.Success(run, HttpContext.TraceIdentifier));
    }

    [HttpGet("suspense")]
    [RequirePermission(Permissions.Cash.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<SuspenseItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<SuspenseItemDto>>>> GetSuspense([FromQuery] PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var suspense = await _cashService.GetSuspenseAsync(cancellationToken);
        return this.OkPaged(suspense, pagination);
    }

    [HttpPost("suspense/{id:guid}/resolve")]
    [RequirePermission(Permissions.Cash.SuspenseResolve)]
    [ProducesResponseType(typeof(ApiResponse<SuspenseItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SuspenseItemDto>>> ResolveSuspense(Guid id, [FromBody] ResolveSuspenseRequest request, CancellationToken cancellationToken)
    {
        var suspense = await _cashService.ResolveSuspenseAsync(id, request, cancellationToken);
        return Ok(ApiResponse<SuspenseItemDto>.Success(suspense, HttpContext.TraceIdentifier));
    }

    [HttpPost("payments")]
    [RequirePermission(Permissions.Cash.PaymentsCreate)]
    [ProducesResponseType(typeof(ApiResponse<PaymentInstructionDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<PaymentInstructionDto>>> CreatePaymentInstruction([FromBody] CreatePaymentInstructionRequest request, CancellationToken cancellationToken)
    {
        var payment = await _cashService.CreatePaymentInstructionAsync(request, cancellationToken);
        return Created($"/api/cash/payments/{payment.Id}", ApiResponse<PaymentInstructionDto>.Success(payment, HttpContext.TraceIdentifier));
    }

    [HttpPost("payments/{id:guid}/status")]
    [RequirePermission(Permissions.Cash.PaymentsStatus)]
    [ProducesResponseType(typeof(ApiResponse<PaymentInstructionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaymentInstructionDto>>> UpdatePaymentStatus(Guid id, [FromBody] UpdatePaymentStatusRequest request, CancellationToken cancellationToken)
    {
        var payment = await _cashService.UpdatePaymentStatusAsync(id, request, cancellationToken);
        return Ok(ApiResponse<PaymentInstructionDto>.Success(payment, HttpContext.TraceIdentifier));
    }

    [HttpPost("reversals")]
    [RequirePermission(Permissions.Cash.ReversalsCreate)]
    [ProducesResponseType(typeof(ApiResponse<ReversalRequestDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<ReversalRequestDto>>> CreateReversal([FromBody] CreateReversalRequest request, CancellationToken cancellationToken)
    {
        var reversal = await _cashService.CreateReversalAsync(request, cancellationToken);
        return Created($"/api/cash/reversals/{reversal.Id}", ApiResponse<ReversalRequestDto>.Success(reversal, HttpContext.TraceIdentifier));
    }

    [HttpPost("reversals/{id:guid}/approve")]
    [RequirePermission(Permissions.Cash.ReversalsApprove)]
    [ProducesResponseType(typeof(ApiResponse<ReversalRequestDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ReversalRequestDto>>> ApproveReversal(Guid id, [FromBody] ApproveReversalRequest request, CancellationToken cancellationToken)
    {
        var reversal = await _cashService.ApproveReversalAsync(id, request, cancellationToken);
        return Ok(ApiResponse<ReversalRequestDto>.Success(reversal, HttpContext.TraceIdentifier));
    }
}
