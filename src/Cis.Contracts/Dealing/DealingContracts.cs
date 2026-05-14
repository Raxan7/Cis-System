using System.ComponentModel.DataAnnotations;

namespace Cis.Contracts.Dealing;

public sealed record CreateSubscriptionRequest(
    Guid InvestorId,
    Guid SchemeId,
    Guid SchemeClassId,
    [Required] string Channel,
    DateOnly BusinessDate,
    DateTime ReceivedAtUtc,
    [Required] string Mode,
    decimal? Amount,
    decimal? Units,
    [Required, StringLength(3, MinimumLength = 3)] string Currency,
    bool FundsCleared,
    bool ApprovedNavAvailable,
    decimal? ApprovedNavPrice,
    DateOnly? ApprovedNavDate);

public sealed record CreateRedemptionRequest(
    Guid InvestorId,
    Guid SchemeId,
    Guid SchemeClassId,
    [Required] string Channel,
    DateOnly BusinessDate,
    DateTime ReceivedAtUtc,
    [Required] string Mode,
    decimal? Amount,
    decimal? Units,
    bool FullRedemption,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal AvailableUnits,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal LockedUnits,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal MinimumBalanceUnits,
    [Range(0, int.MaxValue)] int LockInDays,
    [Range(0, int.MaxValue)] int NoticePeriodDays,
    bool ApprovedNavAvailable,
    decimal ApprovedNavPrice,
    DateOnly ApprovedNavDate,
    [Range(typeof(decimal), "0", "100")] decimal ExitFeeRate,
    [Range(typeof(decimal), "0", "100")] decimal TaxRate,
    [Required, StringLength(3, MinimumLength = 3)] string Currency);

public sealed record CreateSwitchRequest(
    Guid InvestorId,
    Guid SourceSchemeId,
    Guid SourceSchemeClassId,
    Guid TargetSchemeId,
    Guid TargetSchemeClassId,
    [Required] string Channel,
    DateOnly BusinessDate,
    DateTime ReceivedAtUtc,
    [Required] string Mode,
    decimal? Amount,
    decimal? Units,
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal FeeAmount,
    [Required] string OwnershipHistoryJson);

public sealed record CreateTransferRequest(
    Guid FromInvestorId,
    Guid ToInvestorId,
    Guid SchemeId,
    Guid SchemeClassId,
    [Required] string Channel,
    DateOnly BusinessDate,
    DateTime ReceivedAtUtc,
    decimal Units,
    [Required] string OwnershipHistoryJson);

public sealed record CreateLienRequest(
    Guid InvestorId,
    Guid SchemeId,
    Guid SchemeClassId,
    decimal? Units,
    decimal? Amount,
    [Required, StringLength(3, MinimumLength = 3)] string Currency,
    [Required, StringLength(500)] string DocumentationReference,
    [Required, StringLength(500)] string ApproverEvidenceReference);

public sealed record ReleaseLienRequest(
    [Required, StringLength(500)] string ReleaseEvidenceReference);

public sealed record CreateRecurringContributionPlanRequest(
    Guid InvestorId,
    Guid SchemeId,
    Guid SchemeClassId,
    [Required] string Frequency,
    decimal Amount,
    [Required, StringLength(3, MinimumLength = 3)] string Currency,
    [Required] string Channel,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    [Required, StringLength(100)] string CollectionMethod);

public sealed record AmendRecurringContributionPlanRequest(
    [Required] string Frequency,
    decimal Amount,
    [Required] string Channel,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    [Required, StringLength(100)] string CollectionMethod);

public sealed record DealingInstructionActionRequest(
    [StringLength(1000)] string? Comment);

public sealed record DealingInstructionDto(
    Guid Id,
    string InstructionNumber,
    string InstructionType,
    Guid InvestorId,
    Guid SchemeId,
    Guid SchemeClassId,
    string Channel,
    DateOnly BusinessDate,
    DateTime ReceivedAtUtc,
    string Status,
    string? SubmittedByUserId,
    string? ApprovedByUserId,
    IReadOnlyCollection<SubscriptionInstructionDto> Subscriptions,
    IReadOnlyCollection<RedemptionInstructionDto> Redemptions,
    IReadOnlyCollection<SwitchInstructionDto> Switches,
    IReadOnlyCollection<TransferInstructionDto> Transfers,
    IReadOnlyCollection<DealingValidationResultDto> ValidationResults,
    IReadOnlyCollection<CutOffBreachDto> CutOffBreaches,
    IReadOnlyCollection<InstructionStatusHistoryDto> StatusHistory);

public sealed record SubscriptionInstructionDto(
    Guid Id,
    string Mode,
    decimal? Amount,
    decimal? Units,
    string Currency,
    bool FundsCleared,
    bool ApprovedNavAvailable,
    decimal? ApprovedNavPrice,
    DateOnly? ApprovedNavDate,
    decimal? AllocatedUnits,
    string? ConfirmationNumber);

public sealed record RedemptionInstructionDto(
    Guid Id,
    string Mode,
    decimal? Amount,
    decimal? Units,
    bool FullRedemption,
    decimal AvailableUnits,
    decimal LienUnits,
    decimal LockedUnits,
    decimal RequestedUnits,
    decimal GrossAmount,
    decimal ExitFeeAmount,
    decimal TaxAmount,
    decimal NetPayoutAmount,
    bool RequiresApprovalThreshold,
    bool PayoutAuthorized,
    string? RedemptionAdviceNumber);

public sealed record SwitchInstructionDto(
    Guid Id,
    Guid TargetSchemeId,
    Guid TargetSchemeClassId,
    string Mode,
    decimal? Amount,
    decimal? Units,
    decimal FeeAmount,
    string OwnershipHistoryJson);

public sealed record TransferInstructionDto(
    Guid Id,
    Guid ToInvestorId,
    decimal Units,
    string OwnershipHistoryJson);

public sealed record DealingValidationResultDto(Guid Id, string RuleCode, string Message, string Severity, bool Passed, DateTime CreatedAtUtc);

public sealed record CutOffBreachDto(Guid Id, DateTime ReceivedAtUtc, TimeOnly CutOffTime, string Reason, bool RequiresApproval, string? ApprovedByUserId);

public sealed record InstructionStatusHistoryDto(Guid Id, string Status, string ChangedByUserId, DateTime ChangedAtUtc, string? Comment);

public sealed record LienDto(
    Guid Id,
    Guid InvestorId,
    Guid SchemeId,
    Guid SchemeClassId,
    decimal? Units,
    decimal? Amount,
    string Currency,
    string DocumentationReference,
    string ApproverEvidenceReference,
    string Status,
    string PlacedByUserId,
    DateTime PlacedAtUtc,
    string? ReleasedByUserId,
    DateTime? ReleasedAtUtc,
    string? ReleaseEvidenceReference);

public sealed record RecurringContributionPlanDto(
    Guid Id,
    Guid InvestorId,
    Guid SchemeId,
    Guid SchemeClassId,
    string Frequency,
    decimal Amount,
    string Currency,
    string Channel,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string CollectionMethod,
    string Status,
    int MissedCollections,
    int FailedDebits);
