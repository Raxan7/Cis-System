using System.ComponentModel.DataAnnotations;

namespace Cis.Contracts.NAV;

public sealed record CreateValuationRunRequest(
    Guid SchemeId,
    Guid SchemeClassId,
    DateOnly ValuationDate,
    [Required, StringLength(50)] string FormulaVersion,
    [Required] string DayCountBasis,
    [Range(0, 12)] int ReportPrecision,
    [Range(0, 3660)] int PriceStaleAfterDays,
    [Range(typeof(decimal), "0", "100")] decimal PriceVarianceTolerancePercent,
    bool AllowsAmortizedCost,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal CashAndBank,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal OtherReceivables,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal Prepayments,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal TotalLiabilities,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal AccruedExpenses,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal OpeningUnits,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal UnitsIssued,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal UnitsRedeemed,
    decimal ApprovedUnitAdjustments,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal GrossContribution,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal EntryFee,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal SubscriptionTaxCharges,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal RedemptionUnits,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal RedemptionPrice,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal ExitFee,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal RedemptionTaxCharges,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal UnitsSwitchedOut,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal SourceFundPrice,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal NetSwitchAmount,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal TargetFundPrice,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal AvailableUnits,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal ApplicablePrice,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal EstimatedExitCharges,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal ApplicableTax,
    IReadOnlyCollection<InstrumentValuationInputRequest> Instruments);

public sealed record InstrumentValuationInputRequest(
    Guid InstrumentId,
    [Required, StringLength(100)] string InstrumentType,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal Quantity,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal? MarketPrice,
    DateOnly? PriceDate,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal Principal,
    [Range(typeof(decimal), "0", "100")] decimal AnnualRate,
    [Range(0, 3660)] int AccrualDays,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal? PurchaseCost,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal? MaturityValue,
    [Range(1, 36600)] int? TotalDaysToMaturity,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal? AmortizedCost,
    bool UseAmortizedCost,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal? PriorPrice,
    [Required, StringLength(100)] string PriceSource);

public sealed record NavWorkflowActionRequest(
    [StringLength(1000)] string? Comment);

public sealed record CreateManualValuationOverrideRequest(
    Guid SchemeId,
    Guid SchemeClassId,
    DateOnly ValuationDate,
    Guid InstrumentId,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal OverridePrice,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal OverrideValue,
    [Required, StringLength(1000)] string Reason);

public sealed record ApproveManualValuationOverrideRequest(
    [StringLength(1000)] string? Comment);

public sealed record CreateNavRestatementRequest(
    Guid OriginalPublicationId,
    Guid CorrectedValuationRunId,
    [Required, StringLength(1000)] string Reason);

public sealed record ValuationRunDto(
    Guid Id,
    Guid SchemeId,
    Guid SchemeClassId,
    DateOnly ValuationDate,
    string RunNumber,
    string Status,
    string FormulaVersion,
    string DayCountBasis,
    int InternalPrecision,
    int ReportPrecision,
    int PriceStaleAfterDays,
    decimal PriceVarianceTolerancePercent,
    bool AllowsAmortizedCost,
    string PreparedByUserId,
    DateTime PreparedAtUtc,
    string? SubmittedByUserId,
    DateTime? SubmittedAtUtc,
    string? CheckedByUserId,
    DateTime? CheckedAtUtc,
    string? ApprovedByUserId,
    DateTime? ApprovedAtUtc,
    string? PublishedByUserId,
    DateTime? PublishedAtUtc,
    int? PublishedVersion,
    IReadOnlyCollection<InstrumentValuationDto> Instruments,
    IReadOnlyCollection<StalePriceExceptionDto> StalePriceExceptions,
    IReadOnlyCollection<PricingVarianceExceptionDto> PricingVarianceExceptions,
    NavCalculationDto? Calculation,
    NavPerUnitDto? NavPerUnit,
    IReadOnlyCollection<NavApprovalDto> Approvals);

