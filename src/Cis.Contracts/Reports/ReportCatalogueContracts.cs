using System.ComponentModel.DataAnnotations;
namespace Cis.Contracts.Reports;
public sealed class ReportQueryParameters
{
    public DateOnly? FromDate { get; init; }
    public DateOnly? ToDate { get; init; }
    public Guid? SchemeId { get; init; }
    public Guid? SchemeClassId { get; init; }
    public Guid? InvestorId { get; init; }
    [StringLength(100)]
    public string? Status { get; init; }
}
public sealed record ReportQueryResultDto(
    string Code,
    string Name,
    string Category,
    string RequiredPermission,
    ReportQueryParameters Parameters,
    DateTime GeneratedAtUtc,
    int RowCount,
    IReadOnlyCollection<ReportCatalogueRowDto> Rows);
public sealed record ReportCsvExportDto(
    string Code,
    string FileName,
    string ContentType,
    string Content,
    int RowCount,
    DateTime GeneratedAtUtc);
public record ReportCatalogueRowDto(
    string Code,
    string Name,
    string Category,
    DateOnly? FromDate,
    DateOnly? ToDate,
    string SourceEntity,
    int SourceRecordCount,
    int ExceptionCount,
    decimal AmountTotal,
    DateTime GeneratedAtUtc);
