using System.ComponentModel.DataAnnotations;

namespace Cis.Contracts.FeesTaxDistribution;

public sealed record CreateFeeAccrualRunRequest(
    Guid SchemeId,
    Guid SchemeClassId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    [Required, StringLength(50)] string FormulaVersion,
    [Required] string DayCountBasis,
    [Range(1, 366)] int PeriodDays,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal AverageNav,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal ApplicableFeeBase,
    [Range(typeof(decimal), "0", "100")] decimal AnnualManagementFeeRate,
    [Range(typeof(decimal), "0", "100")] decimal AnnualCustodyFeeRate,
    [Range(typeof(decimal), "0", "100")] decimal AnnualTrusteeFeeRate,
    [Range(typeof(decimal), "0", "100")] decimal AdminFeeRate,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal? FixedAdminFee,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal GrossContribution,
    [Range(typeof(decimal), "0", "100")] decimal EntryFeeRate,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal? FixedEntryFee,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal GrossRedemptionValue,
    [Range(typeof(decimal), "0", "100")] decimal ExitFeeRate,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal? FixedExitFee,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal SwitchOutValue,
    [Range(typeof(decimal), "0", "100")] decimal SwitchFeeRate,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal? FixedSwitchFee,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal PerformanceBase,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal HurdleBase,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal HighWaterMarkBase,
    [Range(typeof(decimal), "0", "100")] decimal PerformanceFeeRate,
    [Required, StringLength(100)] string Jurisdiction,
    [Required, StringLength(100)] string TaxCategory);

public sealed record CreateFeeWaiverRequest(
    Guid SchemeId,
    Guid SchemeClassId,
    Guid? InvestorId,
    [Required, StringLength(100)] string FeeType,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    [Range(typeof(decimal), "0", "100")] decimal? WaiverRate,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal? WaiverAmount,
    [Required, StringLength(1000)] string Reason);

public sealed record ApproveFeeWaiverRequest(
    [StringLength(1000)] string? Comment);

public sealed record CreateTaxRuleRequest(
    [Required, StringLength(100)] string Jurisdiction,
    [Required, StringLength(100)] string Category,
    [Required, StringLength(100)] string TaxType,
    [Range(typeof(decimal), "0", "100")] decimal Rate,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo);

public sealed record CreateDistributionDeclarationRequest(
    Guid SchemeId,
    Guid SchemeClassId,
    DateOnly DeclarationDate,
    DateOnly RecordDate,
    DateOnly PaymentDate,
    [Required, StringLength(50)] string FormulaVersion,
    decimal InvestmentIncome,
    decimal OtherIncome,
    decimal RealizedGainsLosses,
    decimal PriorPeriodAdjustments,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal FundExpenses,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal Fees,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal Taxes,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal ReserveTransfers,
    [Range(typeof(decimal), "0.000000000001", "79228162514264337593543950335")] decimal EligibleUnits,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal AvailableCash,
    bool CoverageOverrideRequested,
    [StringLength(1000)] string? CoverageOverrideReason);

public sealed record ApproveDistributionDeclarationRequest(
    [StringLength(1000)] string? Comment,
    bool ApproveCoverageOverride);

public sealed record CreateInvestorDistributionRequest(
    Guid InvestorId,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal EligibleUnits,
    [Required] string DistributionMethod,
    [Range(typeof(decimal), "0.000000000001", "79228162514264337593543950335")] decimal OpeningValue,
    decimal ClosingValue,
    decimal CashDistributions,
    decimal NetContributions,
    [StringLength(100)] string? Jurisdiction,
    [StringLength(100)] string? TaxCategory);

public sealed record CreateDistributionRunRequest(
    Guid DeclarationId,
    [Range(typeof(decimal), "0.000000000001", "79228162514264337593543950335")] decimal ReinvestmentPrice,
    IReadOnlyCollection<CreateInvestorDistributionRequest> Investors);

public sealed record PublishDistributionRunRequest(
    [StringLength(1000)] string? Comment);

