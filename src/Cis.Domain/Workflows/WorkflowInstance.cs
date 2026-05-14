using Cis.Domain.Common;

namespace Cis.Domain.Workflows;

public sealed class WorkflowInstance : AuditableAggregateRoot
{
    private readonly List<WorkflowStep> _steps = [];
    private readonly List<WorkflowAction> _actions = [];

    private WorkflowInstance()
    {
    }

    private WorkflowInstance(
        WorkflowType workflowType,
        Guid approvalPolicyId,
        string entityType,
        string entityId,
        string title,
        string initiatedByUserId,
        DateTime createdAtUtc)
    {
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        WorkflowType = workflowType;
        ApprovalPolicyId = approvalPolicyId == Guid.Empty
            ? throw new ArgumentException("Approval policy id cannot be empty.", nameof(approvalPolicyId))
            : approvalPolicyId;
        EntityType = Required(entityType, nameof(entityType), 200);
        EntityId = Required(entityId, nameof(entityId), 100);
        Title = Required(title, nameof(title), 300);
        InitiatedByUserId = Required(initiatedByUserId, nameof(initiatedByUserId), 200);
        Status = WorkflowStatus.Draft;
        MarkCreated(initiatedByUserId, createdAtUtc);
        AddAction(WorkflowActionType.Created, initiatedByUserId, createdAtUtc, "Workflow created.");
    }

    public WorkflowType WorkflowType { get; private set; }

    public Guid ApprovalPolicyId { get; private set; }

    public ApprovalPolicy? ApprovalPolicy { get; private set; }

    public string EntityType { get; private set; } = string.Empty;

