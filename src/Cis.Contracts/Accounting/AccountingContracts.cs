using System.ComponentModel.DataAnnotations;

namespace Cis.Contracts.Accounting;

public sealed record CreateChartOfAccountsRequest(
    Guid SchemeId,
    Guid? SchemeClassId,
    [Required, StringLength(30)] string Code,
    [Required, StringLength(200)] string Name,
    [Required, StringLength(3, MinimumLength = 3)] string BaseCurrency,
    [Required, StringLength(100)] string InitialPeriodName,
    DateOnly InitialPeriodStart,
    DateOnly InitialPeriodEnd,
    IReadOnlyCollection<AccountDefinitionRequest> Accounts);

public sealed record AccountDefinitionRequest(
    [Required, StringLength(30)] string Code,
    [Required, StringLength(200)] string Name,
    [Required] string AccountType,
    bool IsControlAccount);

public sealed record CreateManualJournalRequest(
    Guid SchemeId,
    Guid? SchemeClassId,
    DateOnly PostingDate,
    [Required, StringLength(3, MinimumLength = 3)] string Currency,
    [Required, StringLength(500)] string Description,
    bool IsCorrection,
    Guid? CorrectedJournalId,
    IReadOnlyCollection<CreateJournalLineRequest> Lines);

public sealed record CreateAutomatedJournalRequest(
    Guid SchemeId,
    Guid? SchemeClassId,
    DateOnly PostingDate,
    [Required, StringLength(3, MinimumLength = 3)] string Currency,
    [Required] string AutomatedJournalType,
    [Required, StringLength(500)] string Description,
    [Required, StringLength(100)] string OriginatingEventType,
    [Required, StringLength(100)] string OriginatingEventId,
    IReadOnlyCollection<CreateJournalLineRequest> Lines);

public sealed record CreateJournalLineRequest(
    Guid AccountId,
    [Required, StringLength(500)] string Description,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal Debit,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal Credit);

public sealed record JournalWorkflowActionRequest(
    [StringLength(1000)] string? Comment);

public sealed record ChartOfAccountsDto(
    Guid Id,
    Guid SchemeId,
    string Code,
    string Name,
    string BaseCurrency,
    bool IsActive,
    LedgerDto Ledger,
    AccountingPeriodDto InitialPeriod,
    IReadOnlyCollection<AccountDto> Accounts,
    IReadOnlyCollection<JournalTemplateDto> JournalTemplates);

public sealed record AccountDto(
    Guid Id,
    Guid ChartOfAccountsId,
    Guid SchemeId,
    string Code,
    string Name,
    string AccountType,
    string NormalBalance,
    bool IsControlAccount,
    string Status);

public sealed record LedgerDto(
    Guid Id,
    Guid SchemeId,
    Guid? SchemeClassId,
    Guid ChartOfAccountsId,
    string Code,
    string Name,
    string Currency,
    string Status);

public sealed record AccountingPeriodDto(
    Guid Id,
    Guid SchemeId,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    string? ClosedByUserId,
    DateTime? ClosedAtUtc);

public sealed record JournalTemplateDto(
    Guid Id,
    string JournalType,
    string Name,
    string DebitAccountCode,
    string CreditAccountCode,
    bool IsActive);

public sealed record JournalDto(
    Guid Id,
    Guid SchemeId,
    Guid? SchemeClassId,
    Guid LedgerId,
    Guid AccountingPeriodId,
    string JournalNumber,
    string Source,
    string? AutomatedJournalType,
    string Status,
    DateOnly PostingDate,
    string Currency,
    string Description,
    string? OriginatingEventType,
    string? OriginatingEventId,
    bool IsBackdated,
    bool IsCorrection,
    Guid? CorrectedJournalId,
    decimal TotalDebits,
    decimal TotalCredits,
    string CreatedByUserId,
    DateTime CreatedAtUtc,
    string? SubmittedByUserId,
    DateTime? SubmittedAtUtc,
    string? ApprovedByUserId,
    DateTime? ApprovedAtUtc,
    string? PostedByUserId,
    DateTime? PostedAtUtc,
    IReadOnlyCollection<JournalLineDto> Lines);

public sealed record JournalLineDto(
    Guid Id,
    Guid AccountId,
    int LineNumber,
    string Description,
    decimal Debit,
    decimal Credit,
    string Currency);

public sealed record LedgerEntryDto(
    Guid Id,
    Guid JournalId,
    Guid JournalLineId,
    Guid LedgerId,
    Guid AccountId,
    string AccountCode,
    string AccountName,
    string AccountType,
    Guid SchemeId,
    Guid? SchemeClassId,
    DateOnly PostingDate,
    string Description,
    decimal Debit,
    decimal Credit,
    string Currency,
    string? OriginatingEventType,
    string? OriginatingEventId,
    DateTime PostedAtUtc);

public sealed record GeneralLedgerDto(
    Guid SchemeId,
    Guid? SchemeClassId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    decimal TotalDebits,
    decimal TotalCredits,
    IReadOnlyCollection<LedgerEntryDto> Entries);

public sealed record TrialBalanceDto(
    Guid SchemeId,
    Guid? SchemeClassId,
    DateOnly AsOfDate,
    decimal TotalDebits,
    decimal TotalCredits,
    bool IsBalanced,
    IReadOnlyCollection<TrialBalanceLineDto> Lines);

public sealed record TrialBalanceLineDto(
    Guid AccountId,
    string AccountCode,
    string AccountName,
    string AccountType,
    decimal Debit,
    decimal Credit);

public sealed record JournalRegisterDto(
    Guid? SchemeId,
    Guid? SchemeClassId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    IReadOnlyCollection<JournalDto> Journals);

public sealed record AccountingNavReconciliationDto(
    Guid Id,
    Guid SchemeId,
    Guid? SchemeClassId,
    DateOnly ValuationDate,
    decimal AccountingNav,
    decimal? PublishedNav,
    decimal Difference,
    string Status,
    DateTime GeneratedAtUtc);
