using Cis.Domain.Common;

namespace Cis.Domain.Workflows;

public sealed class WorkflowStep : Entity
{
    private WorkflowStep()
    {
    }

    private WorkflowStep(Guid workflowInstanceId, WorkflowStepType stepType, int stepOrder)
    {
        WorkflowInstanceId = workflowInstanceId == Guid.Empty
            ? throw new ArgumentException("Workflow id cannot be empty.", nameof(workflowInstanceId))
            : workflowInstanceId;
        StepType = stepType;
        StepOrder = stepOrder <= 0 ? throw new ArgumentException("Step order must be positive.", nameof(stepOrder)) : stepOrder;
        Status = WorkflowStepStatus.Pending;
    }

    public Guid WorkflowInstanceId { get; private set; }

    public WorkflowStepType StepType { get; private set; }

    public int StepOrder { get; private set; }

    public WorkflowStepStatus Status { get; private set; }

    public string? CompletedByUserId { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    public WorkflowInstance? WorkflowInstance { get; private set; }

    public static WorkflowStep Create(Guid workflowInstanceId, WorkflowStepType stepType, int stepOrder)
    {
        return new WorkflowStep(workflowInstanceId, stepType, stepOrder);
    }

    public void Complete(string actorUserId, DateTime occurredAtUtc)
    {
        EnsureUtc(occurredAtUtc, nameof(occurredAtUtc));
        if (Status == WorkflowStepStatus.Completed)
        {
            return;
        }

        Status = WorkflowStepStatus.Completed;
        CompletedByUserId = Required(actorUserId, nameof(actorUserId), 200);
        CompletedAtUtc = occurredAtUtc;
    }

    public void Skip()
    {
        if (Status == WorkflowStepStatus.Completed)
        {
            throw new InvalidOperationException("Completed workflow steps cannot be skipped.");
        }

        Status = WorkflowStepStatus.Skipped;
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

    private static void EnsureUtc(DateTime timestamp, string parameterName)
    {
        if (timestamp.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Timestamp must be UTC.", parameterName);
        }
    }
}
