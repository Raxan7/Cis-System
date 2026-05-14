using Cis.Domain.Common;

namespace Cis.Domain.Operations;

public sealed class RtoRpoConfiguration : AuditableAggregateRoot
{
    private RtoRpoConfiguration()
    {
    }

    private RtoRpoConfiguration(OperationalEnvironment environment, string systemName, int rtoMinutes, int rpoMinutes, int backupFrequencyMinutes, DateTime effectiveFromUtc, string createdByUserId)
    {
        Environment = environment;
        SystemName = OperationsValidation.Required(systemName, nameof(systemName), 120);
        RtoMinutes = OperationsValidation.Positive(rtoMinutes, nameof(rtoMinutes));
        RpoMinutes = OperationsValidation.Positive(rpoMinutes, nameof(rpoMinutes));
        BackupFrequencyMinutes = OperationsValidation.Positive(backupFrequencyMinutes, nameof(backupFrequencyMinutes));
        EffectiveFromUtc = OperationsValidation.EnsureUtc(effectiveFromUtc, nameof(effectiveFromUtc));
        IsActive = true;
        MarkCreated(createdByUserId, effectiveFromUtc);
    }

    public OperationalEnvironment Environment { get; private set; }
    public string SystemName { get; private set; } = string.Empty;
    public int RtoMinutes { get; private set; }
    public int RpoMinutes { get; private set; }
    public int BackupFrequencyMinutes { get; private set; }
    public DateTime EffectiveFromUtc { get; private set; }
    public DateTime? EffectiveToUtc { get; private set; }
    public bool IsActive { get; private set; }

    public static RtoRpoConfiguration Create(OperationalEnvironment environment, string systemName, int rtoMinutes, int rpoMinutes, int backupFrequencyMinutes, DateTime effectiveFromUtc, string createdByUserId)
    {
        return new RtoRpoConfiguration(environment, systemName, rtoMinutes, rpoMinutes, backupFrequencyMinutes, effectiveFromUtc, createdByUserId);
    }

    public void Retire(DateTime effectiveToUtc)
    {
        EffectiveToUtc = OperationsValidation.EnsureUtc(effectiveToUtc, nameof(effectiveToUtc));
        IsActive = false;
    }
}

public sealed class DRTestRecord : AuditableAggregateRoot
{
    private DRTestRecord()
    {
    }

    private DRTestRecord(string testName, OperationalEnvironment environment, string scenario, DateTime plannedAtUtc, string requestedByUserId, string? evidenceReference)
    {
        TestName = OperationsValidation.Required(testName, nameof(testName), 200);
        Environment = environment;
        Scenario = OperationsValidation.Required(scenario, nameof(scenario), 2000);
        PlannedAtUtc = OperationsValidation.EnsureUtc(plannedAtUtc, nameof(plannedAtUtc));
        RequestedByUserId = OperationsValidation.Required(requestedByUserId, nameof(requestedByUserId), 200);
        EvidenceReference = OperationsValidation.Optional(evidenceReference, 500);
        Status = OperationalRecordStatus.Scheduled;
        MarkCreated(requestedByUserId, DateTime.UtcNow);
    }

    public string TestName { get; private set; } = string.Empty;
    public OperationalEnvironment Environment { get; private set; }
    public string Scenario { get; private set; } = string.Empty;
    public DateTime PlannedAtUtc { get; private set; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public OperationalRecordStatus Status { get; private set; }
    public int? RtoAchievedMinutes { get; private set; }
    public int? RpoAchievedMinutes { get; private set; }
    public string RequestedByUserId { get; private set; } = string.Empty;
    public string? EvidenceReference { get; private set; }
    public string? Findings { get; private set; }

    public static DRTestRecord Schedule(string testName, OperationalEnvironment environment, string scenario, DateTime plannedAtUtc, string requestedByUserId, string? evidenceReference)
    {
        return new DRTestRecord(testName, environment, scenario, plannedAtUtc, requestedByUserId, evidenceReference);
    }

    public void Complete(DateTime startedAtUtc, DateTime completedAtUtc, int rtoAchievedMinutes, int rpoAchievedMinutes, string evidenceReference, string findings)
    {
        StartedAtUtc = OperationsValidation.EnsureUtc(startedAtUtc, nameof(startedAtUtc));
        CompletedAtUtc = OperationsValidation.EnsureUtc(completedAtUtc, nameof(completedAtUtc));
        RtoAchievedMinutes = OperationsValidation.Positive(rtoAchievedMinutes, nameof(rtoAchievedMinutes));
        RpoAchievedMinutes = OperationsValidation.Positive(rpoAchievedMinutes, nameof(rpoAchievedMinutes));
        EvidenceReference = OperationsValidation.Required(evidenceReference, nameof(evidenceReference), 500);
        Findings = OperationsValidation.Required(findings, nameof(findings), 2000);
        Status = OperationalRecordStatus.Completed;
    }
}

public sealed class BackupRunRecord : AuditableAggregateRoot
{
    private BackupRunRecord()
    {
    }

