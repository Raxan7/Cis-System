using System.ComponentModel.DataAnnotations;

namespace Cis.Contracts.Cash;

public sealed record ImportBankStatementRequest(
    Guid SchemeBankAccountId,
    DateOnly StatementDate,
    [Range(0, 365)] int DateToleranceDays,
    [Required, StringLength(200)] string FileName,
    [Required] string CsvContent);

public sealed record BankStatementLineDto(
    Guid Id,
    int LineNumber,
    DateOnly TransactionDate,
    string Reference,
    string? Description,
    decimal Amount,
    string Direction,
    Guid? InvestorId,
    Guid? SchemeId,
    Guid? SchemeClassId,
    string MatchStatus,
    Guid? CashBookEntryId,
    Guid? SuspenseItemId,
    Guid? RelatedEntityId,
    string? RelatedEntityType,
    string? MatchRule);

public sealed record BankStatementImportDto(
    Guid Id,
    Guid SchemeBankAccountId,
    string FileName,
    DateOnly StatementDate,
    int DateToleranceDays,
    string? IdempotencyKey,
    string Status,
    string ImportedByUserId,
    DateTime ImportedAtUtc,
    int LineCount,
    int MatchedLineCount,
    int SuspenseLineCount,
    IReadOnlyCollection<BankStatementLineDto> Lines);

public sealed record ReconciliationRunRequest(
    Guid SchemeBankAccountId,
    DateOnly RunDate,
    [Range(0, 365)] int AgingThresholdDays);

public sealed record ReconciliationRunDto(
    Guid Id,
    Guid SchemeBankAccountId,
    DateOnly RunDate,
    int AgingThresholdDays,
    string Status,
    string RequestedByUserId,
    DateTime RequestedAtUtc,
    DateTime? CompletedAtUtc,
    string? CompletedByUserId,
    int MatchedCount,
    int SuspenseCount,
    int BreakCount,
    int AgedBreakCount,
    string? Summary);

public sealed record SuspenseItemDto(
    Guid Id,
    Guid BankStatementLineId,
    Guid SchemeBankAccountId,
    decimal Amount,
    string Currency,
    string Reference,
    string Reason,
    string Status,
    string OpenedByUserId,
    DateTime OpenedAtUtc,
    string? ResolvedByUserId,
    DateTime? ResolvedAtUtc,
    string? ResolutionComment,
    Guid? PaymentInstructionId,
    Guid? CashBookEntryId);

public sealed record ResolveSuspenseRequest(
    [StringLength(1000)] string? ResolutionComment,
    Guid? PaymentInstructionId,
    Guid? CashBookEntryId);

public sealed record CreatePaymentInstructionRequest(
    Guid InvestorId,
    Guid SchemeId,
    Guid SchemeClassId,
    Guid SchemeBankAccountId,
    decimal Amount,
    [Required, StringLength(3, MinimumLength = 3)] string Currency,
    [Required, StringLength(100)] string Reference,
    [Required] string PaymentType,
    Guid? RelatedDealingInstructionId);

public sealed record PaymentStatusEventDto(
    Guid Id,
    string Status,
    string EventType,
    DateTime OccurredAtUtc,
    string? Reason,
    string? ExternalReference,
    string ChangedByUserId);

public sealed record PaymentInstructionDto(
    Guid Id,
    Guid InvestorId,
    Guid SchemeId,
    Guid SchemeClassId,
    Guid SchemeBankAccountId,
    decimal Amount,
    string Currency,
    string Reference,
    string PaymentType,
    string Status,
    Guid? RelatedDealingInstructionId,
    string RequestedByUserId,
    DateTime RequestedAtUtc,
    string? IdempotencyKey,
    string? ExternalReference,
    string? FailedReason,
    IReadOnlyCollection<PaymentStatusEventDto> StatusEvents);

public sealed record UpdatePaymentStatusRequest(
    [Required] string Status,
    DateTime OccurredAtUtc,
    [StringLength(1000)] string? Reason,
    [StringLength(100)] string? ExternalReference);

public sealed record CreateReversalRequest(
    Guid PaymentInstructionId,
    [Required, StringLength(1000)] string Reason);

public sealed record ApproveReversalRequest(
    [StringLength(1000)] string? Comment);

public sealed record ReversalRequestDto(
    Guid Id,
    Guid PaymentInstructionId,
    string Reason,
    string Status,
    string RequestedByUserId,
    DateTime RequestedAtUtc,
    string? ApprovedByUserId,
    DateTime? ApprovedAtUtc,
    string? ApprovalComment);

public sealed record ReturnedFundDto(
    Guid Id,
    Guid PaymentInstructionId,
    decimal Amount,
    string Currency,
    string Reason,
    string Status,
    string ReturnedByUserId,
    DateTime ReturnedAtUtc,
    string? ResolutionComment);

public sealed record CashMatchDto(
    Guid Id,
    Guid BankStatementLineId,
    Guid CashBookEntryId,
    string MatchRule,
    Guid? RelatedEntityId,
    string? RelatedEntityType,
    string MatchedByUserId,
    DateTime MatchedAtUtc);