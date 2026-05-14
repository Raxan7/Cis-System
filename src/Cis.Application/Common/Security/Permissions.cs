namespace Cis.Application.Common.Security;

public static class Permissions
{
    public const string All = "*";

    public static class Identity
    {
        public const string UsersRead = "Identity.Users.Read";
        public const string UsersCreate = "Identity.Users.Create";
        public const string UsersDeactivateRequest = "Identity.Users.Deactivate.Request";
        public const string UsersPasswordReset = "Identity.Users.PasswordReset";
        public const string UsersMfaManage = "Identity.Users.Mfa.Manage";
        public const string AccessChangeRequest = "Identity.AccessChange.Request";
        public const string AccessChangeApprove = "Identity.AccessChange.Approve";
        public const string RolesRead = "Identity.Roles.Read";
    }

    public static class Audit
    {
        public const string Read = "Audit.Read";
    }

    public static class Schemes
    {
        public const string Read = "Schemes.Read";
        public const string Create = "Schemes.Create";
        public const string Amend = "Schemes.Amend";
        public const string Submit = "Schemes.Submit";
        public const string Check = "Schemes.Check";
        public const string Approve = "Schemes.Approve";
    }

    public static class Investors
    {
        public const string Read = "Investors.Read";
        public const string Create = "Investors.Create";
        public const string Amend = "Investors.Amend";
        public const string SubmitKyc = "Investors.Kyc.Submit";
        public const string Approve = "Investors.Approve";
        public const string Reject = "Investors.Reject";
        public const string Suspend = "Investors.Suspend";
        public const string Close = "Investors.Close";
        public const string DocumentsUpload = "Investors.Documents.Upload";
        public const string AmlScreen = "Investors.Aml.Screen";
        public const string KycRead = "Investors.Kyc.Read";
        public const string AmlRead = "Investors.Aml.Read";
    }

    public static class Dealing
    {
        public const string Read = "Dealing.Read";
        public const string Create = "Dealing.Create";
        public const string Submit = "Dealing.Submit";
        public const string Approve = "Dealing.Approve";
        public const string Reject = "Dealing.Reject";
        public const string Cancel = "Dealing.Cancel";
        public const string LiensManage = "Dealing.Liens.Manage";
        public const string RecurringPlansManage = "Dealing.RecurringPlans.Manage";
    }

    public static class UnitRegister
    {
        public const string Read = "UnitRegister.Read";
        public const string AdjustmentsCreate = "UnitRegister.Adjustments.Create";
        public const string AdjustmentsApprove = "UnitRegister.Adjustments.Approve";
    }

    public static class Cash
    {
        public const string Read = "Cash.Read";
        public const string BankStatementsImport = "Cash.BankStatements.Import";
        public const string ReconciliationRunsCreate = "Cash.ReconciliationRuns.Create";
        public const string SuspenseResolve = "Cash.Suspense.Resolve";
        public const string PaymentsCreate = "Cash.Payments.Create";
        public const string PaymentsStatus = "Cash.Payments.Status";
        public const string ReversalsCreate = "Cash.Reversals.Create";
        public const string ReversalsApprove = "Cash.Reversals.Approve";
    }

    public static class Portfolio
    {
        public const string Read = "Portfolio.Read";
        public const string InstrumentsCreate = "Portfolio.Instruments.Create";
        public const string CounterpartiesCreate = "Portfolio.Counterparties.Create";
        public const string PlacementsCreate = "Portfolio.Placements.Create";
        public const string PlacementsSubmit = "Portfolio.Placements.Submit";
        public const string PlacementsApprove = "Portfolio.Placements.Approve";
        public const string IncomeReceiptsCreate = "Portfolio.IncomeReceipts.Create";
        public const string RolloversCreate = "Portfolio.Rollovers.Create";
    }

    public static class Nav
    {
        public const string Read = "NAV.Read";
        public const string ValuationRunsCreate = "NAV.ValuationRuns.Create";
        public const string ValuationRunsCalculate = "NAV.ValuationRuns.Calculate";
        public const string ValuationRunsSubmit = "NAV.ValuationRuns.Submit";
        public const string ValuationRunsCheck = "NAV.ValuationRuns.Check";
        public const string ValuationRunsApprove = "NAV.ValuationRuns.Approve";
        public const string ValuationRunsPublish = "NAV.ValuationRuns.Publish";
        public const string OverridesCreate = "NAV.Overrides.Create";
        public const string OverridesApprove = "NAV.Overrides.Approve";
        public const string RestatementsCreate = "NAV.Restatements.Create";
    }

