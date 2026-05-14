using System.Text.Json;
using Cis.Application.Common.Interfaces;
using Cis.Contracts.Archive;
using Cis.Domain.Archive;
using Cis.Domain.Audit;
using Cis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Cis.Application.Common.Exceptions;

namespace Cis.Infrastructure.Archive;

internal sealed class ImmutableArchiveService : IImmutableArchiveService
{
    private readonly CisDbContext _dbContext;
    private readonly IAuditWriter _auditWriter;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ImmutableArchiveService(
        CisDbContext dbContext,
        IAuditWriter auditWriter,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _auditWriter = auditWriter;
        _currentUserContext = currentUserContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ArchiveRecordDto> CreateAsync(CreateArchiveRecordRequest request, CancellationToken cancellationToken = default)
    {
        ValidateJson(request.PayloadJson);
        var actor = _currentUserContext.UserId ?? "system";
        var record = ImmutableArchiveRecord.Create(
            request.Module,
            request.EntityType,
            request.EntityId,
            request.PayloadJson,
            actor,
            _dateTimeProvider.UtcNow,
            request.WorkflowId,
            request.RetentionPolicyId,
            request.Reason);

        _dbContext.ImmutableArchiveRecords.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        var dto = Map(record);
        await _auditWriter.WriteAsync(new AuditLogEntry(
            "Archive",
            "ArchiveRecordCreated",
            "ImmutableArchiveRecord",
            record.Id.ToString(),
            EventType: AuditEventType.Created,
            AfterJson: JsonSerializer.Serialize(dto),
            Reason: request.Reason,
            WorkflowId: request.WorkflowId,
            Summary: $"Archived {request.EntityType} {request.EntityId}."), cancellationToken);

        return dto;
    }

    public async Task<IReadOnlyCollection<ArchiveRecordDto>> GetAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.ImmutableArchiveRecords
            .AsNoTracking()
            .OrderByDescending(record => record.ArchivedAtUtc)
            .Take(500)
            .Select(record => new ArchiveRecordDto(
                record.Id,
                record.Module,
                record.EntityType,
                record.EntityId,
                record.PayloadJson,
                record.PayloadHash,
                record.ArchivedByUserId,
                record.ArchivedAtUtc,
                record.WorkflowId,
                record.RetentionPolicyId,
                record.Reason))
            .ToListAsync(cancellationToken);
    }

    public async Task<ArchiveRecordDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.ImmutableArchiveRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken)
            ?? throw new NotFoundException("Archive record was not found.");

        return Map(record);
    }

    private static void ValidateJson(string payloadJson)
    {
        try
        {
            using var _ = JsonDocument.Parse(payloadJson);
        }
        catch (JsonException exception)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["payloadJson"] = [$"Payload JSON is invalid: {exception.Message}"]
            });
        }
    }

    private static ArchiveRecordDto Map(ImmutableArchiveRecord record)
    {
        return new ArchiveRecordDto(
            record.Id,
            record.Module,
            record.EntityType,
            record.EntityId,
            record.PayloadJson,
            record.PayloadHash,
            record.ArchivedByUserId,
            record.ArchivedAtUtc,
            record.WorkflowId,
            record.RetentionPolicyId,
            record.Reason);
    }
}
