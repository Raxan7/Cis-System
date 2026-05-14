using System.ComponentModel.DataAnnotations;

namespace Cis.Contracts.CustodyReconciliation;

public sealed record CreateCustodianAccountRequest(
    Guid SchemeId,
    Guid? SchemeClassId,
    [Required, StringLength(100)] string AccountNumber,
    [Required, StringLength(200)] string AccountName,
    [Required, StringLength(3)] string Currency);

public sealed record CreateCustodianRequest(
    [Required, StringLength(50)] string Code,
    [Required, StringLength(200)] string Name,
    [StringLength(20)] string? SwiftCode,
    IReadOnlyCollection<CreateCustodianAccountRequest> Accounts);

public sealed record CustodianAccountDto(
    Guid Id,
    Guid CustodianId,
    Guid SchemeId,
    Guid? SchemeClassId,
    string AccountNumber,
    string AccountName,
    string Currency,
    bool IsActive);

public sealed record CustodianDto(
    Guid Id,
    string Code,
    string Name,
    string? SwiftCode,
    string Status,
    string CreatedByUserId,
    DateTime CreatedAtUtc,
    IReadOnlyCollection<CustodianAccountDto> Accounts);

public sealed record ImportHoldingLineRequest(
    Guid? CustodianAccountId,
    Guid SchemeId,
    Guid? SchemeClassId,
    Guid? InstrumentId,
    [Required, StringLength(100)] string InstrumentCode,
    [Required, StringLength(200)] string InstrumentName,
    decimal Quantity,
    decimal MarketValue,
    [Required, StringLength(3)] string Currency,
    [StringLength(100)] string? SettlementReference,
    bool IsSettled);

public sealed record ImportCustodianHoldingsRequest(
    Guid CustodianId,
    Guid? CustodianAccountId,
    DateOnly StatementDate,
    [Required, StringLength(200)] string SourceFileName,
    IReadOnlyCollection<ImportHoldingLineRequest> Lines);

public sealed record ImportCashLineRequest(
    Guid? CustodianAccountId,
    Guid SchemeId,
    [Required, StringLength(100)] string AccountNumber,
    [Required, StringLength(3)] string Currency,
    DateOnly BalanceDate,
    decimal CashBalance,
    [StringLength(100)] string? SettlementReference,
    bool IsSettled);

public sealed record ImportCustodianCashRequest(
    Guid CustodianId,
    Guid? CustodianAccountId,
    DateOnly StatementDate,
    [Required, StringLength(200)] string SourceFileName,
    IReadOnlyCollection<ImportCashLineRequest> Lines);

public sealed record CustodianHoldingLineDto(
    Guid Id,
    Guid CustodianStatementImportId,
    Guid CustodianId,
    Guid? CustodianAccountId,
    Guid SchemeId,
    Guid? SchemeClassId,
    Guid? InstrumentId,
    string InstrumentCode,
    string InstrumentName,
    decimal Quantity,
    decimal MarketValue,
    string Currency,
    string? SettlementReference,
    bool IsSettled);

public sealed record CustodianCashLineDto(
    Guid Id,
    Guid CustodianStatementImportId,
    Guid CustodianId,
    Guid? CustodianAccountId,
    Guid SchemeId,
    string AccountNumber,
    string Currency,
    DateOnly BalanceDate,
    decimal CashBalance,
    string? SettlementReference,
    bool IsSettled);

public sealed record CustodianStatementImportDto(
    Guid Id,
    Guid CustodianId,
    Guid? CustodianAccountId,
    string StatementType,
    DateOnly StatementDate,
    string SourceFileName,
    string IdempotencyKey,
    string SourceHash,
    string Status,
    string ImportedByUserId,
    DateTime ImportedAtUtc,
    int LineCount,
    IReadOnlyCollection<CustodianHoldingLineDto> HoldingLines,
    IReadOnlyCollection<CustodianCashLineDto> CashLines);

public sealed record InternalHoldingSnapshotRequest(
    Guid SchemeId,
    Guid? SchemeClassId,
    [Required, StringLength(100)] string InstrumentCode,
    decimal Quantity,
    decimal MarketValue,
    [Required, StringLength(3)] string Currency);

public sealed record InternalCashSnapshotRequest(
    Guid SchemeId,
    [Required, StringLength(100)] string AccountNumber,
    [Required, StringLength(3)] string Currency,
    decimal CashBalance);

public sealed record CreateCustodyReconciliationRunRequest(
    Guid CustodianId,
    Guid? HoldingsImportId,
    Guid? CashImportId,
    DateOnly BusinessDate,
    IReadOnlyCollection<InternalHoldingSnapshotRequest> InternalHoldings,
    IReadOnlyCollection<InternalCashSnapshotRequest> InternalCashBalances);

public sealed record CustodyReconciliationRunDto(
    Guid Id,
    Guid CustodianId,
    Guid? HoldingsImportId,
    Guid? CashImportId,
    DateOnly BusinessDate,
    string Status,
    string RunByUserId,
    DateTime RunAtUtc,
    int HoldingBreakCount,
    int CashBreakCount,
    IReadOnlyCollection<CustodyReconciliationBreakDto> Breaks);

public sealed record AssignCustodyBreakRequest(
    [Required, StringLength(200)] string OwnerUserId,
    [Required, StringLength(1000)] string Note);

public sealed record ResolveCustodyBreakRequest(
    [Required, StringLength(500)] string EvidenceReference,
    [Required, StringLength(1000)] string Note);

public sealed record CustodyReconciliationBreakDto(
    Guid Id,
    Guid ReconciliationRunId,
    string BreakCategory,
    string BreakType,
    string Severity,
    string Status,
    Guid SchemeId,
    Guid? SchemeClassId,
    string? InstrumentCode,
    string? AccountNumber,
    string? Currency,
    decimal InternalQuantity,
    decimal CustodianQuantity,
    decimal QuantityDifference,
    decimal InternalMarketValue,
    decimal CustodianMarketValue,
    decimal MarketValueDifference,
    decimal InternalCashBalance,
    decimal CustodianCashBalance,
    decimal CashDifference,
    string? OwnerUserId,
    int LatestAgeDays,
    string? LatestActionNote,
    string? ResolutionEvidenceReference,
    DateTime CreatedAtUtc,
    DateTime? ResolvedAtUtc);

public sealed record CreateSafekeepingConfirmationRequest(
    Guid CustodianId,
    Guid? SchemeId,
    Guid? SourceReconciliationRunId,
    DateOnly BusinessDate,
    [StringLength(100)] string? SettlementReference,
    bool SettlementConfirmed);

public sealed record SafekeepingConfirmationDto(
    Guid Id,
    Guid CustodianId,
    Guid? SchemeId,
    Guid? SourceReconciliationRunId,
    DateOnly BusinessDate,
    int HoldingsCount,
    int CashLineCount,
    decimal TotalMarketValue,
    decimal TotalCashBalance,
    string? SettlementReference,
    bool SettlementConfirmed,
    string ConfirmationPayloadJson,
    string Status,
    string GeneratedByUserId,
    DateTime GeneratedAtUtc);
