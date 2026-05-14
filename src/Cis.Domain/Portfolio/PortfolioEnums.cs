namespace Cis.Domain.Portfolio;

public enum InstrumentType
{
    Deposit = 1,
    CallAccount = 2,
    TreasuryBill = 3,
    TreasuryBond = 4,
    Bond = 5,
    Sukuk = 6,
    Repo = 7,
    OtherPermittedInvestment = 8,
    ListedMarketPricedInstrument = 9
}

public enum PlacementStatus
{
    Draft = 1,
    Submitted = 2,
    PendingApproval = 3,
    Approved = 4,
    Active = 5,
    Matured = 6,
    Liquidated = 7,
    Rejected = 8,
    Cancelled = 9
}

public enum InvestmentTransactionType
{
    Acquisition = 1,
    Coupon = 2,
    Dividend = 3,
    MaturityProceeds = 4,
    Rollover = 5,
    AccruedIncome = 6,
    ProfitShare = 7,
    Disposal = 8,
    Reversal = 9
}

public enum SettlementStatus
{
    Pending = 1,
    Settled = 2,
    Failed = 3
}

public enum MaturityEventType
{
    Final = 1,
    Interim = 2,
    Call = 3,
    Put = 4
}

public enum RolloverEventType
{
    Automatic = 1,
    ManualRequest = 2,
    MaturityRollover = 3
}

public enum MandateValidationStatus
{
    Compliant = 1,
    TenorBreached = 2,
    CounterpartyLimitBreached = 3,
    IssuerLimitBreached = 4,
    InstrumentNotApproved = 5,
    AssetClassLimitBreached = 6,
    MultipleBreaches = 7
}

public enum CounterpartyExposureStatus
{
    Compliant = 1,
    Warning = 2,
    Breach = 3
}
