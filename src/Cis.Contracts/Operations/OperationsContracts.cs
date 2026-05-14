namespace Cis.Contracts.Operations;

public sealed record CreateDrTestRequest(
    string TestName,
    string Environment,
    string Scenario,
    DateTime PlannedAtUtc,
    string? EvidenceReference);

public sealed record RtoRpoConfigurationDto(
    Guid Id,
    string Environment,
    string SystemName,
    int RtoMinutes,
    int RpoMinutes,
    int BackupFrequencyMinutes,
    DateTime EffectiveFromUtc,
    DateTime? EffectiveToUtc,
    bool IsActive);

public sealed record BackupRunRecordDto(
    Guid Id,
    string Environment,
    string BackupType,
    string DatabaseName,
    string StorageReference,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    string Status,
    long? SizeBytes,
    string? Sha256,
    string? ErrorMessage);

public sealed record RestoreTestRecordDto(
    Guid Id,
    string Environment,
    Guid? BackupRunRecordId,
    string TargetDatabaseName,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    string Status,
    string? EvidenceReference,
    string? ValidationSummary);

public sealed record DrTestRecordDto(
    Guid Id,
    string TestName,
    string Environment,
    string Scenario,
    DateTime PlannedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc,
    string Status,
    int? RtoAchievedMinutes,
    int? RpoAchievedMinutes,
    string RequestedByUserId,
    string? EvidenceReference,
    string? Findings);

public sealed record DeepHealthDto(
    string Status,
    DateTime TimestampUtc,
    decimal DurationMilliseconds,
    IReadOnlyCollection<DeepHealthEntryDto> Entries);

public sealed record DeepHealthEntryDto(
    string Name,
    string Status,
    decimal DurationMilliseconds,
    string? Description,
    string? Error);
