using Cis.Domain.Common;

namespace Cis.Domain.Workflows;

public sealed class WorkflowAction : Entity
{
    private WorkflowAction()
    {
    }

    private WorkflowAction(Guid workflowInstanceId, WorkflowActionType actionType, string actorUserId, DateTime occurredAtUtc, string? comment)
    {
        EnsureUtc(occurredAtUtc, nameof(occurredAtUtc));
        WorkflowInstanceId = workflowInstanceId == Guid.Empty
            ? throw new ArgumentException("Workflow id cannot be empty.", nameof(workflowInstanceId))
            : workflowInstanceId;
        ActionType = actionType;
        ActorUserId = Required(actorUserId, nameof(actorUserId), 200);
        OccurredAtUtc = occurredAtUtc;
        Comment = Optional(comment, 1000);
    }

    public Guid WorkflowInstanceId { get; private set; }

    public WorkflowActionType ActionType { get; private set; }

    public string ActorUserId { get; private set; } = string.Empty;

    public DateTime OccurredAtUtc { get; private set; }

    public string? Comment { get; private set; }

    public WorkflowInstance? WorkflowInstance { get; private set; }

    public static WorkflowAction Create(
        Guid workflowInstanceId,
        WorkflowActionType actionType,
        string actorUserId,
        DateTime occurredAtUtc,
        string? comment)
    {
        return new WorkflowAction(workflowInstanceId, actionType, actorUserId, occurredAtUtc, comment);
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
