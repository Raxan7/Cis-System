namespace Cis.Contracts.Audit;

public sealed record AuditLogDto(
    Guid Id,
    string Module,
    string EventType,
    string Action,
    string EntityType,
    string? EntityId,
    string? ActorUserId,
    string? ActorDisplayName,
    string? ActorRole,
    DateTime TimestampUtc,
    string? BeforeJson,
    string? AfterJson,
    string? IpAddress,
    string? CorrelationId,
    string? Reason,
    Guid? WorkflowId);
