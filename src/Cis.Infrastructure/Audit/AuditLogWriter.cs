using Cis.Application.Common.Interfaces;
using Cis.Contracts;
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

    public async Task<PagedResult<AuditLogDto>> GetAsync(PaginationRequest pagination, CancellationToken cancellationToken = default)
    {
        var query = ApplyAuditLogFilters(_dbContext.AuditLogs.AsNoTracking(), pagination);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await ApplyAuditLogSorting(query, pagination)
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
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

        return new PagedResult<AuditLogDto>(items, totalCount, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<PagedResult<AuditLogDto>> GetForEntityAsync(
        string entityType,
        string entityId,
        PaginationRequest pagination,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyAuditLogFilters(
            _dbContext.AuditLogs
            .AsNoTracking()
            .Where(auditLog => auditLog.EntityName == entityType && auditLog.EntityId == entityId),
            pagination);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await ApplyAuditLogSorting(query, pagination)
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .AsNoTracking()
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

        return new PagedResult<AuditLogDto>(items, totalCount, pagination.PageNumber, pagination.PageSize);
    }

    private static IQueryable<AuditLog> ApplyAuditLogFilters(IQueryable<AuditLog> query, PaginationRequest pagination)
    {
        if (string.IsNullOrWhiteSpace(pagination.Search))
        {
            return query;
        }

        var search = pagination.Search.Trim();
        return query.Where(auditLog =>
            auditLog.Module.Contains(search) ||
            auditLog.Action.Contains(search) ||
            auditLog.EntityName.Contains(search) ||
            (auditLog.EntityId != null && auditLog.EntityId.Contains(search)) ||
            (auditLog.ActorDisplayName != null && auditLog.ActorDisplayName.Contains(search)) ||
            (auditLog.ActorId != null && auditLog.ActorId.Contains(search)));
    }

    private static IQueryable<AuditLog> ApplyAuditLogSorting(IQueryable<AuditLog> query, PaginationRequest pagination)
    {
        var descending = pagination.IsDescending;
        return (pagination.SortBy ?? string.Empty).Trim().ToUpperInvariant() switch
        {
            "MODULE" => descending ? query.OrderByDescending(auditLog => auditLog.Module).ThenByDescending(auditLog => auditLog.OccurredAtUtc) : query.OrderBy(auditLog => auditLog.Module).ThenByDescending(auditLog => auditLog.OccurredAtUtc),
            "ACTION" => descending ? query.OrderByDescending(auditLog => auditLog.Action).ThenByDescending(auditLog => auditLog.OccurredAtUtc) : query.OrderBy(auditLog => auditLog.Action).ThenByDescending(auditLog => auditLog.OccurredAtUtc),
            "EVENTTYPE" => descending ? query.OrderByDescending(auditLog => auditLog.EventType).ThenByDescending(auditLog => auditLog.OccurredAtUtc) : query.OrderBy(auditLog => auditLog.EventType).ThenByDescending(auditLog => auditLog.OccurredAtUtc),
            _ => descending || string.IsNullOrWhiteSpace(pagination.SortBy)
                ? query.OrderByDescending(auditLog => auditLog.OccurredAtUtc).ThenByDescending(auditLog => auditLog.Id)
                : query.OrderBy(auditLog => auditLog.OccurredAtUtc).ThenBy(auditLog => auditLog.Id)
        };
    }

    private string? CurrentRoles()
    {
        return _currentUserContext.Roles.Count == 0
            ? null
            : string.Join(",", _currentUserContext.Roles);
    }

}
