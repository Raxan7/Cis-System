using System.ComponentModel.DataAnnotations;

namespace Cis.Contracts.Schemes;

public sealed record CreateSchemeRequest(
    [Required, StringLength(30)] string Code,
    [Required, StringLength(200)] string Name,
    [Required, StringLength(100)] string LegalType,
    [Required, StringLength(3, MinimumLength = 3)] string BaseCurrency);

public sealed record UpdateSchemeRequest(
    [Required, StringLength(200)] string Name,
    [Required, StringLength(100)] string LegalType,
    [Required, StringLength(3, MinimumLength = 3)] string BaseCurrency,
    DateOnly? EffectiveDate);

public sealed record SchemeWorkflowActionRequest(
    [StringLength(1000)] string? Comment);

public sealed record AddSchemeClassRequest(
    [Required, StringLength(30)] string Code,
    [Required, StringLength(200)] string Name,
    [Required, StringLength(3, MinimumLength = 3)] string Currency,
    [Required] string ValuationFrequency,
    [Required] string DealingFrequency,
    [Required] TimeOnly CutOffTime,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal MinimumContribution,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal MinimumBalance,
    [Range(0, int.MaxValue)] int LockInDays,
    [Range(0, int.MaxValue)] int NoticePeriodDays);

public sealed record AddFeeRuleRequest(
    decimal? FromAmount,
    decimal? ToAmount,
    decimal? Rate,
    decimal? FixedAmount);

public sealed record AddFeeScheduleRequest(
    Guid? SchemeClassId,
    [Required, StringLength(100)] string FeeType,
    [Required, StringLength(100)] string CalculationBasis,
    decimal? Rate,
    decimal? FixedAmount,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    IReadOnlyCollection<AddFeeRuleRequest>? TierRules);

public sealed record AddEligibilityRuleRequest(
    [Required, StringLength(100)] string RuleType,
    [Required, StringLength(500)] string Description,
    [Required] string RuleExpressionJson);

public sealed record AddApprovedInstrumentRuleRequest(
    [Required, StringLength(100)] string InstrumentType,
    [Range(0, int.MaxValue)] int TenorLimitDays,
    [Range(typeof(decimal), "0", "100")] decimal IssuerLimit,
    [Range(typeof(decimal), "0", "100")] decimal CounterpartyLimit,
    [Range(typeof(decimal), "0", "100")] decimal AssetClassLimit);

public sealed record AddSchemeBankAccountRequest(
    [Required, StringLength(200)] string BankName,
    [Required, StringLength(100)] string AccountNumber,
    [Required, StringLength(200)] string AccountName,
    [Required, StringLength(3, MinimumLength = 3)] string Currency,
    [StringLength(20)] string? SwiftCode);

public sealed record AddSchemeCustodianMappingRequest(
    [Required, StringLength(200)] string CustodianName,
    [Required, StringLength(100)] string CustodyAccountReference,
    [Required, StringLength(100)] string SettlementAccountReference);

public sealed record AddSchemeConfigurationRequest(
    [Required, StringLength(100)] string NavPricingBasis,
    [Required, StringLength(100)] string IncomeRecognitionBasis);

public sealed record AddSchemeRiskProfileRequest(
    [Required, StringLength(100)] string RiskRating,
    [Range(typeof(decimal), "0", "100")] decimal MaxSingleIssuerExposure);

public sealed record AddLiquidityThresholdRequest(
    [Range(typeof(decimal), "0", "100")] decimal MinimumLiquidAssetRatio,
    [Range(typeof(decimal), "0", "100")] decimal WarningThreshold,
    [Range(typeof(decimal), "0", "100")] decimal BreachThreshold);

public sealed record AddDistributionRuleRequest(
    [Required] string DistributionFrequency,
    bool ReinvestmentAllowed,
    [Range(1, 31)] int PaymentDay);

public sealed record AddTemplateMappingRequest(
    [Required, StringLength(100)] string TemplateType,
    [Required, StringLength(100)] string TemplateCode);

