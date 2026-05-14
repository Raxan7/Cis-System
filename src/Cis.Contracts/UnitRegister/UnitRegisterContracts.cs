using System.ComponentModel.DataAnnotations;

namespace Cis.Contracts.UnitRegister;

public sealed record CreateUnitAdjustmentRequest(
    Guid InvestorId,
    Guid SchemeId,
    Guid SchemeClassId,
    decimal Units,
    DateOnly ValuationDate,
    [Required, StringLength(100)] string TransactionReference,
    [Range(4, 6)] int UnitPrecision,
    [Required, StringLength(1000)] string Reason);

public sealed record ApproveUnitAdjustmentRequest(
    [StringLength(1000)] string? Comment);

public sealed record UnitAdjustmentDto(
    Guid Id,
    Guid InvestorId,
    Guid SchemeId,
    Guid SchemeClassId,
    decimal Units,
    DateOnly ValuationDate,
    string TransactionReference,
    int UnitPrecision,
    string Reason,
    string Status,
    string RequestedByUserId,
    DateTime RequestedAtUtc,
    string? ApprovedByUserId,
    DateTime? ApprovedAtUtc);

public sealed record UnitLedgerEntryDto(
    Guid Id,
    string MovementType,
    Guid InvestorId,
    Guid SchemeId,
    Guid SchemeClassId,
    DateOnly ValuationDate,
    decimal Units,
    decimal BalanceUnits,
    decimal LienUnits,
    string SourceType,
    Guid SourceEntityId,
    string TransactionReference,
    int UnitPrecision,
    string PostedByUserId,
    DateTime PostedAtUtc,
    string? Narrative);

public sealed record UnitHoldingDto(
    Guid Id,
    Guid InvestorId,
    Guid SchemeId,
    Guid SchemeClassId,
    decimal Units,
    decimal LienedUnits,
    decimal RedeemableUnits,
    int UnitPrecision,
    DateOnly? LastMovementDate,
    string? LastTransactionReference);

public sealed record InvestorPositionDto(
    Guid Id,
    Guid InvestorId,
    Guid SchemeId,
    Guid SchemeClassId,
    decimal Units,
    decimal LienedUnits,
    decimal RedeemableUnits,
    int UnitPrecision,
    DateOnly? AsOfDate,
    string? LastTransactionReference);

public sealed record UnitRegisterSnapshotDto(
    Guid Id,
    Guid SchemeId,
    Guid SchemeClassId,
    DateOnly SnapshotDate,
    decimal TotalUnits,
    decimal LienedUnits,
    decimal RedeemableUnits,
    int HoldingCount,
    string? LastTransactionReference);

public sealed record SchemeClassUnitRegisterDto(
    Guid SchemeId,
    Guid SchemeClassId,
    decimal TotalUnits,
    decimal LienedUnits,
    decimal RedeemableUnits,
    int HoldingCount,
    IReadOnlyCollection<UnitHoldingDto> Holdings,
    UnitRegisterSnapshotDto? LatestSnapshot);

public sealed record HistoricalHoldingDto(
    Guid InvestorId,
    Guid SchemeId,
    Guid SchemeClassId,
    DateOnly ValuationDate,
    string TransactionReference,
    decimal Units,
    decimal LienedUnits,
    decimal RedeemableUnits,
    int UnitPrecision);
