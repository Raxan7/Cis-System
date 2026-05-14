using Cis.Api.Security;
using Cis.Application.Common.Interfaces;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.Accounting;
using Microsoft.AspNetCore.Mvc;

namespace Cis.Api.Accounting;

[ApiController]
[Route("api/accounting")]
public sealed class AccountingController : ControllerBase
{
    private readonly IAccountingService _accountingService;

    public AccountingController(IAccountingService accountingService)
    {
        _accountingService = accountingService;
    }

    [HttpPost("chart-of-accounts")]
    [RequirePermission(Permissions.Accounting.ChartOfAccountsCreate)]
    [ProducesResponseType(typeof(ApiResponse<ChartOfAccountsDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<ChartOfAccountsDto>>> CreateChartOfAccounts(
        [FromBody] CreateChartOfAccountsRequest request,
        CancellationToken cancellationToken)
    {
        var chart = await _accountingService.CreateChartOfAccountsAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetTrialBalance), new { schemeId = chart.SchemeId, asOfDate = chart.InitialPeriod.EndDate }, ApiResponse<ChartOfAccountsDto>.Success(chart, HttpContext.TraceIdentifier));
    }

    [HttpPost("journals/manual")]
    [RequirePermission(Permissions.Accounting.JournalsManualCreate)]
    [ProducesResponseType(typeof(ApiResponse<JournalDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<JournalDto>>> CreateManualJournal(
        [FromBody] CreateManualJournalRequest request,
        CancellationToken cancellationToken)
    {
        var journal = await _accountingService.CreateManualJournalAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetJournalRegister), new { schemeId = journal.SchemeId }, ApiResponse<JournalDto>.Success(journal, HttpContext.TraceIdentifier));
    }

    [HttpPost("journals/automated")]
    [RequirePermission(Permissions.Accounting.JournalsAutomatedCreate)]
    [ProducesResponseType(typeof(ApiResponse<JournalDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<JournalDto>>> CreateAutomatedJournal(
        [FromBody] CreateAutomatedJournalRequest request,
        CancellationToken cancellationToken)
    {
        var journal = await _accountingService.CreateAutomatedJournalAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetJournalRegister), new { schemeId = journal.SchemeId }, ApiResponse<JournalDto>.Success(journal, HttpContext.TraceIdentifier));
    }

    [HttpPost("journals/{id:guid}/submit")]
    [RequirePermission(Permissions.Accounting.JournalsSubmit)]
    [ProducesResponseType(typeof(ApiResponse<JournalDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<JournalDto>>> SubmitJournal(
        Guid id,
        [FromBody] JournalWorkflowActionRequest request,
        CancellationToken cancellationToken)
    {
        var journal = await _accountingService.SubmitJournalAsync(id, request, cancellationToken);
        return Ok(ApiResponse<JournalDto>.Success(journal, HttpContext.TraceIdentifier));
    }

    [HttpPost("journals/{id:guid}/approve")]
    [RequirePermission(Permissions.Accounting.JournalsApprove)]
    [ProducesResponseType(typeof(ApiResponse<JournalDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<JournalDto>>> ApproveJournal(
        Guid id,
        [FromBody] JournalWorkflowActionRequest request,
        CancellationToken cancellationToken)
    {
        var journal = await _accountingService.ApproveJournalAsync(id, request, cancellationToken);
        return Ok(ApiResponse<JournalDto>.Success(journal, HttpContext.TraceIdentifier));
    }

    [HttpGet("general-ledger")]
    [RequirePermission(Permissions.Accounting.Read)]
    [ProducesResponseType(typeof(ApiResponse<GeneralLedgerDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<GeneralLedgerDto>>> GetGeneralLedger(
        [FromQuery] Guid schemeId,
        [FromQuery] Guid? schemeClassId,
        [FromQuery] Guid? accountId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        CancellationToken cancellationToken)
    {
        var ledger = await _accountingService.GetGeneralLedgerAsync(schemeId, schemeClassId, accountId, fromDate, toDate, cancellationToken);
        return Ok(ApiResponse<GeneralLedgerDto>.Success(ledger, HttpContext.TraceIdentifier));
    }

    [HttpGet("trial-balance")]
    [RequirePermission(Permissions.Accounting.Read)]
    [ProducesResponseType(typeof(ApiResponse<TrialBalanceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TrialBalanceDto>>> GetTrialBalance(
        [FromQuery] Guid schemeId,
        [FromQuery] Guid? schemeClassId,
        [FromQuery] DateOnly asOfDate,
        CancellationToken cancellationToken)
    {
        var trialBalance = await _accountingService.GetTrialBalanceAsync(schemeId, schemeClassId, asOfDate, cancellationToken);
        return Ok(ApiResponse<TrialBalanceDto>.Success(trialBalance, HttpContext.TraceIdentifier));
    }

    [HttpGet("journal-register")]
    [RequirePermission(Permissions.Accounting.Read)]
    [ProducesResponseType(typeof(ApiResponse<JournalRegisterDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<JournalRegisterDto>>> GetJournalRegister(
        [FromQuery] Guid? schemeId,
        [FromQuery] Guid? schemeClassId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        CancellationToken cancellationToken)
    {
        var register = await _accountingService.GetJournalRegisterAsync(schemeId, schemeClassId, fromDate, toDate, cancellationToken);
        return Ok(ApiResponse<JournalRegisterDto>.Success(register, HttpContext.TraceIdentifier));
    }

    [HttpPost("periods/{id:guid}/close")]
    [RequirePermission(Permissions.Accounting.PeriodsClose)]
    [ProducesResponseType(typeof(ApiResponse<AccountingPeriodDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AccountingPeriodDto>>> ClosePeriod(
        Guid id,
        [FromBody] JournalWorkflowActionRequest request,
        CancellationToken cancellationToken)
    {
        var period = await _accountingService.ClosePeriodAsync(id, request, cancellationToken);
        return Ok(ApiResponse<AccountingPeriodDto>.Success(period, HttpContext.TraceIdentifier));
    }

    [HttpGet("nav-reconciliation")]
    [RequirePermission(Permissions.Accounting.NavReconciliationRead)]
    [ProducesResponseType(typeof(ApiResponse<AccountingNavReconciliationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AccountingNavReconciliationDto>>> GetNavReconciliation(
        [FromQuery] Guid schemeId,
        [FromQuery] Guid? schemeClassId,
        [FromQuery] DateOnly valuationDate,
        CancellationToken cancellationToken)
    {
        var reconciliation = await _accountingService.GetNavReconciliationAsync(schemeId, schemeClassId, valuationDate, cancellationToken);
        return Ok(ApiResponse<AccountingNavReconciliationDto>.Success(reconciliation, HttpContext.TraceIdentifier));
    }
}