public static class ReportCatalogueCodes
{
    public static IReadOnlyCollection<string> All { get; } =
    [
        "INV-01",
        "INV-02",
        "INV-03",
        "INV-04",
        "INV-05",
        "INV-06",
        "INV-07",
        "INV-08",
        "INV-09",
        "INV-10",
        "INV-11",
        "INV-12",
        "INV-13",
        "INV-14",
        "INV-15",
        "INV-16",
        "SCH-01",
        "SCH-02",
        "SCH-03",
        "SCH-04",
        "SCH-05",
        "SCH-06",
        "SCH-07",
        "SCH-08",
        "SCH-09",
        "SCH-10",
        "SCH-11",
        "SCH-12",
        "SCH-13",
        "SCH-14",
        "CB-01",
        "CB-02",
        "CB-03",
        "CB-04",
        "CB-05",
        "CB-06",
        "CB-07",
        "PF-01",
        "PF-02",
        "PF-03",
        "PF-04",
        "PF-05",
        "PF-06",
        "PF-07",
        "PF-08",
        "PF-09",
        "PF-10",
        "PF-11",
        "PF-12",
        "NAV-01",
        "NAV-02",
        "NAV-03",
        "NAV-04",
        "NAV-05",
        "NAV-06",
        "NAV-07",
        "NAV-08",
        "NAV-09",
        "NAV-10",
        "NAV-11",
        "FIN-01",
        "FIN-02",
        "FIN-03",
        "FIN-04",
        "FIN-05",
        "FIN-06",
        "FIN-07",
        "FIN-08",
        "FIN-09",
        "FIN-10",
        "FIN-11",
        "FIN-12",
        "FIN-13",
        "CMP-01",
        "CMP-02",
        "CMP-03",
        "CMP-04",
        "CMP-05",
        "CMP-06",
        "CMP-07",
        "CMP-08",
        "CMP-09",
        "CMP-10",
        "CMP-11",
        "CUS-01",
        "CUS-02",
        "CUS-03",
        "CUS-04",
        "CUS-05",
        "CUS-06",
        "MGT-01",
        "MGT-02",
        "MGT-03",
        "MGT-04",
        "MGT-05",
        "MGT-06",
        "MGT-07",
        "REG-01",
        "REG-02",
        "REG-03",
        "REG-04",
        "REG-05",
        "REG-06",
        "REG-07",
        "REG-08",
        "DGT-01",
        "DGT-02",
        "DGT-03",

    ];
}public sealed record INV01InvestorMasterRegisterReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("INV-01", "Investor Master Register", "Investor", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record INV02KYCCompletenessReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("INV-02", "KYC Completeness Report", "Investor", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record INV03ExpiredKYCDocumentRenewalReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("INV-03", "Expired KYC / Document Renewal Report", "Investor", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record INV04AMLPEPSanctionsExceptionReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("INV-04", "AML / PEP / Sanctions Exception Report", "Investor", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record INV05InvestorBankDetailChangeLogReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("INV-05", "Investor Bank Detail Change Log", "Investor", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record INV06InvestorContactMandateChangeLogReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("INV-06", "Investor Contact & Mandate Change Log", "Investor", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record INV07AccountStatementReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("INV-07", "Account Statement", "Investor", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record INV08TransactionStatementReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("INV-08", "Transaction Statement", "Investor", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record INV09HoldingStatementReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("INV-09", "Holding Statement", "Investor", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record INV10SubscriptionConfirmationReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("INV-10", "Subscription Confirmation", "Investor", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record INV11RedemptionAdviceReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("INV-11", "Redemption Advice", "Investor", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record INV12SwitchTransferConfirmationReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("INV-12", "Switch / Transfer Confirmation", "Investor", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record INV13IncomeDistributionStatementReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("INV-13", "Income Distribution Statement", "Investor", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record INV14TaxCertificateReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("INV-14", "Tax Certificate", "Investor", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record INV15DormantInactiveInvestorReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("INV-15", "Dormant / Inactive Investor Report", "Investor", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record INV16InvestorComplaintsTurnaroundReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("INV-16", "Investor Complaints & Turnaround Report", "Investor", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record SCH01SchemeMasterRegisterReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("SCH-01", "Scheme Master Register", "SchemeDealing", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record SCH02SchemeFeeMatrixReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("SCH-02", "Scheme Fee Matrix Report", "SchemeDealing", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record SCH03DailySubscriptionRegisterReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("SCH-03", "Daily Subscription Register", "SchemeDealing", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record SCH04DailyRedemptionRegisterReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("SCH-04", "Daily Redemption Register", "SchemeDealing", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record SCH05PendingInstructionsReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("SCH-05", "Pending Instructions Report", "SchemeDealing", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record SCH06UnclearedFundsReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("SCH-06", "Uncleared Funds Report", "SchemeDealing", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record SCH07RejectedCancelledInstructionsReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("SCH-07", "Rejected / Cancelled Instructions Report", "SchemeDealing", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record SCH08LienRegisterReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("SCH-08", "Lien Register", "SchemeDealing", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record SCH09SwitchRegisterReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("SCH-09", "Switch Register", "SchemeDealing", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record SCH10TransferRegisterReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("SCH-10", "Transfer Register", "SchemeDealing", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record SCH11RecurringContributionPlanRegisterReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("SCH-11", "Recurring Contribution Plan Register", "SchemeDealing", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record SCH12RecurringContributionExceptionReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("SCH-12", "Recurring Contribution Exception Report", "SchemeDealing", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record SCH13CutOffBreachReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("SCH-13", "Cut-off Breach Report", "SchemeDealing", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record SCH14OutstandingUnprocessedInstructionAgeingReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("SCH-14", "Outstanding Unprocessed Instruction Ageing", "SchemeDealing", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record CB01BankCollectionSummaryReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("CB-01", "Bank Collection Summary", "Cash", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record CB02BankReconciliationReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("CB-02", "Bank Reconciliation Report", "Cash", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record CB03UnmatchedCashSuspenseReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("CB-03", "Unmatched Cash / Suspense Report", "Cash", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record CB04RedemptionPaymentStatusReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("CB-04", "Redemption Payment Status Report", "Cash", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record CB05ReturnedFailedPaymentReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("CB-05", "Returned / Failed Payment Report", "Cash", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record CB06CashPositionBySchemeReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("CB-06", "Cash Position by Scheme", "Cash", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record CB07CashForecastRequirementReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("CB-07", "Cash Forecast Requirement Report", "Cash", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record PF01PortfolioHoldingsBySchemeReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("PF-01", "Portfolio Holdings by Scheme", "Portfolio", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record PF02AssetAllocationReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("PF-02", "Asset Allocation Report", "Portfolio", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record PF03IssuerCounterpartyExposureReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("PF-03", "Issuer / Counterparty Exposure Report", "Portfolio", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record PF04MaturityLadderReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("PF-04", "Maturity Ladder Report", "Portfolio", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record PF05DepositPlacementRegisterReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("PF-05", "Deposit Placement Register", "Portfolio", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record PF06ApprovedInstrumentRegisterReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("PF-06", "Approved Instrument Register", "Portfolio", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record PF07InvestmentIncomeDueReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("PF-07", "Investment Income Due Report", "Portfolio", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record PF08InvestmentIncomeReceivedReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("PF-08", "Investment Income Received Report", "Portfolio", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record PF09InvestmentTransactionRegisterReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("PF-09", "Investment Transaction Register", "Portfolio", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record PF10PerformanceSummaryReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("PF-10", "Performance Summary Report", "Portfolio", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record PF11BenchmarkComparisonReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("PF-11", "Benchmark Comparison Report", "Portfolio", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record PF12RestrictedNonCompliantAssetReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("PF-12", "Restricted / Non-Compliant Asset Report", "Portfolio", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record NAV01DailyNAVSummaryReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("NAV-01", "Daily NAV Summary", "NAV", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record NAV02NAVMovementBridgeReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("NAV-02", "NAV Movement Bridge", "NAV", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record NAV03UnitPriceHistoryReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("NAV-03", "Unit Price History Report", "NAV", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record NAV04UnitsInIssueReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("NAV-04", "Units in Issue Report", "NAV", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record NAV05ValuationSourceReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("NAV-05", "Valuation Source Report", "NAV", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record NAV06StalePriceValuationExceptionReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("NAV-06", "Stale Price / Valuation Exception Report", "NAV", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record NAV07ManualOverrideLogReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("NAV-07", "Manual Override Log", "NAV", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record NAV08AccountingVsNAVReconciliationReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("NAV-08", "Accounting vs NAV Reconciliation Report", "NAV", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record NAV09HistoricalNAVReconstructionPackReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("NAV-09", "Historical NAV Reconstruction Pack", "NAV", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record NAV10RealizedUnrealizedGainLossReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("NAV-10", "Realized / Unrealized Gain-Loss Report", "NAV", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record NAV11NAVApprovalPublicationLogReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("NAV-11", "NAV Approval & Publication Log", "NAV", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record FIN01TrialBalanceReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("FIN-01", "Trial Balance", "Finance", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record FIN02GeneralLedgerReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("FIN-02", "General Ledger Report", "Finance", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record FIN03JournalRegisterReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("FIN-03", "Journal Register", "Finance", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record FIN04ChartOfAccountsReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("FIN-04", "Chart of Accounts Report", "Finance", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record FIN05ManagementFeeAccrualReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("FIN-05", "Management Fee Accrual Report", "Finance", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record FIN06CustodyTrusteeAdminFeeAccrualReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("FIN-06", "Custody / Trustee / Admin Fee Accrual Report", "Finance", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record FIN07FeeInvoiceRegisterReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("FIN-07", "Fee Invoice Register", "Finance", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record FIN08ExpenseAllocationReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("FIN-08", "Expense Allocation Report", "Finance", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record FIN09VATReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("FIN-09", "VAT Report", "Finance", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record FIN10WithholdingTaxReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("FIN-10", "Withholding Tax Report", "Finance", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record FIN11InvestorTaxDetailReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("FIN-11", "Investor Tax Detail Report", "Finance", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record FIN12IncomeDistributionComputationReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("FIN-12", "Income Distribution Computation Report", "Finance", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record FIN13FinancialStatementsPackReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("FIN-13", "Financial Statements Pack", "Finance", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record CMP01StatutoryLimitMonitoringReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("CMP-01", "Statutory Limit Monitoring Report", "ComplianceRisk", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record CMP02InternalPolicyLimitReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("CMP-02", "Internal Policy Limit Report", "ComplianceRisk", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record CMP03LiquidityCoverageReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("CMP-03", "Liquidity Coverage Report", "ComplianceRisk", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record CMP04RedemptionStressTestReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("CMP-04", "Redemption Stress Test Report", "ComplianceRisk", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record CMP05LiquidationTimeAnalysisReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("CMP-05", "Liquidation Time Analysis", "ComplianceRisk", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record CMP06BreachExceptionRegisterReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("CMP-06", "Breach & Exception Register", "ComplianceRisk", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record CMP07OverrideApprovalLogReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("CMP-07", "Override Approval Log", "ComplianceRisk", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record CMP08RelatedPartyExposureReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("CMP-08", "Related-Party Exposure Report", "ComplianceRisk", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record CMP09CounterpartyLimitUsageReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("CMP-09", "Counterparty Limit Usage Report", "ComplianceRisk", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record CMP10KYCAMLComplianceDashboardReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("CMP-10", "KYC / AML Compliance Dashboard", "ComplianceRisk", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record CMP11ShariahComplianceDashboardReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("CMP-11", "Shariah Compliance Dashboard", "ComplianceRisk", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record CUS01CustodianHoldingsReconciliationReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("CUS-01", "Custodian Holdings Reconciliation", "Custody", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record CUS02CustodianCashReconciliationReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("CUS-02", "Custodian Cash Reconciliation", "Custody", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record CUS03SettlementStatusAgedUnsettledReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("CUS-03", "Settlement Status / Aged Unsettled Report", "Custody", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record CUS04ReconciliationBreakRegisterReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("CUS-04", "Reconciliation Break Register", "Custody", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record CUS05AssetSafekeepingConfirmationReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("CUS-05", "Asset Safekeeping Confirmation Report", "Custody", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record CUS06IncomeReceiptReconciliationReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("CUS-06", "Income Receipt Reconciliation Report", "Custody", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record MGT01AUMDashboardReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("MGT-01", "AUM Dashboard", "ManagementRegulatory", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record MGT02AUMGrowthNetFlowsReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("MGT-02", "AUM Growth & Net Flows Report", "ManagementRegulatory", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record MGT03TopInvestorConcentrationReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("MGT-03", "Top Investor Concentration Report", "ManagementRegulatory", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record MGT04ChannelRelationshipManagerContributionReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("MGT-04", "Channel / Relationship Manager Contribution Report", "ManagementRegulatory", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record MGT05ProductSchemeProfitabilityReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("MGT-05", "Product / Scheme Profitability Report", "ManagementRegulatory", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record MGT06FeeRevenueAnalyticsReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("MGT-06", "Fee Revenue Analytics Report", "ManagementRegulatory", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record MGT07ManagementKPIDashboardReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("MGT-07", "Management KPI Dashboard", "ManagementRegulatory", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record REG01RegulatoryReturnPackReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("REG-01", "Regulatory Return Pack", "ManagementRegulatory", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record REG02ComplianceCalendarReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("REG-02", "Compliance Calendar Report", "ManagementRegulatory", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record REG03TrusteeCustodianReportingPackReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("REG-03", "Trustee / Custodian Reporting Pack", "ManagementRegulatory", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record REG04AuditTrailExtractReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("REG-04", "Audit Trail Extract", "ManagementRegulatory", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record REG05HistoricalNAVReconstructionPackReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("REG-05", "Historical NAV Reconstruction Pack", "ManagementRegulatory", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record REG06TransactionTraceabilityPackReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("REG-06", "Transaction Traceability Pack", "ManagementRegulatory", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record REG07ComplianceBreachHistoryReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("REG-07", "Compliance Breach History Report", "ManagementRegulatory", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record REG08BoardReportingPackReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("REG-08", "Board Reporting Pack", "ManagementRegulatory", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record DGT01StatementDeliveryStatusReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("DGT-01", "Statement Delivery Status Report", "Digital", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record DGT02PortalLoginUsageReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("DGT-02", "Portal Login & Usage Report", "Digital", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);

public sealed record DGT03NotificationExceptionReportReportDto(DateOnly? FromDate, DateOnly? ToDate, string SourceEntity, int SourceRecordCount, int ExceptionCount, decimal AmountTotal, DateTime GeneratedAtUtc)
    : ReportCatalogueRowDto("DGT-03", "Notification Exception Report", "Digital", FromDate, ToDate, SourceEntity, SourceRecordCount, ExceptionCount, AmountTotal, GeneratedAtUtc);


