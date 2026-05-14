using Cis.Domain.Common;

namespace Cis.Domain.Audit;

public sealed class AuditLog : AggregateRoot
{
    private AuditLog()
    {
    }

    private AuditLog(
        string module,
        AuditEventType eventType,
        string action,
        string entityName,
        string? entityId,
        string? actorId,
        string? actorDisplayName,
        string? actorRole,
        DateTime occurredAtUtc,
        string? correlationId,
        string? ipAddress,
        string? userAgent,
        string? summary,
        string? changesJson,
        string? beforeJson,
        string? afterJson,
        string? reason,
        Guid? workflowId,
        string? idempotencyKey)
    {
        if (occurredAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Audit log timestamp must be UTC.", nameof(occurredAtUtc));
        }

        Module = Required(module, nameof(module), 100);
        EventType = eventType;
        Action = Required(action, nameof(action), 100);
        EntityName = Required(entityName, nameof(entityName), 200);
        EntityId = Optional(entityId, 100);
        ActorId = Optional(actorId, 200);
        ActorDisplayName = Optional(actorDisplayName, 200);
        ActorRole = Optional(actorRole, 500);
        OccurredAtUtc = occurredAtUtc;
        CorrelationId = Optional(correlationId, 100);
        IpAddress = Optional(ipAddress, 100);
        UserAgent = Optional(userAgent, 500);
        Summary = Optional(summary, 1000);
        ChangesJson = changesJson ?? afterJson;
        BeforeJson = beforeJson;
        AfterJson = afterJson ?? changesJson;
        Reason = Optional(reason, 1000);
        WorkflowId = workflowId;
        IdempotencyKey = Optional(idempotencyKey, 200);
    }

    public string Module { get; private set; } = string.Empty;

    public AuditEventType EventType { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public string EntityName { get; private set; } = string.Empty;

    public string EntityType => EntityName;

    public string? EntityId { get; private set; }

    public string? ActorId { get; private set; }

    public string? ActorUserId => ActorId;

    public string? ActorDisplayName { get; private set; }

    public string? ActorRole { get; private set; }

    public DateTime OccurredAtUtc { get; private set; }

    public DateTime TimestampUtc => OccurredAtUtc;

    public string? CorrelationId { get; private set; }

    public string? IpAddress { get; private set; }

    public string? UserAgent { get; private set; }

    public string? Summary { get; private set; }

    public string? ChangesJson { get; private set; }

    public string? BeforeJson { get; private set; }

    public string? AfterJson { get; private set; }

    public string? Reason { get; private set; }

    public Guid? WorkflowId { get; private set; }

    public string? IdempotencyKey { get; private set; }

    public static AuditLog Create(
        string module,
        string action,
        string entityName,
        string? entityId,
        string? actorId,
        string? actorDisplayName,
        DateTime occurredAtUtc,
        AuditEventType? eventType = null,
        string? actorRole = null,
        string? correlationId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? summary = null,
        string? changesJson = null,
        string? beforeJson = null,
        string? afterJson = null,
        string? reason = null,
        Guid? workflowId = null,
        string? idempotencyKey = null)
    {
        return new AuditLog(
            module,
            eventType ?? InferEventType(action),
            action,
            entityName,
            entityId,
            actorId,
            actorDisplayName,
            actorRole,
            occurredAtUtc,
            correlationId,
            ipAddress,
            userAgent,
            summary,
            changesJson,
            beforeJson,
            afterJson,
            reason,
            workflowId,
            idempotencyKey);
    }

    private static AuditEventType InferEventType(string action)
    {
        var normalized = Required(action, nameof(action), 100).Replace(" ", string.Empty, StringComparison.Ordinal);
        if (Enum.TryParse<AuditEventType>(normalized, ignoreCase: true, out var eventType))
        {
            return eventType;
        }

        if (normalized.Contains("Role", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("AccessChange", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("Privilege", StringComparison.OrdinalIgnoreCase))
        {
            return AuditEventType.RoleChanged;
        }

        if (normalized.Contains("Reject", StringComparison.OrdinalIgnoreCase))
        {
            return AuditEventType.Rejected;
        }

        if (normalized.Contains("Approve", StringComparison.OrdinalIgnoreCase))
        {
            return AuditEventType.Approved;
        }

        if (normalized.Contains("Submit", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("Request", StringComparison.OrdinalIgnoreCase))
        {
            return AuditEventType.Submitted;
        }

        if (normalized.Contains("Create", StringComparison.OrdinalIgnoreCase))
        {
            return AuditEventType.Created;
        }

        if (normalized.Contains("Update", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("Change", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("Reset", StringComparison.OrdinalIgnoreCase))
        {
            return AuditEventType.Updated;
        }

        return AuditEventType.Updated;
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
}
