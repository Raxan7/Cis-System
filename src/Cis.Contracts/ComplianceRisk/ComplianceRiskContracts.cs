using System.ComponentModel.DataAnnotations;

namespace Cis.Contracts.ComplianceRisk;

public sealed record NavSourceRequest(Guid SchemeId, Guid SchemeClassId, decimal ClosingNav);

public sealed record LimitRuleRequest(
    [Required] string Scope,
    [Required] string LimitType,
    [Required, StringLength(200)] string Reference,
    decimal LimitValue,
    decimal ActualValue,
    [Required, StringLength(1000)] string Description);

public sealed record RelatedPartyExposureRequest(
    [Required, StringLength(200)] string RelatedPartyName,
    decimal ExposureAmount,
    [Required, StringLength(200)] string SourceReference);

public sealed record CounterpartyUsageRequest(
    [Required, StringLength(200)] string CounterpartyName,
    decimal ExposureAmount,
    decimal LimitAmount,
    [Required, StringLength(200)] string SourceReference);

public sealed record CreateLimitCheckRunRequest(
    Guid? SchemeId,
    Guid? SchemeClassId,
    DateOnly BusinessDate,
    [Required, StringLength(50)] string FormulaVersion,
    IReadOnlyCollection<NavSourceRequest> ActiveClassNavs,
    decimal TotalSubscriptions,
    decimal TransferIn,
    decimal TotalRedemptions,
    decimal TransferOut,
    IReadOnlyCollection<decimal> DailyNavs,
    decimal TotalExpensesForPeriod,
    decimal PeriodNetInvestmentIncome,
    decimal AnnualizationFactor,
    IReadOnlyCollection<LimitRuleRequest> LimitRules,
    IReadOnlyCollection<RelatedPartyExposureRequest> RelatedPartyExposures,
    IReadOnlyCollection<CounterpartyUsageRequest> CounterpartyUsages);

public sealed record AssignBreachRequest(
    [Required, StringLength(200)] string OwnerUserId,
    [Required, StringLength(1000)] string Comment);

public sealed record RemediateBreachRequest(
    [Required, StringLength(1000)] string RemediationPlan,
    [Required, StringLength(500)] string EvidenceReference);

public sealed record CloseBreachRequest(
    [Required, StringLength(500)] string EvidenceReference);

public sealed record CreateLiquidityCoverageRunRequest(
    Guid? SchemeId,
    Guid? SchemeClassId,
    DateOnly BusinessDate,
    [Required, StringLength(50)] string FormulaVersion,
    decimal AvailableLiquidAssets,
    decimal ProjectedShortTermRedemptions,
    [StringLength(12000)] string? SourceDataJson);

public sealed record CreateStressScenarioRequest(
    Guid? SchemeId,
    [Required, StringLength(200)] string Name,
    decimal StressRedemptionAmount,
    [Required, StringLength(12000)] string AssumptionsJson);

public sealed record CreateStressTestRunRequest(
    Guid ScenarioId,
    DateOnly BusinessDate,
    [Required, StringLength(50)] string FormulaVersion,
    decimal AvailableLiquidAssets,
    [StringLength(12000)] string? SourceDataJson);

public sealed record WeightedMaturityInputRequest(decimal InstrumentValue, int DaysToMaturityOrReset);

public sealed record CreateLiquidationTimeAnalysisRequest(
    Guid? SchemeId,
    Guid? SchemeClassId,
    DateOnly BusinessDate,
    [Required, StringLength(50)] string FormulaVersion,
    IReadOnlyCollection<WeightedMaturityInputRequest> Instruments,
    [Required, StringLength(12000)] string AssumptionsJson);

public sealed record LimitCheckRunDto(
    Guid Id,
    Guid? SchemeId,
    Guid? SchemeClassId,
    DateOnly BusinessDate,
    string RunNumber,
    string FormulaVersion,
    decimal AssetsUnderManagement,
    decimal NetFlow,
    decimal AverageNav,
    decimal ExpenseToAumRatio,
    decimal AnnualizedYield,
    string Status,
    string RunByUserId,
    DateTime RunAtUtc,
    IReadOnlyCollection<LimitBreachDto> Breaches,
    IReadOnlyCollection<RelatedPartyExposureDto> RelatedPartyExposures,
    IReadOnlyCollection<CounterpartyLimitUsageDto> CounterpartyUsages);

public sealed record LimitBreachDto(
    Guid Id,
    Guid LimitCheckRunId,
    string RuleScope,
    string LimitType,
    string Reference,
    decimal LimitValue,
    decimal ActualValue,
    string Severity,
    string Description,
    string Status,
    string? OwnerUserId,
    string? RemediationPlan,
    string? ClosureEvidenceReference,
    string CreatedByUserId,
    DateTime CreatedAtUtc,
    string? RemediatedByUserId,
    DateTime? RemediatedAtUtc,
    string? ClosedByUserId,
    DateTime? ClosedAtUtc);

public sealed record RelatedPartyExposureDto(Guid Id, Guid LimitCheckRunId, string RelatedPartyName, decimal ExposureAmount, decimal ExposurePercentOfAum, string SourceReference);

public sealed record CounterpartyLimitUsageDto(Guid Id, Guid LimitCheckRunId, string CounterpartyName, decimal ExposureAmount, decimal LimitAmount, decimal UsagePercent, string SourceReference);

public sealed record LiquidityCoverageRunDto(
    Guid Id,
    Guid? SchemeId,
    Guid? SchemeClassId,
    DateOnly BusinessDate,
    string FormulaVersion,
    decimal AvailableLiquidAssets,
    decimal ProjectedShortTermRedemptions,
    decimal LiquidityCoverageRatio,
    string RunByUserId,
    DateTime RunAtUtc);

public sealed record RedemptionStressScenarioDto(Guid Id, Guid? SchemeId, string Name, decimal StressRedemptionAmount, string AssumptionsJson, string Status);

public sealed record RedemptionStressTestRunDto(
    Guid Id,
    Guid ScenarioId,
    Guid? SchemeId,
    DateOnly BusinessDate,
    string FormulaVersion,
    decimal AvailableLiquidAssets,
    decimal StressRedemptionAmount,
    decimal StressCoverage,
    string RunByUserId,
    DateTime RunAtUtc);

public sealed record LiquidationTimeAnalysisRunDto(
    Guid Id,
    Guid? SchemeId,
    Guid? SchemeClassId,
    DateOnly BusinessDate,
    string FormulaVersion,
    decimal WeightedAverageMaturityDays,
    string AssumptionsJson,
    string RunByUserId,
    DateTime RunAtUtc);

public sealed record RiskDashboardSnapshotDto(
    Guid Id,
    DateOnly BusinessDate,
    decimal AssetsUnderManagement,
    decimal NetFlow,
    decimal AverageNav,
    decimal ExpenseToAumRatio,
    decimal LatestLiquidityCoverageRatio,
    decimal LatestStressCoverage,
    int OpenBreaches,
    int CriticalBreaches,
    decimal RelatedPartyExposurePercent,
    DateTime GeneratedAtUtc);
