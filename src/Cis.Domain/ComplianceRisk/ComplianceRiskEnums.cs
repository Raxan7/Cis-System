namespace Cis.Domain.ComplianceRisk;

public enum LimitType
{
    Counterparty = 1,
    Issuer = 2,
    Tenor = 3,
    AssetClass = 4,
    Class = 5,
    Scheme = 6,
    RelatedParty = 7,
    Liquidity = 8
}

public enum LimitRuleScope
{
    Statutory = 1,
    InternalPolicy = 2
}

public enum LimitCheckRunStatus
{
    Completed = 1
}

public enum BreachSeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum BreachStatus
{
    Open = 1,
    Assigned = 2,
    RemediationSubmitted = 3,
    PendingClosureApproval = 4,
    Closed = 5
}

public enum RemediationActionStatus
{
    Submitted = 1,
    Approved = 2
}

public enum StressScenarioStatus
{
    Active = 1,
    Inactive = 2
}
