namespace Cis.Application.Common.Security;

public static class RoleNames
{
    public const string SystemAdmin = "SystemAdmin";
    public const string SchemeAdministrator = "SchemeAdministrator";
    public const string InvestorOnboardingOfficer = "InvestorOnboardingOfficer";
    public const string RelationshipManager = "RelationshipManager";
    public const string FundOperationsOfficer = "FundOperationsOfficer";
    public const string TreasuryInvestmentOfficer = "TreasuryInvestmentOfficer";
    public const string ComplianceRiskOfficer = "ComplianceRiskOfficer";
    public const string FundAccountantFinanceOfficer = "FundAccountantFinanceOfficer";
    public const string CustodyReconciliationOfficer = "CustodyReconciliationOfficer";
    public const string InternalAuditor = "InternalAuditor";
    public const string ExternalAuditor = "ExternalAuditor";
    public const string ExecutiveManagement = "ExecutiveManagement";
    public const string BoardUser = "BoardUser";
    public const string InvestorPortalUser = "InvestorPortalUser";
    public const string RegulatorReadOnlyUser = "RegulatorReadOnlyUser";
    public const string TrusteeReadOnlyUser = "TrusteeReadOnlyUser";

    public static IReadOnlyCollection<string> All { get; } =
    [
        SystemAdmin,
        SchemeAdministrator,
        InvestorOnboardingOfficer,
        RelationshipManager,
        FundOperationsOfficer,
        TreasuryInvestmentOfficer,
        ComplianceRiskOfficer,
        FundAccountantFinanceOfficer,
        CustodyReconciliationOfficer,
        InternalAuditor,
        ExternalAuditor,
        ExecutiveManagement,
        BoardUser,
        InvestorPortalUser,
        RegulatorReadOnlyUser,
        TrusteeReadOnlyUser
    ];
}
