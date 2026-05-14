using Cis.Domain.Common;

namespace Cis.Domain.Cases;

public sealed class ServiceCase : AuditableAggregateRoot
{
    private readonly List<CaseAction> _actions = [];
    private readonly List<CaseEscalation> _escalations = [];
    private readonly List<CaseStatusHistory> _statusHistory = [];

    private ServiceCase()
    {
    }

    private ServiceCase(
        string caseNumber,
        Guid? investorId,
        ServiceCaseType caseType,
        string category,
        string subject,
        string description,
        ServiceCasePriority priority,
        string loggedByUserId,
        DateTime loggedAtUtc,
        BusinessDate businessDate,
        DateTime slaTargetAtUtc)
    {
        CaseNumber = CaseValidation.Required(caseNumber, nameof(caseNumber), 40);
        InvestorId = investorId;
        CaseType = caseType;
        Category = CaseValidation.Required(category, nameof(category), 100);
        Subject = CaseValidation.Required(subject, nameof(subject), 200);
        Description = CaseValidation.Required(description, nameof(description), 4000);
        Priority = priority;
        Status = ServiceCaseStatus.Logged;
        LoggedByUserId = CaseValidation.Required(loggedByUserId, nameof(loggedByUserId), 200);
        LoggedAtUtc = loggedAtUtc;
        BusinessDate = businessDate;
        SlaTargetAtUtc = slaTargetAtUtc;
        CaseValidation.EnsureUtc(loggedAtUtc, nameof(loggedAtUtc));
        CaseValidation.EnsureUtc(slaTargetAtUtc, nameof(slaTargetAtUtc));
        AddStatusHistory(null, Status, loggedByUserId, loggedAtUtc, "Case logged.");
    }

    public string CaseNumber { get; private set; } = string.Empty;

    public Guid? InvestorId { get; private set; }

    public ServiceCaseType CaseType { get; private set; }

    public string Category { get; private set; } = string.Empty;

    public string Subject { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public ServiceCasePriority Priority { get; private set; }

    public ServiceCaseStatus Status { get; private set; }

    public string? OwnerUserId { get; private set; }

    public string LoggedByUserId { get; private set; } = string.Empty;

    public DateTime LoggedAtUtc { get; private set; }

    public BusinessDate BusinessDate { get; private set; } = BusinessDate.From(DateOnly.FromDateTime(DateTime.UnixEpoch));

    public DateTime SlaTargetAtUtc { get; private set; }

    public string? ResolvedByUserId { get; private set; }

    public DateTime? ResolvedAtUtc { get; private set; }

    public string? ResolutionSummary { get; private set; }

    public string? ResolutionEvidenceReference { get; private set; }

    public IReadOnlyCollection<CaseAction> Actions => _actions.AsReadOnly();

    public IReadOnlyCollection<CaseEscalation> Escalations => _escalations.AsReadOnly();

    public IReadOnlyCollection<CaseStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

    public static ServiceCase Create(
        string caseNumber,
        Guid? investorId,
        ServiceCaseType caseType,
        string category,
        string subject,
        string description,
        ServiceCasePriority priority,
        string loggedByUserId,
        DateTime loggedAtUtc,
        BusinessDate businessDate,
        DateTime slaTargetAtUtc)
    {
        return new ServiceCase(caseNumber, investorId, caseType, category, subject, description, priority, loggedByUserId, loggedAtUtc, businessDate, slaTargetAtUtc);
    }

    public void Assign(string ownerUserId, string actorUserId, DateTime assignedAtUtc)
    {
        CaseValidation.EnsureUtc(assignedAtUtc, nameof(assignedAtUtc));
        OwnerUserId = CaseValidation.Required(ownerUserId, nameof(ownerUserId), 200);
        ChangeStatus(Status is ServiceCaseStatus.Logged ? ServiceCaseStatus.Assigned : Status, actorUserId, assignedAtUtc, $"Assigned to {OwnerUserId}.");
    }

    public CaseAction AddAction(CaseActionType actionType, string summary, string? evidenceReference, string actorUserId, DateTime actionedAtUtc)
    {
        var action = CaseAction.Create(Id, actionType, summary, evidenceReference, actorUserId, actionedAtUtc);
        _actions.Add(action);
        return action;
    }

    public CaseEscalation Escalate(string reason, string escalatedToRole, string? escalatedToUserId, string actorUserId, DateTime escalatedAtUtc)
    {
        CaseValidation.EnsureUtc(escalatedAtUtc, nameof(escalatedAtUtc));
        if (Status is ServiceCaseStatus.Resolved or ServiceCaseStatus.Closed)
        {
            throw new InvalidOperationException("Resolved or closed cases cannot be escalated.");
        }

        var escalation = CaseEscalation.Create(Id, reason, escalatedToRole, escalatedToUserId, actorUserId, escalatedAtUtc);
        _escalations.Add(escalation);
        ChangeStatus(ServiceCaseStatus.Escalated, actorUserId, escalatedAtUtc, reason);
        return escalation;
    }

    public void Resolve(string resolutionSummary, string resolutionEvidenceReference, string actorUserId, DateTime resolvedAtUtc)
    {
        CaseValidation.EnsureUtc(resolvedAtUtc, nameof(resolvedAtUtc));
        if (string.IsNullOrWhiteSpace(OwnerUserId))
        {
            throw new InvalidOperationException("Case must have an owner before resolution.");
        }

        ResolutionSummary = CaseValidation.Required(resolutionSummary, nameof(resolutionSummary), 2000);
        ResolutionEvidenceReference = CaseValidation.Required(resolutionEvidenceReference, nameof(resolutionEvidenceReference), 1000);
        ResolvedByUserId = CaseValidation.Required(actorUserId, nameof(actorUserId), 200);
        ResolvedAtUtc = resolvedAtUtc;

        foreach (var escalation in _escalations.Where(escalation => escalation.Status == CaseEscalationStatus.Open))
        {
            escalation.MarkResolved(actorUserId, resolvedAtUtc);
        }

        ChangeStatus(ServiceCaseStatus.Resolved, actorUserId, resolvedAtUtc, "Case resolved.");
    }

    public int AgingDays(DateTime asOfUtc)
    {
        CaseValidation.EnsureUtc(asOfUtc, nameof(asOfUtc));
        var end = ResolvedAtUtc ?? asOfUtc;
        return Math.Max(0, (int)Math.Floor((end - LoggedAtUtc).TotalDays));
    }

    public bool IsSlaBreached(DateTime asOfUtc)
    {
        CaseValidation.EnsureUtc(asOfUtc, nameof(asOfUtc));
        return Status is not (ServiceCaseStatus.Resolved or ServiceCaseStatus.Closed) && asOfUtc > SlaTargetAtUtc;
    }

    private void ChangeStatus(ServiceCaseStatus newStatus, string actorUserId, DateTime changedAtUtc, string reason)
    {
        if (Status == newStatus)
        {
            return;
        }

        var previousStatus = Status;
        Status = newStatus;
        AddStatusHistory(previousStatus, newStatus, actorUserId, changedAtUtc, reason);
    }

    private void AddStatusHistory(ServiceCaseStatus? fromStatus, ServiceCaseStatus toStatus, string actorUserId, DateTime changedAtUtc, string reason)
    {
        _statusHistory.Add(CaseStatusHistory.Create(Id, fromStatus, toStatus, actorUserId, changedAtUtc, reason));
    }
}

public sealed class Complaint : AuditableAggregateRoot
{
    private Complaint()
    {
    }

