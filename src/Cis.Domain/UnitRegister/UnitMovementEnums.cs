namespace Cis.Domain.UnitRegister;

public enum UnitMovementType
{
    Issued = 1,
    Redeemed = 2,
    SwitchedIn = 3,
    SwitchedOut = 4,
    TransferredIn = 5,
    TransferredOut = 6,
    Liened = 7,
    LienReleased = 8,
    Adjustment = 9,
    Cancelled = 10
}

public enum UnitMovementSourceType
{
    DealingInstruction = 1,
    Lien = 2,
    Switch = 3,
    Transfer = 4,
    Adjustment = 5,
    ReinvestmentInstruction = 6
}

public enum UnitAdjustmentStatus
{
    PendingApproval = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4
}
