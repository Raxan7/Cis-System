using Cis.Application.Common.Interfaces;
using Cis.Contracts.Audit;
using Cis.Domain.Audit;
using Cis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cis.Infrastructure.Audit;

internal sealed class AuditLogWriter : IAuditLogWriter, IAuditQueryService
{
    private readonly CisDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AuditLogWriter(
        CisDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        var auditLog = AuditLog.Create(
            entry.Module,
            entry.Action,
            entry.EntityName,
            entry.EntityId,
            entry.ActorId ?? _currentUserContext.UserId,
            entry.ActorDisplayName ?? _currentUserContext.DisplayName,
            _dateTimeProvider.UtcNow,
            entry.EventType,
            entry.ActorRole ?? CurrentRoles(),
            entry.CorrelationId ?? _currentUserContext.CorrelationId,
            entry.IpAddress ?? _currentUserContext.IpAddress,
            entry.UserAgent ?? _currentUserContext.UserAgent,
            entry.Summary,
            entry.ChangesJson,
            entry.BeforeJson,
            entry.AfterJson,
            entry.Reason,
            entry.WorkflowId,
            entry.IdempotencyKey);

        _dbContext.AuditLogs.Add(auditLog);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<AuditLogDto>> GetAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.AuditLogs
            .AsNoTracking()
            .OrderByDescending(auditLog => auditLog.OccurredAtUtc)
            .ThenByDescending(auditLog => auditLog.Id)
            .Take(500)
            .Select(auditLog => new AuditLogDto(
                auditLog.Id,
                auditLog.Module,
                auditLog.EventType.ToString(),
                auditLog.Action,
                auditLog.EntityName,
                auditLog.EntityId,
                auditLog.ActorId,
                auditLog.ActorDisplayName,
                auditLog.ActorRole,
                auditLog.OccurredAtUtc,
                auditLog.BeforeJson,
                auditLog.AfterJson,
                auditLog.IpAddress,
                auditLog.CorrelationId,
                auditLog.Reason ?? auditLog.Summary,
                auditLog.WorkflowId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<AuditLogDto>> GetForEntityAsync(
        string entityType,
        string entityId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.AuditLogs
            .AsNoTracking()
            .Where(auditLog => auditLog.EntityName == entityType && auditLog.EntityId == entityId)
            .OrderBy(auditLog => auditLog.OccurredAtUtc)
            .Select(auditLog => new AuditLogDto(
                auditLog.Id,
                auditLog.Module,
                auditLog.EventType.ToString(),
                auditLog.Action,
                auditLog.EntityName,
                auditLog.EntityId,
                auditLog.ActorId,
                auditLog.ActorDisplayName,
                auditLog.ActorRole,
                auditLog.OccurredAtUtc,
                auditLog.BeforeJson,
                auditLog.AfterJson,
                auditLog.IpAddress,
                auditLog.CorrelationId,
                auditLog.Reason ?? auditLog.Summary,
                auditLog.WorkflowId))
            .ToListAsync(cancellationToken);
    }

    private string? CurrentRoles()
    {
        return _currentUserContext.Roles.Count == 0
            ? null
            : string.Join(",", _currentUserContext.Roles);
    }

}