    public string EntityId { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public WorkflowStatus Status { get; private set; }

    public string InitiatedByUserId { get; private set; } = string.Empty;

    public string? SubmittedByUserId { get; private set; }

    public DateTime? SubmittedAtUtc { get; private set; }

    public string? CheckedByUserId { get; private set; }

    public DateTime? CheckedAtUtc { get; private set; }

    public string? ApprovedByUserId { get; private set; }

    public DateTime? ApprovedAtUtc { get; private set; }

    public string? RejectedByUserId { get; private set; }

    public DateTime? RejectedAtUtc { get; private set; }

    public string? DecisionComment { get; private set; }

    public IReadOnlyCollection<WorkflowStep> Steps => _steps.AsReadOnly();

    public IReadOnlyCollection<WorkflowAction> Actions => _actions.AsReadOnly();

    public static WorkflowInstance Create(
        WorkflowType workflowType,
        Guid approvalPolicyId,
        string entityType,
        string entityId,
        string title,
        string initiatedByUserId,
        DateTime createdAtUtc,
        bool requiresChecker,
        bool requiresApprover)
    {
        var workflow = new WorkflowInstance(workflowType, approvalPolicyId, entityType, entityId, title, initiatedByUserId, createdAtUtc);
        workflow._steps.Add(WorkflowStep.Create(workflow.Id, WorkflowStepType.Maker, 1));
        workflow._steps.Add(WorkflowStep.Create(workflow.Id, WorkflowStepType.Checker, 2));
        if (!requiresChecker)
        {
            workflow._steps[^1].Skip();
        }

        workflow._steps.Add(WorkflowStep.Create(workflow.Id, WorkflowStepType.Approver, 3));
        if (!requiresApprover)
        {
            workflow._steps[^1].Skip();
        }

        return workflow;
    }

    public void Submit(string actorUserId, DateTime occurredAtUtc, string? comment)
    {
        EnsureUtc(occurredAtUtc, nameof(occurredAtUtc));
        if (Status != WorkflowStatus.Draft)
        {
            throw new InvalidOperationException("Only draft workflows can be submitted.");
        }

        var actor = Required(actorUserId, nameof(actorUserId), 200);
        Status = WorkflowStatus.Submitted;
        SubmittedByUserId = actor;
        SubmittedAtUtc = occurredAtUtc;
        DecisionComment = Optional(comment, 1000);
        CompleteStep(WorkflowStepType.Maker, actor, occurredAtUtc);
        AddAction(WorkflowActionType.Submitted, actor, occurredAtUtc, comment);
    }

    public void Check(string actorUserId, DateTime occurredAtUtc, string? comment)
    {
        EnsureUtc(occurredAtUtc, nameof(occurredAtUtc));
        EnsureStatus(WorkflowStatus.Submitted, "Only submitted workflows can be checked.");
        var actor = Required(actorUserId, nameof(actorUserId), 200);
        EnsureDistinctRole(actor, "check");

        Status = WorkflowStatus.Checked;
        CheckedByUserId = actor;
        CheckedAtUtc = occurredAtUtc;
        DecisionComment = Optional(comment, 1000);
        CompleteStep(WorkflowStepType.Checker, actor, occurredAtUtc);
        AddAction(WorkflowActionType.Checked, actor, occurredAtUtc, comment);
    }

    public void Approve(string actorUserId, DateTime occurredAtUtc, string? comment)
    {
        EnsureUtc(occurredAtUtc, nameof(occurredAtUtc));
        var actor = Required(actorUserId, nameof(actorUserId), 200);
        var checkerStep = _steps.FirstOrDefault(step => step.StepType == WorkflowStepType.Checker);
        var expectedStatus = checkerStep?.Status == WorkflowStepStatus.Skipped
            ? WorkflowStatus.Submitted
            : WorkflowStatus.Checked;
        EnsureStatus(expectedStatus, "Workflow must complete required prior steps before approval.");
        EnsureDistinctRole(actor, "approve");

        Status = WorkflowStatus.Approved;
        ApprovedByUserId = actor;
        ApprovedAtUtc = occurredAtUtc;
        DecisionComment = Optional(comment, 1000);
        CompleteStep(WorkflowStepType.Approver, actor, occurredAtUtc);
        AddAction(WorkflowActionType.Approved, actor, occurredAtUtc, comment);
    }

    public void Reject(string actorUserId, DateTime occurredAtUtc, string comment)
    {
        EnsureUtc(occurredAtUtc, nameof(occurredAtUtc));
        if (Status is WorkflowStatus.Approved or WorkflowStatus.Rejected)
        {
            throw new InvalidOperationException("Finalized workflows cannot be rejected.");
        }

        var actor = Required(actorUserId, nameof(actorUserId), 200);
        EnsureDistinctRole(actor, "reject");

        Status = WorkflowStatus.Rejected;
        RejectedByUserId = actor;
        RejectedAtUtc = occurredAtUtc;
        DecisionComment = Required(comment, nameof(comment), 1000);
        AddAction(WorkflowActionType.Rejected, actor, occurredAtUtc, comment);
    }

    private void CompleteStep(WorkflowStepType stepType, string actorUserId, DateTime occurredAtUtc)
    {
        var step = _steps.FirstOrDefault(candidate => candidate.StepType == stepType)
            ?? throw new InvalidOperationException($"Workflow step {stepType} is not configured.");

        if (step.Status == WorkflowStepStatus.Skipped)
        {
            return;
        }

        step.Complete(actorUserId, occurredAtUtc);
    }

    private void AddAction(WorkflowActionType actionType, string actorUserId, DateTime occurredAtUtc, string? comment)
    {
        _actions.Add(WorkflowAction.Create(Id, actionType, actorUserId, occurredAtUtc, comment));
    }

    private void EnsureDistinctRole(string actorUserId, string attemptedRole)
    {
        var priorActors = new[]
        {
            InitiatedByUserId,
            SubmittedByUserId,
            CheckedByUserId
        }.Where(value => !string.IsNullOrWhiteSpace(value));

        if (priorActors.Any(value => string.Equals(value, actorUserId, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"The same user cannot {attemptedRole} a workflow after performing a conflicting workflow role.");
        }
    }

    private void EnsureStatus(WorkflowStatus expectedStatus, string message)
    {
        if (Status != expectedStatus)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static string Required(string value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{parameterName} is required.", parameterName);
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException($"{parameterName} cannot exceed {maxLength} characters.", parameterName);
        }

        return trimmed;
    }

    private static string? Optional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException($"Value cannot exceed {maxLength} characters.");
        }

        return trimmed;
    }

    private static void EnsureUtc(DateTime timestamp, string parameterName)
    {
        if (timestamp.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Timestamp must be UTC.", parameterName);
        }
    }
}
