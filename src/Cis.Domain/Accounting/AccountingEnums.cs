namespace Cis.Domain.Accounting;

public enum AccountType
{
    Asset = 1,
    Liability = 2,
    Equity = 3,
    Income = 4,
    Expense = 5
}

public enum AccountStatus
{
    Active = 1,
    Inactive = 2
}

public enum LedgerStatus
{
    Active = 1,
    Closed = 2
}

public enum LedgerEntryDirection
{
    Debit = 1,
    Credit = 2
}

public enum JournalSource
{
    Manual = 1,
    Automated = 2,
    Correction = 3
}

public enum JournalStatus
{
    Draft = 1,
    Submitted = 2,
    Approved = 3,
    Posted = 4,
    Rejected = 5,
    Reversed = 6
}

public enum AutomatedJournalType
{
    Subscription = 1,
    Redemption = 2,
    InvestmentPlacement = 3,
    Maturity = 4,
    IncomeReceipt = 5,
    FeeAccrual = 6,
    Tax = 7,
    Distribution = 8,
    SuspenseResolution = 9,
    FairValueAdjustment = 10
}

public enum AccountingPeriodStatus
{
    Open = 1,
    Closed = 2,
    Locked = 3
}

public enum FinancialStatementType
{
    TrialBalance = 1,
    BalanceSheet = 2,
    IncomeStatement = 3
}

public enum SuspenseResolutionStatus
{
    Open = 1,
    Resolved = 2
}

public enum FairValueAdjustmentStatus
{
    Posted = 1,
    Reversed = 2
}

public enum AccountingNavReconciliationStatus
{
    Balanced = 1,
    Variance = 2,
    PendingPublishedNav = 3
}
