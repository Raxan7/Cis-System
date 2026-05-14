using System.ComponentModel.DataAnnotations;

namespace Cis.Contracts.Cases;

public sealed record CreateCaseRequest(
    Guid? InvestorId,
    [Required, StringLength(100)] string Category,
    [Required, StringLength(200)] string Subject,
    [Required, StringLength(4000)] string Description,
    [Required] string Priority,
    [Required, StringLength(80)] string ComplaintSource,
    bool IsRegulatory,
    [StringLength(120)] string? RegulatoryCategory);

public sealed record AssignCaseRequest(
    [Required, StringLength(200)] string OwnerUserId);

public sealed record AddCaseActionRequest(
    [Required] string ActionType,
    [Required, StringLength(2000)] string Summary,
    [StringLength(1000)] string? EvidenceReference);

public sealed record EscalateCaseRequest(
    [Required, StringLength(2000)] string Reason,
    [Required, StringLength(120)] string EscalatedToRole,
    [StringLength(200)] string? EscalatedToUserId);

public sealed record ResolveCaseRequest(
    [Required, StringLength(2000)] string ResolutionSummary,
    [Required, StringLength(1000)] string ResolutionEvidenceReference);

public sealed record CaseActionDto(
    Guid Id,
    string ActionType,
    string Summary,
    string? EvidenceReference,
    string ActionedByUserId,
    DateTime ActionedAtUtc);

public sealed record CaseEscalationDto(
    Guid Id,
    string Reason,
    string EscalatedToRole,
    string? EscalatedToUserId,
    string EscalatedByUserId,
    DateTime EscalatedAtUtc,
    string Status,
    string? ResolvedByUserId,
    DateTime? ResolvedAtUtc);

public sealed record CaseStatusHistoryDto(
    Guid Id,
    string? FromStatus,
    string ToStatus,
    string ChangedByUserId,
    DateTime ChangedAtUtc,
    string Reason);

public sealed record ComplaintDto(
    Guid Id,
    Guid ServiceCaseId,
    string ComplaintReference,
    string Source,
    DateTime ReceivedAtUtc,
    bool IsRegulatory,
    string? RegulatoryCategory,
    int? TurnaroundDays);

public sealed record ServiceCaseDto(
    Guid Id,
    string CaseNumber,
    Guid? InvestorId,
    string CaseType,
    string Category,
    string Subject,
    string Description,
    string Priority,
    string Status,
    string? OwnerUserId,
    string LoggedByUserId,
    DateTime LoggedAtUtc,
    DateOnly BusinessDate,
    DateTime SlaTargetAtUtc,
    bool IsSlaBreached,
    int AgingDays,
    string? ResolvedByUserId,
    DateTime? ResolvedAtUtc,
    string? ResolutionSummary,
    string? ResolutionEvidenceReference,
    ComplaintDto? Complaint,
    IReadOnlyCollection<CaseActionDto> Actions,
    IReadOnlyCollection<CaseEscalationDto> Escalations,
    IReadOnlyCollection<CaseStatusHistoryDto> StatusHistory);

public sealed record CaseDashboardDto(
    int TotalCases,
    int OpenCases,
    int ResolvedCases,
    int EscalatedCases,
    int SlaBreachedCases,
    decimal AverageTurnaroundDays);
