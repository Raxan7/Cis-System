namespace Cis.Domain.Cases;

public enum ServiceCaseType
{
    Complaint = 1,
    Inquiry = 2,
    ServiceRequest = 3
}

public enum ServiceCasePriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum ServiceCaseStatus
{
    Logged = 1,
    Assigned = 2,
    InProgress = 3,
    Escalated = 4,
    Resolved = 5,
    Closed = 6
}

public enum CaseActionType
{
    Note = 1,
    ContactedInvestor = 2,
    Investigation = 3,
    EvidenceAttached = 4,
    StatusUpdate = 5
}

public enum CaseEscalationStatus
{
    Open = 1,
    Resolved = 2
}
