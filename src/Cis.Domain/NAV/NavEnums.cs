namespace Cis.Domain.NAV;

public enum ValuationRunStatus
{
    Draft = 1,
    Calculated = 2,
    Submitted = 3,
    Checked = 4,
    Approved = 5,
    Published = 6,
    Rejected = 7,
    Restated = 8
}

public enum ValuationInputType
{
    InvestmentValue = 1,
    CashAndBank = 2,
    AccruedIncome = 3,
    OtherReceivable = 4,
    Prepayment = 5,
    Liability = 6,
    AccruedExpense = 7,
    OpeningUnits = 8,
    UnitsIssued = 9,
    UnitsRedeemed = 10,
    ApprovedUnitAdjustment = 11,
    GrossContribution = 12,
    EntryFee = 13,
    SubscriptionTaxCharge = 14,
    RedemptionUnits = 15,
    RedemptionPrice = 16,
    ExitFee = 17,
    RedemptionTaxCharge = 18,
    UnitsSwitchedOut = 19,
    SourceFundPrice = 20,
    NetSwitchAmount = 21,
    TargetFundPrice = 22,
    AvailableUnits = 23,
    ApplicablePrice = 24,
    EstimatedExitCharge = 25,
    ApplicableTax = 26
}

public enum ValuationSourceType
{
    Portfolio = 1,
    Cash = 2,
    UnitRegister = 3,
    Accounting = 4,
    MarketPrice = 5,
    ManualOverride = 6,
    ExternalPriceProvider = 7
}

public enum DayCountBasis
{
    Actual365 = 1,
    Actual366 = 2,
    Actual360 = 3
}

public enum PriceExceptionStatus
{
    Open = 1,
    Resolved = 2
}

public enum ManualValuationOverrideStatus
{
    PendingApproval = 1,
    Approved = 2,
    Rejected = 3
}

public enum NavApprovalStepType
{
    Submitted = 1,
    Checked = 2,
    Approved = 3,
    Published = 4
}

public enum NavRestatementStatus
{
    Published = 1
}
