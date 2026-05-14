using System.ComponentModel.DataAnnotations;

namespace Cis.Contracts.DataQuality;

public sealed record CreateDataQualityRuleRequest(
    [Required] string RuleCode,
    [Required, StringLength(200)] string Name,
    [Required, StringLength(1000)] string Description,
    [Required] string Severity,
    int ThresholdDays);

public sealed record AssignDataQualityExceptionRequest(
    [Required, StringLength(200)] string OwnerUserId);

public sealed record ResolveDataQualityExceptionRequest(
    [Required, StringLength(500)] string EvidenceReference,
    [StringLength(1000)] string? Comment);

public sealed record DataQualityRuleDto(
    Guid Id,
    string RuleCode,
    string Name,
    string Description,
    string Severity,
    int ThresholdDays,
    string Status);

public sealed record DataQualityCheckRunDto(
    Guid Id,
    string RunNumber,
    string Status,
    string RequestedByUserId,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    int RulesEvaluated,
    int ExceptionsGenerated);

public sealed record DataQualityExceptionDto(
    Guid Id,
    Guid CheckRunId,
    string RuleCode,
    string Severity,
    string EntityType,
    string EntityId,
    string Message,
    string Status,
    DateTime DetectedAtUtc,
    DateTime DueAtUtc,
    bool IsOverdue,
    string? OwnerUserId,
    DateTime? AssignedAtUtc,
    string? ResolvedByUserId,
    DateTime? ResolvedAtUtc,
    string? ResolutionEvidenceReference,
    string? ResolutionComment);

public sealed record DataQualityDashboardDto(
    int OpenExceptions,
    int AssignedExceptions,
    int OverdueExceptions,
    int ResolvedExceptions,
    DateTime GeneratedAtUtc);
