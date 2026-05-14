using System.ComponentModel.DataAnnotations;

namespace Cis.Contracts.Portfolio;

#region Instrument Contracts

public sealed record CreateInstrumentRequest(
    [Required, StringLength(20)] string Isin,
    [Required, StringLength(200)] string Name,
    [Required] string InstrumentType,
    [Required, StringLength(3, MinimumLength = 3)] string Currency,
    Guid? CounterpartyId,
    Guid? IssuerId,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal? YieldRate,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal? CouponRate);

public sealed record InstrumentDto(
    Guid Id,
    string Isin,
    string Name,
    string InstrumentType,
    string Currency,
    string Status,
    Guid? CounterpartyId,
    Guid? IssuerId,
    decimal? YieldRate,
    decimal? CouponRate);

#endregion

#region Counterparty Contracts

public sealed record CreateCounterpartyRequest(
    [Required, StringLength(50)] string Code,
    [Required, StringLength(200)] string Name,
    [StringLength(200)] string? Contact);

public sealed record CounterpartyDto(
    Guid Id,
    string Code,
    string Name,
    string? Contact,
    string Status);

#endregion

#region Placement Contracts

public sealed record CreatePlacementRequest(
    Guid SchemeId,
    Guid SchemeClassId,
    Guid InstrumentId,
    Guid? CounterpartyId,
    Guid? IssuerId,
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")] decimal Principal,
    [Required, StringLength(3, MinimumLength = 3)] string Currency,
    DateOnly AcquisitionDate,
    DateOnly MaturityDate,
    [Range(typeof(decimal), "0", "100")] decimal Yield,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal AccruedIncome);

public sealed record PlacementDto(
    Guid Id,
    Guid SchemeId,
    Guid SchemeClassId,
    Guid InstrumentId,
    Guid? CounterpartyId,
    Guid? IssuerId,
    decimal Principal,
    string Currency,
    DateOnly AcquisitionDate,
    DateOnly MaturityDate,
    decimal Yield,
    decimal AccruedIncome,
    string Status,
    string SettlementStatus,
    string? SubmittedByUserId,
    DateTime? SubmittedAtUtc,
    string? ApprovedByUserId,
    DateTime? ApprovedAtUtc,
    IReadOnlyCollection<IncomeScheduleDto> IncomeSchedules);

public sealed record PlacementSubmitRequest();

public sealed record PlacementApproveRequest(
    [StringLength(1000)] string? Comment);

public sealed record PlacementRejectRequest(
    [Required, StringLength(1000)] string Reason);

#endregion

#region Income Schedule Contracts

public sealed record IncomeScheduleDto(
    Guid Id,
    Guid PlacementId,
    string IncomeType,
    DateOnly DueDate,
    decimal Amount,
    bool Received,
    DateTime? ReceivedAtUtc,
    string? ReceivedByUserId);

#endregion

#region Portfolio Holding Contracts

public sealed record PortfolioHoldingDto(
    Guid Id,
    Guid SchemeId,
    Guid SchemeClassId,
    Guid InstrumentId,
    decimal Quantity,
    decimal MarketValue,
    string Currency,
    DateTime LastUpdatedAtUtc);

#endregion

#region Investment Transaction Contracts

public sealed record CreateInvestmentTransactionRequest(
    Guid PlacementId,
    [Required] string TransactionType,
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")] decimal Amount,
    [Required, StringLength(3, MinimumLength = 3)] string Currency,
    DateOnly TransactionDate,
    [Required] string SettlementStatus);

public sealed record InvestmentTransactionDto(
    Guid Id,
    Guid PlacementId,
    string TransactionType,
    decimal Amount,
    string Currency,
    DateOnly TransactionDate,
    string SettlementStatus,
    string Reference,
    string? Notes);

#endregion

#region Maturity Ladder Contracts

public sealed record MaturityLadderEntryDto(
    Guid PlacementId,
    string InstrumentName,
    string InstrumentIsin,
    decimal Principal,
    string Currency,
    DateOnly MaturityDate,
    int DaysToMaturity,
    string Status);

public sealed record MaturityLadderDto(
    Guid SchemeId,
    Guid SchemeClassId,
    IReadOnlyCollection<MaturityLadderEntryDto> Entries,
    DateTime GeneratedAtUtc);

#endregion

#region Income Due Contracts

public sealed record IncomeDueEntryDto(
    Guid PlacementId,
    string InstrumentName,
    string IncomeType,
    DateOnly DueDate,
    decimal Amount,
    string Currency,
    bool Received,
    int DaysOverdue);

public sealed record IncomeDueDto(
    Guid SchemeId,
    Guid SchemeClassId,
    IReadOnlyCollection<IncomeDueEntryDto> Entries,
    decimal TotalDue,
    decimal TotalReceived,
    string Currency);

#endregion

#region Income Receipt Contracts

public sealed record IncomeReceiptRequest(
    Guid IncomeScheduleId,
    [StringLength(200)] string? Reference);

#endregion

#region Rollover Contracts

public sealed record CreateRolloverRequest(
    Guid PlacementId,
    DateOnly NewMaturityDate,
    decimal? NewPrincipal,
    [StringLength(1000)] string? Notes);

public sealed record RolloverEventDto(
    Guid Id,
    Guid OriginalPlacementId,
    Guid NewPlacementId,
    DateOnly OriginalMaturityDate,
    DateOnly NewMaturityDate,
    string RolloverType,
    DateTime RolledAtUtc);

#endregion

#region Mandate Validation Contracts

public sealed record MandateValidationResultDto(
    Guid Id,
    Guid PlacementId,
    string Status,
    string ValidatedField,
    decimal? LimitValue,
    decimal? ActualValue,
    string? ValidationMessage,
    DateTime ValidatedAtUtc);

#endregion

#region Counterparty Exposure Contracts

public sealed record CounterpartyExposureDto(
    Guid Id,
    Guid SchemeId,
    Guid CounterpartyId,
    decimal TotalExposure,
    decimal ExposurePercentage,
    decimal LimitPercentage,
    string Status,
    DateTime LastCalculatedAtUtc);

#endregion