public sealed record SchemeDto(
    Guid Id,
    string Code,
    string Name,
    string LegalType,
    string BaseCurrency,
    string Status,
    int VersionNumber,
    DateOnly? PendingEffectiveDate,
    string? SubmittedByUserId,
    string? CheckedByUserId,
    string? ApprovedByUserId,
    IReadOnlyCollection<SchemeClassDto> Classes,
    IReadOnlyCollection<FeeScheduleDto> FeeSchedules,
    IReadOnlyCollection<SchemeEligibilityRuleDto> EligibilityRules,
    IReadOnlyCollection<ApprovedInstrumentRuleDto> ApprovedInstrumentRules,
    IReadOnlyCollection<SchemeBankAccountDto> BankAccounts,
    IReadOnlyCollection<SchemeCustodianMappingDto> CustodianMappings,
    IReadOnlyCollection<SchemeConfigurationDto> Configurations,
    IReadOnlyCollection<SchemeRiskProfileDto> RiskProfiles,
    IReadOnlyCollection<LiquidityThresholdDto> LiquidityThresholds,
    IReadOnlyCollection<DistributionRuleDto> DistributionRules,
    IReadOnlyCollection<TemplateMappingDto> TemplateMappings,
    IReadOnlyCollection<SchemeVersionHistoryDto> VersionHistory);

public sealed record SchemeClassDto(
    Guid Id,
    string Code,
    string Name,
    string Currency,
    string ValuationFrequency,
    string DealingFrequency,
    TimeOnly CutOffTime,
    decimal MinimumContribution,
    decimal MinimumBalance,
    int LockInDays,
    int NoticePeriodDays,
    bool IsActive);

public sealed record FeeRuleDto(
    Guid Id,
    decimal? FromAmount,
    decimal? ToAmount,
    decimal? Rate,
    decimal? FixedAmount);

public sealed record FeeScheduleDto(
    Guid Id,
    Guid? SchemeClassId,
    string FeeType,
    string CalculationBasis,
    decimal? Rate,
    decimal? FixedAmount,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string Status,
    IReadOnlyCollection<FeeRuleDto> Rules);

public sealed record SchemeEligibilityRuleDto(
    Guid Id,
    string RuleType,
    string Description,
    string RuleExpressionJson,
    string Status);

public sealed record ApprovedInstrumentRuleDto(
    Guid Id,
    string InstrumentType,
    int TenorLimitDays,
    decimal IssuerLimit,
    decimal CounterpartyLimit,
    decimal AssetClassLimit,
    string Status);

public sealed record SchemeBankAccountDto(
    Guid Id,
    string BankName,
    string AccountNumber,
    string AccountName,
    string Currency,
    string? SwiftCode,
    bool IsActive);

public sealed record SchemeCustodianMappingDto(
    Guid Id,
    string CustodianName,
    string CustodyAccountReference,
    string SettlementAccountReference,
    bool IsActive);

public sealed record SchemeConfigurationDto(
    Guid Id,
    string NavPricingBasis,
    string IncomeRecognitionBasis);

public sealed record SchemeRiskProfileDto(
    Guid Id,
    string RiskRating,
    decimal MaxSingleIssuerExposure);

public sealed record LiquidityThresholdDto(
    Guid Id,
    decimal MinimumLiquidAssetRatio,
    decimal WarningThreshold,
    decimal BreachThreshold);

public sealed record DistributionRuleDto(
    Guid Id,
    string DistributionFrequency,
    bool ReinvestmentAllowed,
    int PaymentDay,
    bool IsActive);

public sealed record TemplateMappingDto(
    Guid Id,
    string TemplateType,
    string TemplateCode,
    bool IsActive);

public sealed record SchemeVersionHistoryDto(
    Guid Id,
    int VersionNumber,
    string ChangeType,
    string ChangedByUserId,
    DateTime ChangedAtUtc,
    DateOnly? EffectiveDate);
