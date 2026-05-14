namespace Cis.Domain.DataQuality;

public enum DataQualityRuleCode
{
    MissingRequiredInvestorFields = 1,
    ExpiredKycDocuments = 2,
    DuplicateInvestorIdentifiers = 3,
    UnmatchedBankReceipts = 4,
    NegativeHoldings = 5,
    UnapprovedNavUsedForDealing = 6,
    StalePriceInputs = 7,
    UnbalancedJournal = 8,
    UnresolvedReconciliationBreakOlderThanThreshold = 9,
    ReportPublicationWithoutApproval = 10,
    ConfigurationEffectiveDateOverlap = 11
}

public enum DataQualitySeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum DataQualityRuleStatus
{
    Active = 1,
    Inactive = 2
}

public enum DataQualityRunStatus
{
    Running = 1,
    Completed = 2,
    Failed = 3
}

public enum DataQualityExceptionStatus
{
    Open = 1,
    Assigned = 2,
    Resolved = 3
}
