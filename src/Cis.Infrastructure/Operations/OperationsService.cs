using System.Text.Json;
using Cis.Application.Common.Exceptions;
using Cis.Application.Common.Interfaces;
using Cis.Contracts.Operations;
using Cis.Domain.Audit;
using Cis.Domain.Operations;
using Cis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cis.Infrastructure.Operations;

internal sealed class OperationsService : IOperationsService
{
    private const string ModuleName = "Operations";
    private readonly CisDbContext _dbContext;
    private readonly IAuditWriter _auditWriter;
    private readonly ICurrentUserContext _currentUserContext;

    public OperationsService(CisDbContext dbContext, IAuditWriter auditWriter, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _auditWriter = auditWriter;
        _currentUserContext = currentUserContext;
    }

    public async Task<IReadOnlyCollection<BackupRunRecordDto>> GetBackupRunsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.BackupRunRecords
            .AsNoTracking()
            .OrderByDescending(record => record.StartedAtUtc)
            .Take(500)
            .Select(record => new BackupRunRecordDto(
                record.Id,
                record.Environment.ToString(),
                record.BackupType.ToString(),
                record.DatabaseName,
                record.StorageReference,
                record.StartedAtUtc,
                record.CompletedAtUtc,
                record.Status.ToString(),
                record.SizeBytes,
                record.Sha256,
                record.ErrorMessage))
            .ToListAsync(cancellationToken);
    }

    public async Task<DrTestRecordDto> CreateDrTestAsync(CreateDrTestRequest request, CancellationToken cancellationToken = default)
    {
        var actor = _currentUserContext.UserId ?? throw new UnauthorizedAccessException("Authenticated user is required.");
        ValidateRequired(request.TestName, nameof(request.TestName));
        ValidateRequired(request.Scenario, nameof(request.Scenario));
        var environment = ParseEnum<OperationalEnvironment>(request.Environment, nameof(request.Environment));
        if (request.PlannedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ValidationException(new Dictionary<string, string[]> { [nameof(request.PlannedAtUtc)] = ["PlannedAtUtc must be UTC."] });
        }

        var record = DRTestRecord.Schedule(request.TestName, environment, request.Scenario, request.PlannedAtUtc, actor, request.EvidenceReference);
        _dbContext.DRTestRecords.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var dto = ToDto(record);
        await _auditWriter.WriteAsync(new AuditLogEntry(
            ModuleName,
            "DRTestScheduled",
            nameof(DRTestRecord),
            record.Id.ToString(),
            AuditEventType.Created,
            _currentUserContext.UserId,
            _currentUserContext.DisplayName,
            CurrentRoles(),
            _currentUserContext.CorrelationId,
            _currentUserContext.IpAddress,
            _currentUserContext.UserAgent,
            "DR test record scheduled.",
            null,
            null,
            JsonSerializer.Serialize(dto, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            request.Scenario),
            cancellationToken);

        return dto;
    }

    public async Task<IReadOnlyCollection<DrTestRecordDto>> GetDrTestsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.DRTestRecords
            .AsNoTracking()
            .OrderByDescending(record => record.PlannedAtUtc)
            .Take(500)
            .Select(record => new DrTestRecordDto(
                record.Id,
                record.TestName,
                record.Environment.ToString(),
                record.Scenario,
                record.PlannedAtUtc,
                record.StartedAtUtc,
                record.CompletedAtUtc,
                record.Status.ToString(),
                record.RtoAchievedMinutes,
                record.RpoAchievedMinutes,
                record.RequestedByUserId,
                record.EvidenceReference,
                record.Findings))
            .ToListAsync(cancellationToken);
    }

    private static BackupRunRecordDto ToDto(BackupRunRecord record)
    {
        return new BackupRunRecordDto(
            record.Id,
            record.Environment.ToString(),
            record.BackupType.ToString(),
            record.DatabaseName,
            record.StorageReference,
            record.StartedAtUtc,
            record.CompletedAtUtc,
            record.Status.ToString(),
            record.SizeBytes,
            record.Sha256,
            record.ErrorMessage);
    }

    private static DrTestRecordDto ToDto(DRTestRecord record)
    {
        return new DrTestRecordDto(
            record.Id,
            record.TestName,
            record.Environment.ToString(),
            record.Scenario,
            record.PlannedAtUtc,
            record.StartedAtUtc,
            record.CompletedAtUtc,
            record.Status.ToString(),
            record.RtoAchievedMinutes,
            record.RpoAchievedMinutes,
            record.RequestedByUserId,
            record.EvidenceReference,
            record.Findings);
    }

    private static TEnum ParseEnum<TEnum>(string value, string fieldName) where TEnum : struct
    {
        return Enum.TryParse<TEnum>(value, true, out var parsed)
            ? parsed
            : throw new ValidationException(new Dictionary<string, string[]> { [fieldName] = [$"{fieldName} has an invalid value."] });
    }

    private static void ValidateRequired(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ValidationException(new Dictionary<string, string[]> { [fieldName] = [$"{fieldName} is required."] });
        }
    }

    private string? CurrentRoles()
    {
        return _currentUserContext.Roles.Count == 0 ? null : string.Join(",", _currentUserContext.Roles);
    }
}
