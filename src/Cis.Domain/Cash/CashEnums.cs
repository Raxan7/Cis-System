namespace Cis.Domain.Cash;

public enum BankStatementLineDirection
{
    Credit = 1,
    Debit = 2
}

public enum BankStatementMatchStatus
{
    Unmatched = 1,
    Matched = 2,
    Suspense = 3
}

public enum BankStatementImportStatus
{
    Imported = 1,
    PartiallyMatched = 2,
    Matched = 3
}

public enum CashBookEntryDirection
{
    Inbound = 1,
    Outbound = 2
}

public enum CashBookEntrySourceType
{
    BankStatement = 1,
    PaymentInstruction = 2,
    Reversal = 3,
    Manual = 4
}

public enum CashBookEntryStatus
{
    Posted = 1,
    Reversed = 2
}

public enum CashMatchRule
{
    ReferenceAmountDateInvestorSchemeBankAccount = 1,
    ReferenceAmountDateInvestor = 2,
    ReferenceAmountDate = 3,
    Manual = 4
}

public enum SuspenseItemStatus
{
    Open = 1,
    Resolved = 2
}

public enum PaymentInstructionType
{
    Redemption = 1,
    Distribution = 2
}

public enum PaymentInstructionStatus
{
    Draft = 1,
    Sent = 2,
    Completed = 3,
    Failed = 4,
    Returned = 5,
    Reversed = 6
}

public enum PaymentStatusEventType
{
    Sent = 1,
    Completed = 2,
    Failed = 3,
    Returned = 4,
    Reversed = 5
}

public enum ReturnedFundStatus
{
    Open = 1,
    Investigating = 2,
    Resolved = 3
}

public enum ReversalRequestStatus
{
    PendingApproval = 1,
    Approved = 2,
    Rejected = 3
}

public enum ReconciliationRunStatus
{
    Running = 1,
    Completed = 2,
    Failed = 3
}