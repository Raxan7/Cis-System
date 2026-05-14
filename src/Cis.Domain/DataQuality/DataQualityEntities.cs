using Cis.Domain.Common;

namespace Cis.Domain.DataQuality;

public sealed class DataQualityRule : AuditableAggregateRoot
{
    private DataQualityRule()
    {
    }

    private DataQualityRule(DataQualityRuleCode code, string name, string description, DataQualitySeverity severity, int thresholdDays, string createdByUserId, DateTime createdAtUtc)
    {
        Code = code;
        Name = DataQualityValidation.Required(name, nameof(name), 200);
        Description = DataQualityValidation.Required(description, nameof(description), 1000);
        Severity = severity;
        ThresholdDays = thresholdDays < 0 ? throw new ArgumentOutOfRangeException(nameof(thresholdDays), "Threshold days cannot be negative.") : thresholdDays;
        Status = DataQualityRuleStatus.Active;
        MarkCreated(createdByUserId, DataQualityValidation.EnsureUtc(createdAtUtc, nameof(createdAtUtc)));
    }

    public DataQualityRuleCode Code { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DataQualitySeverity Severity { get; private set; }
    public int ThresholdDays { get; private set; }
    public DataQualityRuleStatus Status { get; private set; }

    public static DataQualityRule Create(DataQualityRuleCode code, string name, string description, DataQualitySeverity severity, int thresholdDays, string createdByUserId, DateTime createdAtUtc)
    {
        return new DataQualityRule(code, name, description, severity, thresholdDays, createdByUserId, createdAtUtc);
    }
}

public sealed class DataQualityCheckRun : AuditableAggregateRoot
{
    private DataQualityCheckRun()
    {
    }

    private DataQualityCheckRun(string runNumber, string requestedByUserId, DateTime startedAtUtc)
    {
        RunNumber = DataQualityValidation.Required(runNumber, nameof(runNumber), 50);
        RequestedByUserId = DataQualityValidation.Required(requestedByUserId, nameof(requestedByUserId), 200);
        StartedAtUtc = DataQualityValidation.EnsureUtc(startedAtUtc, nameof(startedAtUtc));
        Status = DataQualityRunStatus.Running;
        MarkCreated(requestedByUserId, startedAtUtc);
    }

    public string RunNumber { get; private set; } = string.Empty;
    public DataQualityRunStatus Status { get; private set; }
    public string RequestedByUserId { get; private set; } = string.Empty;
    public DateTime StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public int RulesEvaluated { get; private set; }
    public int ExceptionsGenerated { get; private set; }

    public static DataQualityCheckRun Start(string runNumber, string requestedByUserId, DateTime startedAtUtc)
    {
        return new DataQualityCheckRun(runNumber, requestedByUserId, startedAtUtc);
    }

    public void Complete(int rulesEvaluated, int exceptionsGenerated, DateTime completedAtUtc)
    {
        RulesEvaluated = rulesEvaluated;
        ExceptionsGenerated = exceptionsGenerated;
        CompletedAtUtc = DataQualityValidation.EnsureUtc(completedAtUtc, nameof(completedAtUtc));
        Status = DataQualityRunStatus.Completed;
    }
}

public sealed class DataQualityException : AuditableAggregateRoot
{
    private DataQualityException()
    {
    }

    private DataQualityException(Guid checkRunId, Guid ruleId, DataQualityRuleCode ruleCode, DataQualitySeverity severity, string entityType, string entityId, string message, DateTime detectedAtUtc, DateTime dueAtUtc)
    {
        CheckRunId = checkRunId;
        RuleId = ruleId;
        RuleCode = ruleCode;
        Severity = severity;
        EntityType = DataQualityValidation.Required(entityType, nameof(entityType), 100);
        EntityId = DataQualityValidation.Required(entityId, nameof(entityId), 100);
        Message = DataQualityValidation.Required(message, nameof(message), 1000);
        DetectedAtUtc = DataQualityValidation.EnsureUtc(detectedAtUtc, nameof(detectedAtUtc));
        DueAtUtc = DataQualityValidation.EnsureUtc(dueAtUtc, nameof(dueAtUtc));
        Status = DataQualityExceptionStatus.Open;
        MarkCreated("system", detectedAtUtc);
    }