    public static class Accounting
    {
        public const string Read = "Accounting.Read";
        public const string ChartOfAccountsCreate = "Accounting.ChartOfAccounts.Create";
        public const string JournalsManualCreate = "Accounting.Journals.Manual.Create";
        public const string JournalsAutomatedCreate = "Accounting.Journals.Automated.Create";
        public const string JournalsSubmit = "Accounting.Journals.Submit";
        public const string JournalsApprove = "Accounting.Journals.Approve";
        public const string PeriodsClose = "Accounting.Periods.Close";
        public const string NavReconciliationRead = "Accounting.NavReconciliation.Read";
    }

    public static class FeesTaxDistribution
    {
        public const string Read = "FeesTaxDistribution.Read";
        public const string AccrualRunsCreate = "FeesTaxDistribution.AccrualRuns.Create";
        public const string WaiversCreate = "FeesTaxDistribution.Waivers.Create";
        public const string WaiversApprove = "FeesTaxDistribution.Waivers.Approve";
        public const string TaxRulesCreate = "FeesTaxDistribution.TaxRules.Create";
        public const string DistributionsCreate = "FeesTaxDistribution.Distributions.Create";
        public const string DistributionsApprove = "FeesTaxDistribution.Distributions.Approve";
        public const string DistributionsPublish = "FeesTaxDistribution.Distributions.Publish";
    }

    public static class ComplianceRisk
    {
        public const string Read = "ComplianceRisk.Read";
        public const string LimitRunsCreate = "ComplianceRisk.LimitRuns.Create";
        public const string BreachesManage = "ComplianceRisk.Breaches.Manage";
        public const string BreachesClose = "ComplianceRisk.Breaches.Close";
        public const string LiquidityRunsCreate = "ComplianceRisk.LiquidityRuns.Create";
        public const string StressScenariosCreate = "ComplianceRisk.StressScenarios.Create";
        public const string StressRunsCreate = "ComplianceRisk.StressRuns.Create";
        public const string LiquidationAnalysisCreate = "ComplianceRisk.LiquidationAnalysis.Create";
    }

    public static class CustodyReconciliation
    {
        public const string Read = "CustodyReconciliation.Read";
        public const string CustodiansCreate = "CustodyReconciliation.Custodians.Create";
        public const string StatementsImport = "CustodyReconciliation.Statements.Import";
        public const string ReconciliationRunsCreate = "CustodyReconciliation.ReconciliationRuns.Create";
        public const string BreaksManage = "CustodyReconciliation.Breaks.Manage";
        public const string BreaksResolve = "CustodyReconciliation.Breaks.Resolve";
        public const string SafekeepingConfirmationsCreate = "CustodyReconciliation.SafekeepingConfirmations.Create";
    }

    public static class Portal
    {
        public const string Read = "Portal.Read";
        public const string RequestsCreate = "Portal.Requests.Create";
        public const string DocumentsUpload = "Portal.Documents.Upload";
    }

    public static class Reports
    {
        public const string Read = "Reports.Read";
        public const string SchedulesManage = "Reports.Schedules.Manage";
        public const string Run = "Reports.Run";
        public const string Approve = "Reports.Approve";
        public const string Publish = "Reports.Publish";
        public const string Download = "Reports.Download";
        public const string BundlesManage = "Reports.Bundles.Manage";
        public const string RegulatorPack = "Reports.RegulatorPack";
    }

    public static class Integrations
    {
        public const string Read = "Integrations.Read";
        public const string InboundUpload = "Integrations.Inbound.Upload";
        public const string NotificationsTest = "Integrations.Notifications.Test";
        public const string ErpExport = "Integrations.Erp.Export";
    }

    public static class Cases
    {
        public const string Read = "Cases.Read";
        public const string Create = "Cases.Create";
        public const string Assign = "Cases.Assign";
        public const string ActionsAdd = "Cases.Actions.Add";
        public const string Escalate = "Cases.Escalate";
        public const string Resolve = "Cases.Resolve";
    }

    public static class DataQuality
    {
        public const string Read = "DataQuality.Read";
        public const string RulesManage = "DataQuality.Rules.Manage";
        public const string CheckRunsCreate = "DataQuality.CheckRuns.Create";
        public const string ExceptionsManage = "DataQuality.Exceptions.Manage";
        public const string ExceptionsResolve = "DataQuality.Exceptions.Resolve";
    }

    public static class Operations
    {
        public const string Read = "Operations.Read";
        public const string HealthRead = "Operations.Health.Read";
        public const string DrTestsManage = "Operations.DrTests.Manage";
        public const string BackupRead = "Operations.Backup.Read";
    }

    public static class Workflow
    {
        public const string Read = "Workflow.Read";
        public const string Submit = "Workflow.Submit";
        public const string Check = "Workflow.Check";
        public const string Approve = "Workflow.Approve";
        public const string Reject = "Workflow.Reject";
    }

    public static class Archive
    {
        public const string Read = "Archive.Read";
    }