public sealed record InstrumentValuationDto(
    Guid Id,
    Guid InstrumentId,
    string InstrumentType,
    decimal Quantity,
    decimal? MarketPrice,
    decimal AccruedIncome,
    decimal DailyAccretion,
    decimal? AmortizedCost,
    bool UseAmortizedCost,
    decimal InvestmentValue,
    DateOnly? PriceDate,
    string PriceSource,
    bool IsPriceMissing,
    bool IsPriceStale,
    bool OverrideApplied,
    Guid? ManualValuationOverrideId,
    string FormulaCode);

public sealed record StalePriceExceptionDto(
    Guid Id,
    Guid InstrumentId,
    DateOnly? PriceDate,
    DateOnly ValuationDate,
    int MaxAgeDays,
    bool MissingPrice,
    string Message,
    string Status);

public sealed record PricingVarianceExceptionDto(
    Guid Id,
    Guid InstrumentId,
    decimal CurrentPrice,
    decimal PriorPrice,
    decimal VariancePercent,
    decimal TolerancePercent,
    string Message,
    string Status);

public sealed record NavCalculationDto(
    Guid Id,
    string FormulaVersion,
    decimal InvestmentValue,
    decimal InstrumentAccruedIncome,
    decimal DailyAccretion,
    decimal CashAndBank,
    decimal OtherReceivables,
    decimal Prepayments,
    decimal GrossAssetValue,
    decimal TotalLiabilities,
    decimal AccruedExpenses,
    decimal NetAssetValue,
    decimal NetSubscriptionAmount,
    decimal UnitsAllocated,
    decimal GrossRedemptionValue,
    decimal NetRedemptionPayable,
    decimal SwitchOutValue,
    decimal SwitchInUnits,
    decimal RedeemableAmount,
    string CalculatedByUserId,
    DateTime CalculatedAtUtc);

public sealed record NavPerUnitDto(
    Guid Id,
    Guid SchemeClassId,
    decimal OpeningUnits,
    decimal UnitsIssued,
    decimal UnitsRedeemed,
    decimal ApprovedUnitAdjustments,
    decimal ClosingUnits,
    decimal NavAmount,
    decimal UnitPrice,
    decimal ReportedUnitPrice);

public sealed record NavApprovalDto(
    Guid Id,
    string Step,
    string ActorUserId,
    DateTime OccurredAtUtc,
    string? Comment);

public sealed record ManualValuationOverrideDto(
    Guid Id,
    Guid SchemeId,
    Guid SchemeClassId,
    DateOnly ValuationDate,
    Guid InstrumentId,
    decimal OverridePrice,
    decimal OverrideValue,
    string Reason,
    string Status,
    string RequestedByUserId,
    DateTime RequestedAtUtc,
    string? ApprovedByUserId,
    DateTime? ApprovedAtUtc,
    string? ApprovalComment);

public sealed record NavPublicationDto(
    Guid Id,
    Guid ValuationRunId,
    Guid SchemeId,
    Guid SchemeClassId,
    DateOnly ValuationDate,
    int VersionNumber,
    decimal PublishedNav,
    decimal PublishedUnitPrice,
    decimal ReportedUnitPrice,
    string FormulaVersion,
    string PublishedByUserId,
    DateTime PublishedAtUtc);

public sealed record NavRestatementDto(
    Guid Id,
    Guid OriginalPublicationId,
    Guid CorrectedValuationRunId,
    Guid CorrectedPublicationId,
    Guid SchemeId,
    Guid SchemeClassId,
    DateOnly ValuationDate,
    int CorrectedVersionNumber,
    string Reason,
    string Status);

public sealed record NavReconstructionDto(
    Guid SchemeId,
    DateOnly ValuationDate,
    IReadOnlyCollection<NavPublicationDto> Publications,
    DateTime ReconstructedAtUtc);
