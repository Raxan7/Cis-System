using Cis.Domain.Audit;

namespace Cis.Application.Common.Interfaces;

public interface IAuditWriter
{
    Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);
}

public interface IAuditLogWriter : IAuditWriter
{
}

public sealed record AuditLogEntry(
    string Module,
    string Action,
    string EntityName,
    string? EntityId,
    AuditEventType? EventType = null,
    string? ActorId = null,
    string? ActorDisplayName = null,
    string? ActorRole = null,
    string? CorrelationId = null,
    string? IpAddress = null,
    string? UserAgent = null,
    string? Summary = null,
    string? ChangesJson = null,
    string? BeforeJson = null,
    string? AfterJson = null,
    string? Reason = null,
    Guid? WorkflowId = null,
    string? IdempotencyKey = null);