public sealed record FeeAccrualRunDto(
    Guid Id,
    Guid SchemeId,
    Guid SchemeClassId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string RunNumber,
    string FormulaVersion,
    string DayCountBasis,
    string Status,
    string OwnerUserId,
    DateTime CalculatedAtUtc,
    string? ApprovedByUserId,
    DateTime? ApprovedAtUtc,
    decimal TotalFeeAmount,
    decimal TotalWaiverAmount,
    decimal TotalVatAmount,
    decimal TotalWithholdingTaxAmount,
    decimal NetPayableAmount,
    decimal TotalExpenseRatio,
    IReadOnlyCollection<FeeCalculationDto> Calculations,
    IReadOnlyCollection<VatCalculationDto> VatCalculations,
    IReadOnlyCollection<WithholdingTaxCalculationDto> WithholdingTaxCalculations);

public sealed record FeeCalculationDto(
    Guid Id,
    string FeeType,
    string FormulaCode,
    string FormulaVersion,
    decimal ApplicableFeeBase,
    decimal? AnnualRate,
    int? PeriodDays,
    decimal? FixedAmount,
    decimal GrossAmount,
    decimal WaiverAmount,
    decimal NetAmount,
    string OwnerUserId,
    string? ApprovedByUserId);

public sealed record VatCalculationDto(
    Guid Id,
    decimal TaxableFeeAmount,
    decimal VatRate,
    decimal VatAmount,
    Guid? TaxRuleId,
    string OwnerUserId,
    string? ApproverUserId);

public sealed record WithholdingTaxCalculationDto(
    Guid Id,
    decimal TaxablePaymentBase,
    decimal WithholdingTaxRate,
    decimal WithholdingTaxAmount,
    Guid? TaxRuleId,
    string OwnerUserId,
    string? ApproverUserId);

public sealed record FeeWaiverRequestDto(
    Guid Id,
    Guid SchemeId,
    Guid SchemeClassId,
    Guid? InvestorId,
    string FeeType,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    decimal? WaiverRate,
    decimal? WaiverAmount,
    string Reason,
    string Status,
    string RequestedByUserId,
    DateTime RequestedAtUtc,
    string? ApprovedByUserId,
    DateTime? ApprovedAtUtc,
    string? DecisionComment);

public sealed record TaxRuleDto(
    Guid Id,
    string Jurisdiction,
    string Category,
    string TaxType,
    decimal Rate,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string Status);

public sealed record DistributionDeclarationDto(
    Guid Id,
    Guid SchemeId,
    Guid SchemeClassId,
    DateOnly DeclarationDate,
    DateOnly RecordDate,
    DateOnly PaymentDate,
    string FormulaVersion,
    decimal GrossDistributableIncome,
    decimal NetDistributableIncome,
    decimal EligibleUnits,
    decimal DistributionPerUnit,
    decimal AvailableCash,
    decimal PlannedDistributionAmount,
    decimal DistributionCoverage,
    bool CoverageOverrideRequested,
    bool CoverageOverrideApproved,
    string Status,
    string CreatedByUserId,
    DateTime CreatedAtUtc,
    string? ApprovedByUserId,
    DateTime? ApprovedAtUtc);

public sealed record DistributionRunDto(
    Guid Id,
    Guid DeclarationId,
    Guid SchemeId,
    Guid SchemeClassId,
    DateOnly RunDate,
    string RunNumber,
    string Status,
    string CreatedByUserId,
    DateTime CreatedAtUtc,
    string? PublishedByUserId,
    DateTime? PublishedAtUtc,
    decimal TotalGrossDistribution,
    decimal TotalTaxAmount,
    decimal TotalNetDistribution,
    decimal TotalReinvestedUnits,
    IReadOnlyCollection<InvestorDistributionDto> InvestorDistributions,
    IReadOnlyCollection<ReinvestmentUnitAllocationDto> ReinvestmentAllocations);

public sealed record InvestorDistributionDto(
    Guid Id,
    Guid DistributionRunId,
    Guid InvestorId,
    decimal EligibleUnits,
    decimal DistributionPerUnit,
    decimal GrossDistribution,
    decimal InvestorTaxAmount,
    decimal NetDistribution,
    string Method,
    decimal InvestorReturnForPeriod,
    Guid? TaxRuleId);

public sealed record ReinvestmentUnitAllocationDto(
    Guid Id,
    Guid DistributionRunId,
    Guid InvestorId,
    Guid InvestorDistributionId,
    Guid ReinvestmentInstructionId,
    Guid? UnitLedgerEntryId,
    decimal NetDistribution,
    decimal ReinvestmentPrice,
    decimal UnitsAllocated,
    string SourceReference,
    DateTime? AllocatedAtUtc);