    private Complaint(Guid serviceCaseId, string complaintReference, string source, DateTime receivedAtUtc, bool isRegulatory, string? regulatoryCategory)
    {
        ServiceCaseId = serviceCaseId;
        ComplaintReference = CaseValidation.Required(complaintReference, nameof(complaintReference), 50);
        Source = CaseValidation.Required(source, nameof(source), 80);
        ReceivedAtUtc = receivedAtUtc;
        IsRegulatory = isRegulatory;
        RegulatoryCategory = CaseValidation.Optional(regulatoryCategory, 120);
        CaseValidation.EnsureUtc(receivedAtUtc, nameof(receivedAtUtc));
    }

    public Guid ServiceCaseId { get; private set; }

    public string ComplaintReference { get; private set; } = string.Empty;

    public string Source { get; private set; } = string.Empty;

    public DateTime ReceivedAtUtc { get; private set; }

    public bool IsRegulatory { get; private set; }

    public string? RegulatoryCategory { get; private set; }

    public int? TurnaroundDays { get; private set; }

    public static Complaint Create(Guid serviceCaseId, string complaintReference, string source, DateTime receivedAtUtc, bool isRegulatory, string? regulatoryCategory)
    {
        return new Complaint(serviceCaseId, complaintReference, source, receivedAtUtc, isRegulatory, regulatoryCategory);
    }

    public void MarkResolved(DateTime resolvedAtUtc)
    {
        CaseValidation.EnsureUtc(resolvedAtUtc, nameof(resolvedAtUtc));
        TurnaroundDays = Math.Max(0, (int)Math.Floor((resolvedAtUtc - ReceivedAtUtc).TotalDays));
    }
}

public sealed class CaseAction : Entity
{
    private CaseAction()
    {
    }

    private CaseAction(Guid serviceCaseId, CaseActionType actionType, string summary, string? evidenceReference, string actionedByUserId, DateTime actionedAtUtc)
    {
        ServiceCaseId = serviceCaseId;
        ActionType = actionType;
        Summary = CaseValidation.Required(summary, nameof(summary), 2000);
        EvidenceReference = CaseValidation.Optional(evidenceReference, 1000);
        ActionedByUserId = CaseValidation.Required(actionedByUserId, nameof(actionedByUserId), 200);
        ActionedAtUtc = actionedAtUtc;
        CaseValidation.EnsureUtc(actionedAtUtc, nameof(actionedAtUtc));
    }