    public Guid CheckRunId { get; private set; }
    public Guid RuleId { get; private set; }
    public DataQualityRuleCode RuleCode { get; private set; }
    public DataQualitySeverity Severity { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public DataQualityExceptionStatus Status { get; private set; }
    public DateTime DetectedAtUtc { get; private set; }
    public DateTime DueAtUtc { get; private set; }
    public string? OwnerUserId { get; private set; }
    public DateTime? AssignedAtUtc { get; private set; }
    public string? ResolvedByUserId { get; private set; }
    public DateTime? ResolvedAtUtc { get; private set; }
    public string? ResolutionEvidenceReference { get; private set; }
    public string? ResolutionComment { get; private set; }

    public static DataQualityException Create(Guid checkRunId, Guid ruleId, DataQualityRuleCode ruleCode, DataQualitySeverity severity, string entityType, string entityId, string message, DateTime detectedAtUtc, DateTime dueAtUtc)
    {
        return new DataQualityException(checkRunId, ruleId, ruleCode, severity, entityType, entityId, message, detectedAtUtc, dueAtUtc);
    }

    public void Assign(string ownerUserId, DateTime assignedAtUtc)
    {
        OwnerUserId = DataQualityValidation.Required(ownerUserId, nameof(ownerUserId), 200);
        AssignedAtUtc = DataQualityValidation.EnsureUtc(assignedAtUtc, nameof(assignedAtUtc));
        Status = DataQualityExceptionStatus.Assigned;
    }

    public void Resolve(string resolvedByUserId, DateTime resolvedAtUtc, string evidenceReference, string? comment)
    {
        ResolvedByUserId = DataQualityValidation.Required(resolvedByUserId, nameof(resolvedByUserId), 200);
        ResolvedAtUtc = DataQualityValidation.EnsureUtc(resolvedAtUtc, nameof(resolvedAtUtc));
        ResolutionEvidenceReference = DataQualityValidation.Required(evidenceReference, nameof(evidenceReference), 500);
        ResolutionComment = DataQualityValidation.Optional(comment, 1000);
        Status = DataQualityExceptionStatus.Resolved;
    }
}

public sealed class ExceptionQueue : AuditableAggregateRoot
{
    private ExceptionQueue()
    {
    }

    private ExceptionQueue(Guid dataQualityExceptionId, DataQualitySeverity severity, DateTime queuedAtUtc)
    {
        DataQualityExceptionId = dataQualityExceptionId;
        Severity = severity;
        QueuedAtUtc = DataQualityValidation.EnsureUtc(queuedAtUtc, nameof(queuedAtUtc));
        Status = DataQualityExceptionStatus.Open;
        MarkCreated("system", queuedAtUtc);
    }

    public Guid DataQualityExceptionId { get; private set; }
    public DataQualitySeverity Severity { get; private set; }
    public DataQualityExceptionStatus Status { get; private set; }
    public DateTime QueuedAtUtc { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }

    public static ExceptionQueue Create(Guid dataQualityExceptionId, DataQualitySeverity severity, DateTime queuedAtUtc)
    {
        return new ExceptionQueue(dataQualityExceptionId, severity, queuedAtUtc);
    }

    public void MarkAssigned()
    {
        Status = DataQualityExceptionStatus.Assigned;
    }

    public void MarkResolved(DateTime closedAtUtc)
    {
        ClosedAtUtc = DataQualityValidation.EnsureUtc(closedAtUtc, nameof(closedAtUtc));
        Status = DataQualityExceptionStatus.Resolved;
    }
}

public sealed class ExceptionAssignment : Entity
{
    private ExceptionAssignment()
    {
    }

    private ExceptionAssignment(Guid dataQualityExceptionId, string ownerUserId, string assignedByUserId, DateTime assignedAtUtc)
    {
        DataQualityExceptionId = dataQualityExceptionId;
        OwnerUserId = DataQualityValidation.Required(ownerUserId, nameof(ownerUserId), 200);
        AssignedByUserId = DataQualityValidation.Required(assignedByUserId, nameof(assignedByUserId), 200);
        AssignedAtUtc = DataQualityValidation.EnsureUtc(assignedAtUtc, nameof(assignedAtUtc));
    }

    public Guid DataQualityExceptionId { get; private set; }
    public string OwnerUserId { get; private set; } = string.Empty;
    public string AssignedByUserId { get; private set; } = string.Empty;
    public DateTime AssignedAtUtc { get; private set; }

    public static ExceptionAssignment Create(Guid dataQualityExceptionId, string ownerUserId, string assignedByUserId, DateTime assignedAtUtc)
    {
        return new ExceptionAssignment(dataQualityExceptionId, ownerUserId, assignedByUserId, assignedAtUtc);
    }
}

public sealed class DataQualityDashboardSnapshot : AuditableAggregateRoot
{
    private DataQualityDashboardSnapshot()
    {
    }

    private DataQualityDashboardSnapshot(int openExceptions, int assignedExceptions, int overdueExceptions, int resolvedExceptions, DateTime generatedAtUtc)
    {
        OpenExceptions = openExceptions;
        AssignedExceptions = assignedExceptions;
        OverdueExceptions = overdueExceptions;
        ResolvedExceptions = resolvedExceptions;
        GeneratedAtUtc = DataQualityValidation.EnsureUtc(generatedAtUtc, nameof(generatedAtUtc));
        MarkCreated("system", generatedAtUtc);
    }

    public int OpenExceptions { get; private set; }
    public int AssignedExceptions { get; private set; }
    public int OverdueExceptions { get; private set; }
    public int ResolvedExceptions { get; private set; }
    public DateTime GeneratedAtUtc { get; private set; }

    public static DataQualityDashboardSnapshot Create(int openExceptions, int assignedExceptions, int overdueExceptions, int resolvedExceptions, DateTime generatedAtUtc)
    {
        return new DataQualityDashboardSnapshot(openExceptions, assignedExceptions, overdueExceptions, resolvedExceptions, generatedAtUtc);
    }
}
