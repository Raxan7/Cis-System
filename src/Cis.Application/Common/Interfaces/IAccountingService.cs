using Cis.Contracts.Accounting;

namespace Cis.Application.Common.Interfaces;

public interface IAccountingService
{
    Task<ChartOfAccountsDto> CreateChartOfAccountsAsync(CreateChartOfAccountsRequest request, CancellationToken cancellationToken = default);

    Task<JournalDto> CreateManualJournalAsync(CreateManualJournalRequest request, CancellationToken cancellationToken = default);

    Task<JournalDto> CreateAutomatedJournalAsync(CreateAutomatedJournalRequest request, CancellationToken cancellationToken = default);

    Task<JournalDto> SubmitJournalAsync(Guid id, JournalWorkflowActionRequest request, CancellationToken cancellationToken = default);

    Task<JournalDto> ApproveJournalAsync(Guid id, JournalWorkflowActionRequest request, CancellationToken cancellationToken = default);

    Task<GeneralLedgerDto> GetGeneralLedgerAsync(Guid schemeId, Guid? schemeClassId, Guid? accountId, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken = default);

    Task<TrialBalanceDto> GetTrialBalanceAsync(Guid schemeId, Guid? schemeClassId, DateOnly asOfDate, CancellationToken cancellationToken = default);

    Task<JournalRegisterDto> GetJournalRegisterAsync(Guid? schemeId, Guid? schemeClassId, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken = default);

    Task<AccountingPeriodDto> ClosePeriodAsync(Guid id, JournalWorkflowActionRequest request, CancellationToken cancellationToken = default);

    Task<AccountingNavReconciliationDto> GetNavReconciliationAsync(Guid schemeId, Guid? schemeClassId, DateOnly valuationDate, CancellationToken cancellationToken = default);
}
