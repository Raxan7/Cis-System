namespace Cis.Domain.FeesTaxDistribution;

public enum FeeAccrualRunStatus
{
    Calculated = 1,
    Approved = 2
}

public enum FeeCalculationType
{
    Management = 1,
    Custody = 2,
    Trustee = 3,
    Admin = 4,
    Entry = 5,
    Exit = 6,
    Switch = 7,
    Performance = 8
}

public enum FeeWaiverStatus
{
    PendingApproval = 1,
    Approved = 2,
    Rejected = 3
}

public enum TaxRuleStatus
{
    Active = 1,
    Inactive = 2
}

public enum TaxCalculationType
{
    Vat = 1,
    WithholdingTax = 2,
    InvestorDistributionTax = 3
}

public enum DistributionDeclarationStatus
{
    Draft = 1,
    Approved = 2,
    Rejected = 3
}

public enum DistributionRunStatus
{
    Draft = 1,
    Published = 2
}

public enum DistributionMethod
{
    Cash = 1,
    Reinvest = 2
}

public enum ReinvestmentInstructionStatus
{
    PendingApproval = 1,
    Allocated = 2
}

public enum FeeDayCountBasis
{
    Actual365 = 1,
    Actual366 = 2,
    Actual360 = 3
}