    private BackupRunRecord(OperationalEnvironment environment, BackupType backupType, string databaseName, string storageReference, DateTime startedAtUtc, string initiatedByUserId)
    {
        Environment = environment;
        BackupType = backupType;
        DatabaseName = OperationsValidation.Required(databaseName, nameof(databaseName), 120);
        StorageReference = OperationsValidation.Required(storageReference, nameof(storageReference), 500);
        StartedAtUtc = OperationsValidation.EnsureUtc(startedAtUtc, nameof(startedAtUtc));
        Status = OperationalRecordStatus.Running;
        InitiatedByUserId = OperationsValidation.Required(initiatedByUserId, nameof(initiatedByUserId), 200);
        MarkCreated(initiatedByUserId, startedAtUtc);
    }

    public OperationalEnvironment Environment { get; private set; }
    public BackupType BackupType { get; private set; }
    public string DatabaseName { get; private set; } = string.Empty;
    public string StorageReference { get; private set; } = string.Empty;
    public DateTime StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public OperationalRecordStatus Status { get; private set; }
    public long? SizeBytes { get; private set; }
    public string? Sha256 { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string InitiatedByUserId { get; private set; } = string.Empty;

    public static BackupRunRecord Start(OperationalEnvironment environment, BackupType backupType, string databaseName, string storageReference, DateTime startedAtUtc, string initiatedByUserId)
    {
        return new BackupRunRecord(environment, backupType, databaseName, storageReference, startedAtUtc, initiatedByUserId);
    }

    public void Complete(DateTime completedAtUtc, long sizeBytes, string sha256)
    {
        CompletedAtUtc = OperationsValidation.EnsureUtc(completedAtUtc, nameof(completedAtUtc));
        SizeBytes = sizeBytes < 0 ? throw new ArgumentOutOfRangeException(nameof(sizeBytes)) : sizeBytes;
        Sha256 = OperationsValidation.Required(sha256, nameof(sha256), 128);
        Status = OperationalRecordStatus.Completed;
    }

    public void Fail(DateTime completedAtUtc, string errorMessage)
    {
        CompletedAtUtc = OperationsValidation.EnsureUtc(completedAtUtc, nameof(completedAtUtc));
        ErrorMessage = OperationsValidation.Required(errorMessage, nameof(errorMessage), 1000);
        Status = OperationalRecordStatus.Failed;
    }
}

public sealed class RestoreTestRecord : AuditableAggregateRoot
{
    private RestoreTestRecord()
    {
    }

    private RestoreTestRecord(OperationalEnvironment environment, Guid? backupRunRecordId, string targetDatabaseName, DateTime startedAtUtc, string initiatedByUserId)
    {
        Environment = environment;
        BackupRunRecordId = backupRunRecordId;
        TargetDatabaseName = OperationsValidation.Required(targetDatabaseName, nameof(targetDatabaseName), 120);
        StartedAtUtc = OperationsValidation.EnsureUtc(startedAtUtc, nameof(startedAtUtc));
        Status = OperationalRecordStatus.Running;
        InitiatedByUserId = OperationsValidation.Required(initiatedByUserId, nameof(initiatedByUserId), 200);
        MarkCreated(initiatedByUserId, startedAtUtc);
    }

    public OperationalEnvironment Environment { get; private set; }
    public Guid? BackupRunRecordId { get; private set; }
    public string TargetDatabaseName { get; private set; } = string.Empty;
    public DateTime StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public OperationalRecordStatus Status { get; private set; }
    public string? EvidenceReference { get; private set; }
    public string? ValidationSummary { get; private set; }
    public string InitiatedByUserId { get; private set; } = string.Empty;

    public static RestoreTestRecord Start(OperationalEnvironment environment, Guid? backupRunRecordId, string targetDatabaseName, DateTime startedAtUtc, string initiatedByUserId)
    {
        return new RestoreTestRecord(environment, backupRunRecordId, targetDatabaseName, startedAtUtc, initiatedByUserId);
    }

    public void Complete(DateTime completedAtUtc, string evidenceReference, string validationSummary)
    {
        CompletedAtUtc = OperationsValidation.EnsureUtc(completedAtUtc, nameof(completedAtUtc));
        EvidenceReference = OperationsValidation.Required(evidenceReference, nameof(evidenceReference), 500);
        ValidationSummary = OperationsValidation.Required(validationSummary, nameof(validationSummary), 2000);
        Status = OperationalRecordStatus.Completed;
    }
}
