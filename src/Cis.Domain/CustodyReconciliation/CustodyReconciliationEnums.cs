namespace Cis.Domain.CustodyReconciliation;

public enum CustodianStatus
{
    Active = 1,
    Suspended = 2
}

public enum CustodianStatementType
{
    Holdings = 1,
    Cash = 2
}

public enum CustodianStatementImportStatus
{
    Imported = 1,
    Reconciled = 2
}

public enum CustodyReconciliationRunStatus
{
    Completed = 1
}

public enum ReconciliationBreakStatus
{
    Open = 1,
    Assigned = 2,
    Resolved = 3
}

public enum ReconciliationBreakSeverity
{
    Low = 1,
    Medium = 2,
    High = 3
}

public enum ReconciliationBreakType
{
    MissingInternal = 1,
    MissingCustodian = 2,
    QuantityMismatch = 3,
    MarketValueMismatch = 4,
    CashAmountMismatch = 5,
    SettlementUnconfirmed = 6
}

public enum SafekeepingConfirmationStatus
{
    Issued = 1
}
