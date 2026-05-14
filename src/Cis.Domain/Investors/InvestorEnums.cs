namespace Cis.Domain.Investors;

public enum InvestorType
{
    Individual = 1,
    Corporate = 2,
    Joint = 3,
    Group = 4
}

public enum InvestorStatus
{
    Draft = 1,
    PendingReview = 2,
    Approved = 3,
    Rejected = 4,
    Suspended = 5,
    Dormant = 6,
    Closed = 7
}

public enum KycRequirementStatus
{
    Missing = 1,
    Satisfied = 2,
    Expired = 3
}

public enum KycDocumentStatus
{
    Uploaded = 1,
    Expired = 2,
    Rejected = 3
}

public enum KycReviewStatus
{
    Submitted = 1,
    Approved = 2,
    Rejected = 3
}

public enum AmlScreeningStatus
{
    Cleared = 1,
    Exception = 2,
    Resolved = 3
}

public enum AmlHitRiskLevel
{
    Low = 1,
    Medium = 2,
    High = 3
}

public enum InvestorRiskCategory
{
    Low = 1,
    Medium = 2,
    High = 3
}

public enum RiskClassificationStatus
{
    PendingApproval = 1,
    Approved = 2,
    Rejected = 3
}

public enum DuplicateDetectionStatus
{
    Warning = 1,
    Reviewed = 2
}