    public static IReadOnlyCollection<string> AllKnown { get; } =
    [
        All,
        Identity.UsersRead,
        Identity.UsersCreate,
        Identity.UsersDeactivateRequest,
        Identity.UsersPasswordReset,
        Identity.UsersMfaManage,
        Identity.AccessChangeRequest,
        Identity.AccessChangeApprove,
        Identity.RolesRead,
        Audit.Read,
        Schemes.Read,
        Schemes.Create,
        Schemes.Amend,
        Schemes.Submit,
        Schemes.Check,
        Schemes.Approve,
        Investors.Read,
        Investors.Create,
        Investors.Amend,
        Investors.SubmitKyc,
        Investors.Approve,
        Investors.Reject,
        Investors.Suspend,
        Investors.Close,
        Investors.DocumentsUpload,
        Investors.AmlScreen,
        Investors.KycRead,
        Investors.AmlRead,
        Dealing.Read,
        Dealing.Create,
        Dealing.Submit,
        Dealing.Approve,
        Dealing.Reject,
        Dealing.Cancel,
        Dealing.LiensManage,
        Dealing.RecurringPlansManage,
        UnitRegister.Read,
        UnitRegister.AdjustmentsCreate,
        UnitRegister.AdjustmentsApprove,
        Cash.Read,
        Cash.BankStatementsImport,
        Cash.ReconciliationRunsCreate,
        Cash.SuspenseResolve,
        Cash.PaymentsCreate,
        Cash.PaymentsStatus,
        Cash.ReversalsCreate,
        Cash.ReversalsApprove,
        Portfolio.Read,
        Portfolio.InstrumentsCreate,
        Portfolio.CounterpartiesCreate,
        Portfolio.PlacementsCreate,
        Portfolio.PlacementsSubmit,
        Portfolio.PlacementsApprove,
        Portfolio.IncomeReceiptsCreate,
        Portfolio.RolloversCreate,
        Nav.Read,
        Nav.ValuationRunsCreate,
        Nav.ValuationRunsCalculate,
        Nav.ValuationRunsSubmit,
        Nav.ValuationRunsCheck,
        Nav.ValuationRunsApprove,
        Nav.ValuationRunsPublish,
        Nav.OverridesCreate,
        Nav.OverridesApprove,
        Nav.RestatementsCreate,
        Accounting.Read,
        Accounting.ChartOfAccountsCreate,
        Accounting.JournalsManualCreate,
        Accounting.JournalsAutomatedCreate,
        Accounting.JournalsSubmit,
        Accounting.JournalsApprove,
        Accounting.PeriodsClose,
        Accounting.NavReconciliationRead,
        FeesTaxDistribution.Read,
        FeesTaxDistribution.AccrualRunsCreate,
        FeesTaxDistribution.WaiversCreate,
        FeesTaxDistribution.WaiversApprove,
        FeesTaxDistribution.TaxRulesCreate,
        FeesTaxDistribution.DistributionsCreate,
        FeesTaxDistribution.DistributionsApprove,
        FeesTaxDistribution.DistributionsPublish,
        ComplianceRisk.Read,
        ComplianceRisk.LimitRunsCreate,
        ComplianceRisk.BreachesManage,
        ComplianceRisk.BreachesClose,
        ComplianceRisk.LiquidityRunsCreate,
        ComplianceRisk.StressScenariosCreate,
        ComplianceRisk.StressRunsCreate,
        ComplianceRisk.LiquidationAnalysisCreate,
        CustodyReconciliation.Read,
        CustodyReconciliation.CustodiansCreate,
        CustodyReconciliation.StatementsImport,
        CustodyReconciliation.ReconciliationRunsCreate,
        CustodyReconciliation.BreaksManage,
        CustodyReconciliation.BreaksResolve,
        CustodyReconciliation.SafekeepingConfirmationsCreate,
        Portal.Read,
        Portal.RequestsCreate,
        Portal.DocumentsUpload,
        Reports.Read,
        Reports.SchedulesManage,
        Reports.Run,
        Reports.Approve,
        Reports.Publish,
        Reports.Download,
        Reports.BundlesManage,
        Reports.RegulatorPack,
        Integrations.Read,
        Integrations.InboundUpload,
        Integrations.NotificationsTest,
        Integrations.ErpExport,
        Cases.Read,
        Cases.Create,
        Cases.Assign,
        Cases.ActionsAdd,
        Cases.Escalate,
        Cases.Resolve,
        DataQuality.Read,
        DataQuality.RulesManage,
        DataQuality.CheckRunsCreate,
        DataQuality.ExceptionsManage,
        DataQuality.ExceptionsResolve,
        Operations.Read,
        Operations.HealthRead,
        Operations.DrTestsManage,
        Operations.BackupRead,
        Workflow.Read,
        Workflow.Submit,
        Workflow.Check,
        Workflow.Approve,
        Workflow.Reject,
        Archive.Read
    ];
}