    public Guid ServiceCaseId { get; private set; }

    public CaseActionType ActionType { get; private set; }

    public string Summary { get; private set; } = string.Empty;

    public string? EvidenceReference { get; private set; }

    public string ActionedByUserId { get; private set; } = string.Empty;

    public DateTime ActionedAtUtc { get; private set; }

    public static CaseAction Create(Guid serviceCaseId, CaseActionType actionType, string summary, string? evidenceReference, string actionedByUserId, DateTime actionedAtUtc)
    {
        return new CaseAction(serviceCaseId, actionType, summary, evidenceReference, actionedByUserId, actionedAtUtc);
    }
}

public sealed class CaseEscalation : Entity
{
    private CaseEscalation()
    {
    }

    private CaseEscalation(Guid serviceCaseId, string reason, string escalatedToRole, string? escalatedToUserId, string escalatedByUserId, DateTime escalatedAtUtc)
    {
        ServiceCaseId = serviceCaseId;
        Reason = CaseValidation.Required(reason, nameof(reason), 2000);
        EscalatedToRole = CaseValidation.Required(escalatedToRole, nameof(escalatedToRole), 120);
        EscalatedToUserId = CaseValidation.Optional(escalatedToUserId, 200);
        EscalatedByUserId = CaseValidation.Required(escalatedByUserId, nameof(escalatedByUserId), 200);
        EscalatedAtUtc = escalatedAtUtc;
        Status = CaseEscalationStatus.Open;
        CaseValidation.EnsureUtc(escalatedAtUtc, nameof(escalatedAtUtc));
    }

    public Guid ServiceCaseId { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public string EscalatedToRole { get; private set; } = string.Empty;

    public string? EscalatedToUserId { get; private set; }

    public string EscalatedByUserId { get; private set; } = string.Empty;

    public DateTime EscalatedAtUtc { get; private set; }

    public CaseEscalationStatus Status { get; private set; }

    public string? ResolvedByUserId { get; private set; }

    public DateTime? ResolvedAtUtc { get; private set; }

    public static CaseEscalation Create(Guid serviceCaseId, string reason, string escalatedToRole, string? escalatedToUserId, string escalatedByUserId, DateTime escalatedAtUtc)
    {
        return new CaseEscalation(serviceCaseId, reason, escalatedToRole, escalatedToUserId, escalatedByUserId, escalatedAtUtc);
    }

    public void MarkResolved(string actorUserId, DateTime resolvedAtUtc)
    {
        CaseValidation.EnsureUtc(resolvedAtUtc, nameof(resolvedAtUtc));
        Status = CaseEscalationStatus.Resolved;
        ResolvedByUserId = CaseValidation.Required(actorUserId, nameof(actorUserId), 200);
        ResolvedAtUtc = resolvedAtUtc;
    }
}

public sealed class CaseSlaPolicy : AuditableAggregateRoot
{
    private CaseSlaPolicy()
    {
    }

    private CaseSlaPolicy(string category, ServiceCasePriority priority, int targetHours, bool isActive)
    {
        Category = CaseValidation.Required(category, nameof(category), 100);
        Priority = priority;
        TargetHours = targetHours > 0 ? targetHours : throw new ArgumentOutOfRangeException(nameof(targetHours), "SLA target hours must be positive.");
        IsActive = isActive;
    }

    public string Category { get; private set; } = string.Empty;

    public ServiceCasePriority Priority { get; private set; }

    public int TargetHours { get; private set; }

    public bool IsActive { get; private set; }

    public static CaseSlaPolicy Create(string category, ServiceCasePriority priority, int targetHours, bool isActive = true)
    {
        return new CaseSlaPolicy(category, priority, targetHours, isActive);
    }
}

public sealed class CaseStatusHistory : Entity
{
    private CaseStatusHistory()
    {
    }

    private CaseStatusHistory(Guid serviceCaseId, ServiceCaseStatus? fromStatus, ServiceCaseStatus toStatus, string changedByUserId, DateTime changedAtUtc, string reason)
    {
        ServiceCaseId = serviceCaseId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        ChangedByUserId = CaseValidation.Required(changedByUserId, nameof(changedByUserId), 200);
        ChangedAtUtc = changedAtUtc;
        Reason = CaseValidation.Required(reason, nameof(reason), 2000);
        CaseValidation.EnsureUtc(changedAtUtc, nameof(changedAtUtc));
    }

    public Guid ServiceCaseId { get; private set; }

    public ServiceCaseStatus? FromStatus { get; private set; }

    public ServiceCaseStatus ToStatus { get; private set; }

    public string ChangedByUserId { get; private set; } = string.Empty;

    public DateTime ChangedAtUtc { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public static CaseStatusHistory Create(Guid serviceCaseId, ServiceCaseStatus? fromStatus, ServiceCaseStatus toStatus, string changedByUserId, DateTime changedAtUtc, string reason)
    {
        return new CaseStatusHistory(serviceCaseId, fromStatus, toStatus, changedByUserId, changedAtUtc, reason);
    }
}
