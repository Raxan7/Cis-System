namespace Cis.Domain.Dealing;

public enum DealingInstructionType
{
    Subscription = 1,
    Redemption = 2,
    Switch = 3,
    Transfer = 4
}

public enum DealingInstructionStatus
{
    Draft = 1,
    Submitted = 2,
    PendingFunds = 3,
    CashVerified = 4,
    PendingReview = 5,
    PendingApproval = 6,
    Approved = 7,
    Rejected = 8,
    Cancelled = 9,
    Priced = 10,
    Allocated = 11,
    Settled = 12,
    Failed = 13,
    Reversed = 14
}

public enum DealingChannel
{
    Branch = 1,
    Operations = 2,
    FileUpload = 3,
    API = 4,
    InvestorPortal = 5
}

public enum DealingInstructionMode
{
    Amount = 1,
    Units = 2
}

public enum ValidationSeverity
{
    Info = 1,
    Warning = 2,
    Error = 3
}

public enum LienStatus
{
    Placed = 1,
    Released = 2
}

public enum RecurringPlanFrequency
{
    Weekly = 1,
    Monthly = 2,
    Quarterly = 3
}

public enum RecurringPlanStatus
{
    Active = 1,
    Paused = 2,
    Cancelled = 3
}

public enum DealingBatchStatus
{
    Open = 1,
    Closed = 2,
    Failed = 3
}
