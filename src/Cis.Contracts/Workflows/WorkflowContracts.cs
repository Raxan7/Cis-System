using System.ComponentModel.DataAnnotations;

namespace Cis.Contracts.Workflows;

public sealed record CreateWorkflowRequest(
    [Required] string WorkflowType,
    [Required, StringLength(200)] string EntityType,
    [Required, StringLength(100)] string EntityId,
    [Required, StringLength(300)] string Title,
    [StringLength(1000)] string? Reason);

public sealed record WorkflowActionRequest(
    [StringLength(1000)] string? Comment);

public sealed record WorkflowDto(
    Guid Id,
    string WorkflowType,
    string EntityType,
    string EntityId,
    string Title,
    string Status,
    string InitiatedByUserId,
    string? SubmittedByUserId,
    DateTime? SubmittedAtUtc,
    string? CheckedByUserId,
    DateTime? CheckedAtUtc,
    string? ApprovedByUserId,
    DateTime? ApprovedAtUtc,
    string? RejectedByUserId,
    DateTime? RejectedAtUtc,
    string? DecisionComment,
    IReadOnlyCollection<WorkflowStepDto> Steps);

public sealed record WorkflowStepDto(
    string StepType,
    int StepOrder,
    string Status,
    string? CompletedByUserId,
    DateTime? CompletedAtUtc);

public sealed record ApprovalPolicyDto(
    Guid Id,
    string WorkflowType,
    string Name,
    string Module,
    bool RequiresChecker,
    bool RequiresApprover,
    bool IsActive);
