# Report Catalogue

The report catalogue is registered in the Reports module and exposed through `/api/reports/definitions`, `/api/reports/{code}/query`, and `/api/reports/{code}/export/csv`. CSV is implemented now; PDF, Excel, Word, and HTML remain behind the export abstraction for later renderers.

| Code | Report | Category | Frequency | Primary Users | Required Permission | Source Entity |
| --- | --- | --- | --- | --- | --- | --- |
| INV-01 | Investor Master Register | Investor | Monthly | Investor onboarding officers; Relationship managers; Compliance officers | Reports.Read | Investors |
| INV-02 | KYC Completeness Report | Investor | Daily | Investor onboarding officers; Compliance officers | Reports.Read | KycRequirements |
| INV-03 | Expired KYC / Document Renewal Report | Investor | Daily | Investor onboarding officers; Compliance officers | Reports.Read | KycDocuments |
| INV-04 | AML / PEP / Sanctions Exception Report | Investor | Daily | Compliance officers; Internal auditors | Reports.Read | AmlScreeningHits |
| INV-05 | Investor Bank Detail Change Log | Investor | Daily | Operations officers; Internal auditors | Reports.Read | InvestorBankAccounts |
| INV-06 | Investor Contact & Mandate Change Log | Investor | Daily | Operations officers; Relationship managers | Reports.Read | InvestorMandates |
| INV-07 | Account Statement | Investor | Monthly | Relationship managers; Investor portal users | Reports.Read | InvestorPositions |
| INV-08 | Transaction Statement | Investor | Monthly | Relationship managers; Investor portal users | Reports.Read | DealingInstructions |
| INV-09 | Holding Statement | Investor | Monthly | Relationship managers; Investor portal users | Reports.Read | UnitHoldings |
| INV-10 | Subscription Confirmation | Investor | Daily | Fund operations officers; Relationship managers | Reports.Read | SubscriptionInstructions |
| INV-11 | Redemption Advice | Investor | Daily | Fund operations officers; Relationship managers | Reports.Read | RedemptionInstructions |
| INV-12 | Switch / Transfer Confirmation | Investor | Daily | Fund operations officers; Relationship managers | Reports.Read | SwitchTransferInstructions |
| INV-13 | Income Distribution Statement | Investor | Quarterly | Fund accountants; Relationship managers | Reports.Read | InvestorDistributions |
| INV-14 | Tax Certificate | Investor | Annually | Fund accountants; Investor portal users | Reports.Read | TaxCalculations |
| INV-15 | Dormant / Inactive Investor Report | Investor | Monthly | Compliance officers; Relationship managers | Reports.Read | Investors |
| INV-16 | Investor Complaints & Turnaround Report | Investor | Monthly | Compliance officers; Executive management | Reports.Read | Complaints |
| SCH-01 | Scheme Master Register | SchemeDealing | Monthly | Scheme administrators; Fund operations officers | Reports.Read | Schemes |
| SCH-02 | Scheme Fee Matrix Report | SchemeDealing | Monthly | Scheme administrators; Fund accountants | Reports.Read | FeeSchedules |
| SCH-03 | Daily Subscription Register | SchemeDealing | Daily | Fund operations officers | Reports.Read | SubscriptionInstructions |
| SCH-04 | Daily Redemption Register | SchemeDealing | Daily | Fund operations officers | Reports.Read | RedemptionInstructions |
| SCH-05 | Pending Instructions Report | SchemeDealing | Daily | Fund operations officers | Reports.Read | DealingInstructions |
| SCH-06 | Uncleared Funds Report | SchemeDealing | Daily | Fund operations officers; Finance officers | Reports.Read | SuspenseItems |
| SCH-07 | Rejected / Cancelled Instructions Report | SchemeDealing | Daily | Fund operations officers; Internal auditors | Reports.Read | DealingInstructions |
| SCH-08 | Lien Register | SchemeDealing | Daily | Fund operations officers; Compliance officers | Reports.Read | Liens |
| SCH-09 | Switch Register | SchemeDealing | Daily | Fund operations officers | Reports.Read | SwitchInstructions |
| SCH-10 | Transfer Register | SchemeDealing | Daily | Fund operations officers | Reports.Read | TransferInstructions |
| SCH-11 | Recurring Contribution Plan Register | SchemeDealing | Monthly | Fund operations officers; Relationship managers | Reports.Read | RecurringContributionPlans |
| SCH-12 | Recurring Contribution Exception Report | SchemeDealing | Daily | Fund operations officers | Reports.Read | RecurringContributionPlans |
| SCH-13 | Cut-off Breach Report | SchemeDealing | Daily | Fund operations officers; Compliance officers | Reports.Read | CutOffBreaches |
| SCH-14 | Outstanding Unprocessed Instruction Ageing | SchemeDealing | Daily | Fund operations officers; Executive management | Reports.Read | DealingInstructions |
| CB-01 | Bank Collection Summary | Cash | Daily | Treasury officers; Finance officers | Reports.Read | BankStatementLines |
| CB-02 | Bank Reconciliation Report | Cash | Daily | Treasury officers; Finance officers | Reports.Read | ReconciliationRuns |
| CB-03 | Unmatched Cash / Suspense Report | Cash | Daily | Treasury officers; Finance officers | Reports.Read | SuspenseItems |
| CB-04 | Redemption Payment Status Report | Cash | Daily | Treasury officers; Fund operations officers | Reports.Read | PaymentInstructions |
| CB-05 | Returned / Failed Payment Report | Cash | Daily | Treasury officers; Finance officers | Reports.Read | ReturnedFunds |
| CB-06 | Cash Position by Scheme | Cash | Daily | Treasury officers; Fund accountants | Reports.Read | CashBookEntries |
| CB-07 | Cash Forecast Requirement Report | Cash | Daily | Treasury officers; Executive management | Reports.Read | PaymentInstructions |
| PF-01 | Portfolio Holdings by Scheme | Portfolio | Daily | Treasury investment officers; Fund accountants | Reports.Read | PortfolioHoldings |
| PF-02 | Asset Allocation Report | Portfolio | Daily | Treasury investment officers; Executive management | Reports.Read | PortfolioHoldings |
| PF-03 | Issuer / Counterparty Exposure Report | Portfolio | Daily | Treasury investment officers; Compliance officers | Reports.Read | CounterpartyExposures |
| PF-04 | Maturity Ladder Report | Portfolio | Weekly | Treasury investment officers | Reports.Read | Placements |
| PF-05 | Deposit Placement Register | Portfolio | Daily | Treasury investment officers | Reports.Read | Placements |
| PF-06 | Approved Instrument Register | Portfolio | Monthly | Treasury investment officers; Compliance officers | Reports.Read | ApprovedInstrumentRules |
| PF-07 | Investment Income Due Report | Portfolio | Daily | Treasury investment officers; Fund accountants | Reports.Read | IncomeSchedules |
| PF-08 | Investment Income Received Report | Portfolio | Daily | Treasury investment officers; Fund accountants | Reports.Read | InvestmentTransactions |
| PF-09 | Investment Transaction Register | Portfolio | Daily | Treasury investment officers; Internal auditors | Reports.Read | InvestmentTransactions |
| PF-10 | Performance Summary Report | Portfolio | Monthly | Treasury investment officers; Executive management | Reports.Read | PortfolioHoldings |
| PF-11 | Benchmark Comparison Report | Portfolio | Monthly | Treasury investment officers; Executive management | Reports.Read | PortfolioHoldings |
| PF-12 | Restricted / Non-Compliant Asset Report | Portfolio | Daily | Compliance officers; Treasury investment officers | Reports.Read | MandateValidationResults |
| NAV-01 | Daily NAV Summary | NAV | Daily | Fund accountants; Executive management | Reports.Read | NavPublications |
| NAV-02 | NAV Movement Bridge | NAV | Daily | Fund accountants | Reports.Read | NavCalculations |
| NAV-03 | Unit Price History Report | NAV | Daily | Fund accountants; Relationship managers | Reports.Read | NavPerUnits |
| NAV-04 | Units in Issue Report | NAV | Daily | Fund accountants; Unit register officers | Reports.Read | UnitHoldings |
| NAV-05 | Valuation Source Report | NAV | Daily | Fund accountants; Internal auditors | Reports.Read | ValuationSources |
| NAV-06 | Stale Price / Valuation Exception Report | NAV | Daily | Fund accountants; Compliance officers | Reports.Read | ValuationExceptions |
| NAV-07 | Manual Override Log | NAV | Daily | Fund accountants; Internal auditors | Reports.Read | ManualValuationOverrides |
| NAV-08 | Accounting vs NAV Reconciliation Report | NAV | Daily | Fund accountants; Finance officers | Reports.Read | AccountingNavReconciliations |
| NAV-09 | Historical NAV Reconstruction Pack | NAV | AdHoc | Fund accountants; Internal auditors | Reports.Read | NavVersionArchives |
| NAV-10 | Realized / Unrealized Gain-Loss Report | NAV | Monthly | Fund accountants; Treasury investment officers | Reports.Read | InstrumentValuations |
| NAV-11 | NAV Approval & Publication Log | NAV | Daily | Fund accountants; Internal auditors | Reports.Read | NavApprovals |
| FIN-01 | Trial Balance | Finance | Monthly | Finance officers; Internal auditors | Reports.Read | TrialBalances |
| FIN-02 | General Ledger Report | Finance | Monthly | Finance officers; Internal auditors | Reports.Read | LedgerEntries |
| FIN-03 | Journal Register | Finance | Daily | Finance officers; Internal auditors | Reports.Read | Journals |
| FIN-04 | Chart of Accounts Report | Finance | Monthly | Finance officers | Reports.Read | Accounts |
| FIN-05 | Management Fee Accrual Report | Finance | Daily | Fund accountants; Finance officers | Reports.Read | FeeCalculations |
| FIN-06 | Custody / Trustee / Admin Fee Accrual Report | Finance | Daily | Fund accountants; Finance officers | Reports.Read | FeeAccrualRuns |
| FIN-07 | Fee Invoice Register | Finance | Monthly | Finance officers | Reports.Read | FeeAccrualRuns |
| FIN-08 | Expense Allocation Report | Finance | Monthly | Finance officers | Reports.Read | JournalLines |
| FIN-09 | VAT Report | Finance | Monthly | Finance officers | Reports.Read | VatCalculations |
| FIN-10 | Withholding Tax Report | Finance | Monthly | Finance officers | Reports.Read | WithholdingTaxCalculations |
| FIN-11 | Investor Tax Detail Report | Finance | Monthly | Finance officers | Reports.Read | TaxCalculations |
| FIN-12 | Income Distribution Computation Report | Finance | Quarterly | Fund accountants; Finance officers | Reports.Read | DistributionRuns |
| FIN-13 | Financial Statements Pack | Finance | Quarterly | Finance officers; Board users | Reports.Read | FinancialStatements |
| CMP-01 | Statutory Limit Monitoring Report | ComplianceRisk | Daily | Compliance officers; Executive management | Reports.Read | StatutoryLimits |
| CMP-02 | Internal Policy Limit Report | ComplianceRisk | Daily | Compliance officers; Executive management | Reports.Read | InternalPolicyLimits |
| CMP-03 | Liquidity Coverage Report | ComplianceRisk | Daily | Compliance officers; Treasury officers | Reports.Read | LiquidityCoverageRuns |
| CMP-04 | Redemption Stress Test Report | ComplianceRisk | Weekly | Compliance officers; Executive management | Reports.Read | RedemptionStressTestRuns |
| CMP-05 | Liquidation Time Analysis | ComplianceRisk | Weekly | Compliance officers; Treasury officers | Reports.Read | LiquidationTimeAnalysisRuns |
| CMP-06 | Breach & Exception Register | ComplianceRisk | Daily | Compliance officers; Internal auditors | Reports.Read | LimitBreaches |
| CMP-07 | Override Approval Log | ComplianceRisk | Daily | Compliance officers; Internal auditors | Reports.Read | WorkflowActions |
| CMP-08 | Related-Party Exposure Report | ComplianceRisk | Daily | Compliance officers; Executive management | Reports.Read | RelatedPartyExposures |
| CMP-09 | Counterparty Limit Usage Report | ComplianceRisk | Daily | Compliance officers; Treasury officers | Reports.Read | CounterpartyLimitUsages |
| CMP-10 | KYC / AML Compliance Dashboard | ComplianceRisk | Daily | Compliance officers; Executive management | Reports.Read | RiskDashboardSnapshots |
| CMP-11 | Shariah Compliance Dashboard | ComplianceRisk | Monthly | Compliance officers; Board users | Reports.Read | RiskDashboardSnapshots |
| CUS-01 | Custodian Holdings Reconciliation | Custody | Daily | Custody reconciliation officers | Reports.Read | CustodianHoldingLines |
| CUS-02 | Custodian Cash Reconciliation | Custody | Daily | Custody reconciliation officers | Reports.Read | CustodianCashLines |
| CUS-03 | Settlement Status / Aged Unsettled Report | Custody | Daily | Custody reconciliation officers | Reports.Read | CustodyReconciliationRuns |
| CUS-04 | Reconciliation Break Register | Custody | Daily | Custody reconciliation officers; Internal auditors | Reports.Read | ReconciliationBreaks |
| CUS-05 | Asset Safekeeping Confirmation Report | Custody | Monthly | Custody reconciliation officers; Trustees | Reports.Read | SafekeepingConfirmations |
| CUS-06 | Income Receipt Reconciliation Report | Custody | Daily | Custody reconciliation officers; Fund accountants | Reports.Read | IncomeSchedules |
| MGT-01 | AUM Dashboard | ManagementRegulatory | Daily | Executive management; Board users | Reports.Read | RiskDashboardSnapshots |
| MGT-02 | AUM Growth & Net Flows Report | ManagementRegulatory | Monthly | Executive management | Reports.Read | NavPublications |
| MGT-03 | Top Investor Concentration Report | ManagementRegulatory | Monthly | Executive management; Compliance officers | Reports.Read | InvestorPositions |
| MGT-04 | Channel / Relationship Manager Contribution Report | ManagementRegulatory | Monthly | Executive management; Relationship managers | Reports.Read | DealingInstructions |
| MGT-05 | Product / Scheme Profitability Report | ManagementRegulatory | Monthly | Executive management; Finance officers | Reports.Read | FinancialStatements |
| MGT-06 | Fee Revenue Analytics Report | ManagementRegulatory | Monthly | Executive management; Finance officers | Reports.Read | FeeCalculations |
| MGT-07 | Management KPI Dashboard | ManagementRegulatory | Monthly | Executive management; Board users | Reports.Read | RiskDashboardSnapshots |
| REG-01 | Regulatory Return Pack | ManagementRegulatory | Monthly | Compliance officers; Regulators | Reports.RegulatorPack | ReportRuns |
| REG-02 | Compliance Calendar Report | ManagementRegulatory | Monthly | Compliance officers | Reports.RegulatorPack | WorkflowInstances |
| REG-03 | Trustee / Custodian Reporting Pack | ManagementRegulatory | Monthly | Compliance officers; Trustees | Reports.RegulatorPack | SafekeepingConfirmations |
| REG-04 | Audit Trail Extract | ManagementRegulatory | AdHoc | Internal auditors; External auditors; Regulators | Reports.RegulatorPack | AuditLogs |
| REG-05 | Historical NAV Reconstruction Pack | ManagementRegulatory | AdHoc | Compliance officers; Regulators | Reports.RegulatorPack | NavVersionArchives |
| REG-06 | Transaction Traceability Pack | ManagementRegulatory | AdHoc | Compliance officers; Regulators | Reports.RegulatorPack | DealingInstructions |
| REG-07 | Compliance Breach History Report | ManagementRegulatory | Monthly | Compliance officers; Regulators | Reports.RegulatorPack | LimitBreaches |
| REG-08 | Board Reporting Pack | ManagementRegulatory | Quarterly | Executive management; Board users | Reports.RegulatorPack | ReportBundles |
| DGT-01 | Statement Delivery Status Report | Digital | Daily | Portal administrators; Operations officers | Reports.Read | PortalDocumentDownloads |
| DGT-02 | Portal Login & Usage Report | Digital | Daily | Portal administrators; Compliance officers | Reports.Read | PortalSessions |
| DGT-03 | Notification Exception Report | Digital | Daily | Portal administrators; Operations officers | Reports.Read | IntegrationErrors |

