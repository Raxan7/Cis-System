# Final SRS Coverage Audit

Audit date: 2026-05-17  
Repository: `C:\Users\WINDOWS 11\Documents\Projects\CIS-SYSTEM`

## Scope and audit rule

This audit classifies a requirement as `Complete` only when the repository shows:

1. domain model
2. database persistence
3. API or background-job surface
4. authorization where relevant
5. audit trail
6. tests
7. updated traceability

Status legend:

- `Complete` = all audit rule items present
- `Partial` = implemented in part, but one or more audit rule items or requirement-specific semantics are incomplete
- `Open` = explicitly not finished
- `Baseline gap` = the numbered requirement id is not defined in the current repository traceability baseline, so implementation cannot be honestly claimed against that id

## 1. Functional requirement coverage table (FR-001 to FR-083)

| ID | Status | Module | Evidence summary | Recommendation if not complete |
| --- | --- | --- | --- | --- |
| FR-001 | Complete | Schemes | `Scheme`, EF config/migration, `/api/schemes`, workflow endpoints, permission checks, audits, `SchemesModuleTests` | - |
| FR-002 | Complete | Schemes | `SchemeClass` persisted with dealing/valuation fields, API, authorization, audits, tests | - |
| FR-003 | Complete | Schemes | `FeeSchedule`/`FeeRule`, overlap validation, persistence, API, audits, tests | - |
| FR-004 | Complete | Schemes | Eligibility, approved instruments, risk/liquidity entities, API, authorization, audit, tests | - |
| FR-005 | Complete | Schemes | Bank accounts, custodian mappings, version history, API, audits, tests | - |
| FR-006 | Complete | Investors | Investor aggregate + profiles, persistence, APIs, auth, audit, `InvestorsKycAmlDocumentsTests`, `PortalModuleTests` | - |
| FR-007 | Complete | Investors/KYC | `KycDocument`, `KycRequirement`, expiry checks, APIs, auth, audit, tests | - |
| FR-008 | Complete | Investors | Duplicate detection entity/service/tests exist; surfaced during onboarding | - |
| FR-009 | Complete | Investors/AML | AML case/hit model, manual provider stub, API, auth, audit, tests | - |
| FR-010 | Complete | Investors | Risk classification persisted and enforced during approval path, audited, tested | - |
| FR-011 | Complete | Investors | Bank/contact/mandate change logs persisted, audited, tested | - |
| FR-012 | Complete | Dealing | Dealing instruction hierarchy, channels/modes, APIs, auth, audit, tests | - |
| FR-013 | Complete | Dealing | Investor/scheme/cut-off/minimum validations, tests, audit trail | - |
| FR-014 | Complete | Dealing | `PendingFunds` subscription path, persistence, API workflow, tests | - |
| FR-015 | Complete | Dealing/NAV | Allocation requires cleared funds + approved NAV, confirmation output, tests | - |
| FR-016 | Complete | Dealing | Redemption entity fields/status history persisted and tested | - |
| FR-017 | Complete | Dealing/UnitRegister | Lien, lock-in, minimum balance, notice period validations implemented and tested | - |
| FR-018 | Complete | Dealing | Exit fee/tax/net payout decimal calculations persisted and tested | - |
| FR-019 | Complete | Dealing/Workflow | Approval-threshold workflow present with SoD and tests | - |
| FR-020 | Complete | Dealing/Cash | Payout authorization and redemption advice path implemented and audited | - |
| FR-021 | Complete | Dealing | Switch capture with source/target scheme/class persisted and tested | - |
| FR-022 | Complete | Dealing | Transfer instructions with approved source/target investors, tested | - |
| FR-023 | Complete | Dealing | Fee/cut-off/valuation controls recorded for switch/transfer flows, tested | - |
| FR-024 | Complete | Dealing | Ownership history JSON persisted for switches/transfers, tested | - |
| FR-025 | Complete | Dealing | Lien placement/release with evidence/documentation and audit trail, tested | - |
| FR-026 | Complete | Dealing | Lien affects redeemable balance calculations, tested | - |
| FR-027 | Complete | Dealing | Recurring plan aggregate, persistence, APIs, auth, audit, tests | - |
| FR-028 | Complete | Dealing | Missed/failed/pause/resume/cancel/amend lifecycle implemented and tested | - |
| FR-029 | Complete | Dealing | Pending and cut-off breach query endpoints protected and tested | - |
| FR-030 | Complete | UnitRegister | Append-only ledger, source validation, API, audit, tests | - |
| FR-031 | Complete | UnitRegister | Derived holdings/positions persisted and reconciled in tests | - |
| FR-032 | Complete | UnitRegister | Historical reconstruction by date/reference implemented and tested | - |
| FR-033 | Complete | UnitRegister | Adjustment approval flow + idempotency + audit + tests | - |
| FR-034 | Complete | Cash | Bank statement import contracts, persistence, idempotency/source-hash replay protection, authorization, audit, and lifecycle/performance tests now map explicitly in traceability | - |
| FR-035 | Complete | Cash | Unmatched cash creates suspense records with controlled resolution paths, linked references, audit trail, and lifecycle test coverage | - |
| FR-036 | Complete | Cash | Reconciliation runs persist matched/suspense/break/aging summaries with API, authorization, audit, and lifecycle evidence | - |
| FR-037 | Complete | Cash | Payment instruction and payment-status lifecycle contracts, persistence, authorization, audit logging, and lifecycle evidence exist | - |
| FR-038 | Complete | Cash | Reversal request/approval workflow preserves original payment history, authorization, audit trail, and lifecycle evidence | - |
| FR-039 | Complete | Portfolio | Instrument and counterparty reference data are persisted with unique constraints, protected APIs, audits, and `PortfolioModuleTests` | - |
| FR-040 | Complete | Portfolio | Placement create/submit/approve/settle workflow with mandate validation, authorization, audits, and tests is implemented | - |
| FR-041 | Complete | Portfolio | Holdings plus maturity-ladder and income-due views are persisted/exposed with protected APIs and `PortfolioModuleTests` | - |
| FR-042 | Complete | Portfolio | Income receipt and rollover processing preserve downstream transaction/exposure source data and are covered by `PortfolioModuleTests` | - |
| FR-043 | Complete | NAV | Valuation run/input/source model + persistence + APIs + auth + audit + tests | - |
| FR-044 | Complete | NAV | Instrument valuation, stale/variance exceptions, pricing policy, tests | - |
| FR-045 | Complete | NAV | GAV/NAV/unit price calculations stored and tested | - |
| FR-046 | Complete | NAV/Workflow | Submit/check/approve/publish flow with SoD, auth, audit, tests | - |
| FR-047 | Complete | NAV | Manual override approval path exists and is audited/tested | - |
| FR-048 | Complete | NAV/Archive | Immutable publication and restatement flow enforced/tested | - |
| FR-049 | Complete | NAV | History and reconstruction endpoints exist and are tested | - |
| FR-050 | Complete | Accounting | Chart/accounts/ledgers/periods persisted with API/auth/audit/tests | - |
| FR-051 | Complete | Accounting | Balanced manual/automated/correction journals with workflow and tests | - |
| FR-052 | Complete | Accounting | GL, journal register, trial balance, period-close snapshots, tests | - |
| FR-053 | Complete | Accounting/NAV | NAV reconciliation + suspense/fair value records persisted/tested | - |
| FR-054 | Complete | FeesTaxDistribution | Fee accrual runs, calculations, waivers, APIs, auth, audit, tests | - |
| FR-055 | Complete | FeesTaxDistribution | Tax rules and VAT/WHT/investor tax persistence implemented/tested | - |
| FR-056 | Complete | FeesTaxDistribution | Distribution declarations/runs/approval/coverage/reinvestment implemented/tested | - |
| FR-057 | Complete | FeesTaxDistribution | Investor-level distribution/tax records and publication flow tested | - |
| FR-058 | Complete | ComplianceRisk | Limits and limit check engine persisted with APIs/auth/audit/tests | - |
| FR-059 | Complete | ComplianceRisk | Limit usage and breaches created automatically and tested | - |
| FR-060 | Complete | ComplianceRisk/Workflow | Breach assignment/remediation/closure approval implemented/tested | - |
| FR-061 | Complete | ComplianceRisk | Liquidity coverage runs persisted with APIs/audit/tests | - |
| FR-062 | Complete | ComplianceRisk | Stress scenarios/tests and liquidation analysis implemented/tested | - |
| FR-063 | Complete | ComplianceRisk | Related-party exposure/counterparty usage/dashboard persisted/tested | - |
| FR-064 | Complete | CustodyReconciliation | Custodian master/account model + API/auth/audit/tests | - |
| FR-065 | Complete | CustodyReconciliation | Holdings import with idempotency and persisted source lines, tested | - |
| FR-066 | Complete | CustodyReconciliation | Cash import with idempotency + settlement data, tested | - |
| FR-067 | Complete | CustodyReconciliation | Matching engine generates breaks from holdings/cash mismatches, tested | - |
| FR-068 | Complete | CustodyReconciliation | Break assignment/aging/history/evidence preserved and tested | - |
| FR-069 | Complete | CustodyReconciliation | Safekeeping confirmations persisted and tested | - |
| FR-070 | Complete | Portal | Portal profile/holdings/transactions/statements/tax/notices/activity endpoints exist with auth/audit/tests | - |
| FR-071 | Complete | Portal/Identity | Self-only data scoping + MFA enforcement implemented and tested | - |
| FR-072 | Complete | Portal/Workflow | Digital request workflows exist; official records not directly changed; tested | - |
| FR-073 | Complete | Portal/Documents | Portal document upload requests and download logging implemented/tested | - |
| FR-074 | Complete | Portal/Audit | Portal session/activity logs persisted and tested | - |
| FR-075 | Complete | Reports foundation | Registry, owner matrix, required permissions, persistence, API, auth, audit, tests | - |
| FR-076 | Complete | Reports foundation | Schedules/runs/parameters/outputs/status persisted and tested | - |
| FR-077 | Complete | Reports foundation | Approval-before-publication evidence and audit trail implemented/tested | - |
| FR-078 | Complete | Reports/Archive | Immutable published report archive and distribution records implemented/tested | - |
| FR-079 | Complete | Reports foundation | Bundle creation/publication controls implemented/tested | - |
| FR-080 | Complete | Audit | AuditLog model, persistence, query APIs, auth, append-only guard, tests | - |
| FR-081 | Complete | Workflow | Generic workflow/approval policy/actions/steps infra with tests | - |
| FR-082 | Complete | Archive | Immutable archive + retention model/service/tests | - |
| FR-083 | Complete | Audit/Workflow/Archive | Audit/workflow/archive APIs protected and tested | - |

## 2. Non-functional requirement coverage table (NF-001 to NF-012)

| ID | Status | Evidence summary | Recommendation if not complete |
| --- | --- | --- | --- |
| NF-001 | Complete | JWT auth, fallback authenticated policy, permission checks, CORS allow-list, HTTPS/HSTS, rate limiting, tests, traceability, security controls doc | - |
| NF-002 | Complete | Security-sensitive identity audit logs implemented and tested; traceability updated | - |
| NF-003 | Complete | Consistent ProblemDetails responses for validation, authorization, concurrency, and unexpected failures are implemented, tested, and traceable | - |
| NF-004 | Complete | SoD service/workflow blocking + tests + traceability | - |
| NF-005 | Complete | Database constraints, unique keys, foreign keys, indexes, and concurrency tokens are traceable across EF configurations/migrations and exercised by integration tests | - |
| NF-006 | Complete | UTC timestamp discipline and separate business-date modeling are implemented and tested | - |
| NF-007 | Complete | Idempotency-key replay control is implemented for adjustment and ingestion endpoints with test evidence | - |
| NF-008 | Complete | Pagination, read-side filtering/sorting, caching, and performance-readiness evidence exist and are traceable | - |
| NF-009 | Complete | Health, backup/restore, migration runner, DR persistence, and operational runbooks are implemented and tested | - |
| NF-010 | Complete | File metadata validation and malware-scan hook are implemented and tested for document/KYC intake paths | - |
| NF-011 | Partial | Background queue execution exists with tests and operational visibility, but persisted Hangfire/Quartz durability is still missing | Replace `src/Cis.Infrastructure/Operations/BackgroundJobDispatcher.cs` with a durable scheduler implementation and expose durable job semantics end to end |
| NF-012 | Complete | Append-only audit / immutable archive enforcement in `CisDbContext`, tests, traceability, security controls doc | - |

## 3. Report catalogue implementation table

Audit note: the report foundation is strong, but the catalogue implementation is not fully requirements-true yet. Every code is registered, permission-gated, reachable through `/api/reports/{code}/query` and `/api/reports/{code}/export/csv`, and exercised by the loop test in `tests/Cis.Tests.Integration/Reports/ReportsModuleTests.cs`. However, the implementation uses a generic registry handler in `src/Cis.Infrastructure/Reports/ReportQueryHandlers.cs` and generic summary-row DTOs in `src/Cis.Contracts/Reports/ReportCatalogueContracts.cs`, rather than report-specific query projections returning report-shaped data from seeded scenarios. For that reason the catalogue families below are `Partial`, not `Complete`.

| Family | Codes covered | Status | Current implementation evidence | Exact recommendation |
| --- | --- | --- | --- | --- |
| INV | `INV-01` to `INV-16` | Partial | Codes registered in `src/Cis.Infrastructure/Reports/ReportCatalogue.cs`; generic DTO records in `src/Cis.Contracts/Reports/ReportCatalogueContracts.cs`; generic query/export handler in `src/Cis.Infrastructure/Reports/ReportQueryHandlers.cs`; exercised in `tests/Cis.Tests.Integration/Reports/ReportsModuleTests.cs` | Replace generic summary rows with investor-report DTOs and per-report projections in `src/Cis.Infrastructure/Reports/ReportQueryHandlers.cs`; add seeded correctness tests in `tests/Cis.Tests.Integration/Reports/ReportsModuleTests.cs` or split per-family test files |
| SCH | `SCH-01` to `SCH-14` | Partial | Same as above | Same as above, with scheme/dealing specific projections |
| CB | `CB-01` to `CB-07` | Partial | Same as above | Implement cash-report-specific DTOs/projections from `BankStatementLines`, `SuspenseItems`, `PaymentInstructions`, `CashBookEntries`, `ReconciliationRuns` |
| PF | `PF-01` to `PF-12` | Partial | Same as above | Implement portfolio-report-specific DTOs/projections from `PortfolioHoldings`, `Placements`, `InvestmentTransactions`, `MandateValidationResults`, `CounterpartyExposures` |
| NAV | `NAV-01` to `NAV-11` | Partial | Same as above | Implement NAV-report-specific DTOs/projections from `NavPublications`, `NavCalculations`, `ValuationSources`, `InstrumentValuations`, `NavVersionArchives` |
| FIN | `FIN-01` to `FIN-13` | Partial | Same as above | Implement finance-report-specific DTOs/projections from `TrialBalances`, `LedgerEntries`, `Journals`, `FeeCalculations`, `TaxCalculations`, `FinancialStatements` |
| CMP | `CMP-01` to `CMP-11` | Partial | Same as above | Implement compliance-report-specific DTOs/projections from `LimitBreaches`, `LiquidityCoverageRuns`, `RedemptionStressTestRuns`, `RelatedPartyExposures`, `RiskDashboardSnapshots` |
| CUS | `CUS-01` to `CUS-06` | Partial | Same as above | Implement custody-report-specific DTOs/projections from `CustodianHoldingLines`, `CustodianCashLines`, `CustodyReconciliationRuns`, `SafekeepingConfirmations` |
| MGT | `MGT-01` to `MGT-07` | Partial | Same as above | Implement management KPI/AUM/concentration/profitability report projections |
| REG | `REG-01` to `REG-08` | Partial | Codes are registered and permission-protected with `Reports.RegulatorPack`; bundle and publication workflows are tested | Build regulator-ready pack composition instead of generic summary rows in `src/Cis.Infrastructure/Reports/ReportQueryHandlers.cs`; add seeded regulator-pack content tests |
| DGT | `DGT-01` to `DGT-03` | Partial | Codes registered; source entities exist; generic query/export path and loop tests exist | Implement digital-report-specific DTOs/projections for delivery status, portal usage, and notification exceptions |

## 4. Formula implementation table (D-FML-001 to D-FML-042)

| Formula ID | Status | Implementation evidence | Test evidence |
| --- | --- | --- | --- |
| D-FML-001 | Complete | `src/Cis.Domain/NAV/NavFormulaEngine.cs` (`GrossAssetValue`) | `tests/Cis.Tests.Unit/NAV/NavFormulaEngineTests.cs` |
| D-FML-002 | Complete | `NavFormulaEngine` (`InvestmentValue`) | `NavFormulaEngineTests.cs` |
| D-FML-003 | Complete | `NavFormulaEngine` (`AccruedIncome`) | `NavFormulaEngineTests.cs` |
| D-FML-004 | Complete | `NavFormulaEngine` (`DailyAccretion`) | `NavFormulaEngineTests.cs` |
| D-FML-005 | Complete | `NavFormulaEngine` (`NetAssetValue`) | `NavFormulaEngineTests.cs` |
| D-FML-006 | Complete | `NavFormulaEngine` (`ClosingUnits`) | `NavFormulaEngineTests.cs` |
| D-FML-007 | Complete | `NavFormulaEngine` (`UnitPrice`) | `NavFormulaEngineTests.cs` |
| D-FML-008 | Complete | `NavFormulaEngine` (`NetSubscriptionAmount`) | `NavFormulaEngineTests.cs` |
| D-FML-009 | Complete | `NavFormulaEngine` (`UnitsAllocated`) | `NavFormulaEngineTests.cs` |
| D-FML-010 | Complete | `NavFormulaEngine` (`GrossRedemptionValue`) | `NavFormulaEngineTests.cs` |
| D-FML-011 | Complete | `NavFormulaEngine` (`NetRedemptionPayable`) | `NavFormulaEngineTests.cs` |
| D-FML-012 | Complete | `NavFormulaEngine` (`SwitchOutValue`, `SwitchInUnits`) | `NavFormulaEngineTests.cs` |
| D-FML-013 | Complete | `NavFormulaEngine` (`RedeemableAmount`) | `NavFormulaEngineTests.cs` |
| D-FML-014 | Complete | `src/Cis.Domain/FeesTaxDistribution/FeesTaxDistributionFormulaEngine.cs` (`DailyManagementFee`) | `tests/Cis.Tests.Unit/FeesTaxDistribution/FeesTaxDistributionFormulaEngineTests.cs` |
| D-FML-015 | Complete | `FeesTaxDistributionFormulaEngine` (`PeriodManagementFeeFromDailyAccruals`, `PeriodManagementFeeFromAverageNav`) | `FeesTaxDistributionFormulaEngineTests.cs` |
| D-FML-016 | Complete | `FeesTaxDistributionFormulaEngine` (`DailyCustodyFee`) | `FeesTaxDistributionFormulaEngineTests.cs` |
| D-FML-017 | Complete | `FeesTaxDistributionFormulaEngine` (`DailyTrusteeFee`) | `FeesTaxDistributionFormulaEngineTests.cs` |
| D-FML-018 | Complete | `FeesTaxDistributionFormulaEngine` (`AdminFee`) | `FeesTaxDistributionFormulaEngineTests.cs` |
| D-FML-019 | Complete | `FeesTaxDistributionFormulaEngine` (`EntryFee`) | `FeesTaxDistributionFormulaEngineTests.cs` |
| D-FML-020 | Complete | `FeesTaxDistributionFormulaEngine` (`ExitFee`) | `FeesTaxDistributionFormulaEngineTests.cs` |
| D-FML-021 | Complete | `FeesTaxDistributionFormulaEngine` (`SwitchFee`) | `FeesTaxDistributionFormulaEngineTests.cs` |
| D-FML-022 | Complete | `FeesTaxDistributionFormulaEngine` (`PerformanceFee`) | `FeesTaxDistributionFormulaEngineTests.cs` |
| D-FML-023 | Complete | `FeesTaxDistributionFormulaEngine` (`Vat`) | `FeesTaxDistributionFormulaEngineTests.cs` |
| D-FML-024 | Complete | `FeesTaxDistributionFormulaEngine` (`WithholdingTax`) | `FeesTaxDistributionFormulaEngineTests.cs` |
| D-FML-025 | Complete | `FeesTaxDistributionFormulaEngine` (`NetPayable`) | `FeesTaxDistributionFormulaEngineTests.cs` |
| D-FML-026 | Complete | `FeesTaxDistributionFormulaEngine` (`TotalExpenseRatio`) | `FeesTaxDistributionFormulaEngineTests.cs` |
| D-FML-027 | Complete | `FeesTaxDistributionFormulaEngine` (`GrossDistributableIncome`) | `FeesTaxDistributionFormulaEngineTests.cs` |
| D-FML-028 | Complete | `FeesTaxDistributionFormulaEngine` (`NetDistributableIncome`) | `FeesTaxDistributionFormulaEngineTests.cs` |
| D-FML-029 | Complete | `FeesTaxDistributionFormulaEngine` (`DistributionPerUnit`) | `FeesTaxDistributionFormulaEngineTests.cs` |
| D-FML-030 | Complete | `FeesTaxDistributionFormulaEngine` (`InvestorGrossDistribution`) | `FeesTaxDistributionFormulaEngineTests.cs` |
| D-FML-031 | Complete | `FeesTaxDistributionFormulaEngine` (`InvestorNetDistribution`) | `FeesTaxDistributionFormulaEngineTests.cs` |
| D-FML-032 | Complete | `FeesTaxDistributionFormulaEngine` (`ReinvestmentUnits`) | `FeesTaxDistributionFormulaEngineTests.cs` |
| D-FML-033 | Complete | `FeesTaxDistributionFormulaEngine` (`DistributionCoverage`) | `FeesTaxDistributionFormulaEngineTests.cs` |
| D-FML-034 | Complete | `FeesTaxDistributionFormulaEngine` (`InvestorReturnForPeriod`) | `FeesTaxDistributionFormulaEngineTests.cs` |
| D-FML-035 | Complete | `src/Cis.Domain/ComplianceRisk/ComplianceRiskFormulaEngine.cs` (`AssetsUnderManagement`) | `tests/Cis.Tests.Unit/ComplianceRisk/ComplianceRiskFormulaEngineTests.cs` |
| D-FML-036 | Complete | `ComplianceRiskFormulaEngine` (`NetFlow`) | `ComplianceRiskFormulaEngineTests.cs` |
| D-FML-037 | Complete | `ComplianceRiskFormulaEngine` (`AverageNav`) | `ComplianceRiskFormulaEngineTests.cs` |
| D-FML-038 | Complete | `ComplianceRiskFormulaEngine` (`ExpenseToAumRatio`) | `ComplianceRiskFormulaEngineTests.cs` |
| D-FML-039 | Complete | `ComplianceRiskFormulaEngine` (`LiquidityCoverageRatio`) | `ComplianceRiskFormulaEngineTests.cs` |
| D-FML-040 | Complete | `ComplianceRiskFormulaEngine` (`StressCoverage`) | `ComplianceRiskFormulaEngineTests.cs` |
| D-FML-041 | Complete | `ComplianceRiskFormulaEngine` (`WeightedAverageMaturity`) | `ComplianceRiskFormulaEngineTests.cs` |
| D-FML-042 | Complete | `ComplianceRiskFormulaEngine` (`AnnualizedYield`) | `ComplianceRiskFormulaEngineTests.cs` |

## 5. Gap register closure table (G-01 to G-14)

| Gap ID | Status | Gap statement | Exact file/module recommendation |
| --- | --- | --- | --- |
| G-01 | Closed | `FR-034` to `FR-042` were missing from the traceability baseline | Closed by explicit cash and portfolio rows in `docs/srs-traceability.md` mapped to existing contracts, APIs, audits, and tests |
| G-02 | Closed | `NF-003` and `NF-005` to `NF-011` were missing from the traceability baseline | Closed by extending `docs/srs-traceability.md` and `docs/security-controls.md` with evidence-backed NF rows |
| G-03 | Open | Standalone Documents module remains incomplete | Add `src/Cis.Domain/Documents`, `src/Cis.Infrastructure/Documents`, `src/Cis.Api/Documents`, contracts, EF config, migration, and tests |
| G-04 | Open | Report catalogue handlers are generic, not report-specific | Refactor `src/Cis.Infrastructure/Reports/ReportQueryHandlers.cs` into per-report projection handlers |
| G-05 | Open | Report DTOs are generic summary rows, not report-shaped outputs | Replace generic DTO records in `src/Cis.Contracts/Reports/ReportCatalogueContracts.cs` with per-report data contracts |
| G-06 | Open | Per-report correctness tests are missing | Expand `tests/Cis.Tests.Integration/Reports/ReportsModuleTests.cs` or add family-specific report test files with seeded assertions |
| G-07 | Open | Cash module lacks a dedicated module test suite covering happy path, validation, authorization, and audit matrix | Add `tests/Cis.Tests.Integration/Cash/CashModuleTests.cs` |
| G-08 | Closed | Cash capability was only high-level traceable under `SRS-CASH-001`, not broken down into FR ids | Closed by mapping `FR-034` to `FR-038` in `docs/srs-traceability.md` and tying `SRS-CASH-001` to those rows |
| G-09 | Partial | Background jobs run through an in-process dispatcher, not a durable Hangfire/Quartz scheduler | Replace `src/Cis.Infrastructure/Operations/BackgroundJobDispatcher.cs` with a persisted Hangfire/Quartz implementation; align `src/Cis.Api/Program.cs` and `docs/performance-plan.md` |
| G-10 | Partial | Heavy job API contract is still synchronous from the caller perspective | Add durable job submission/status APIs in the relevant modules and operations dashboard |
| G-11 | Open | Regulator/report packs currently return generic summary rows instead of regulator-ready content packs | Extend `src/Cis.Infrastructure/Reports/ReportQueryHandlers.cs` and `src/Cis.Contracts/Reports` with pack-specific shapes and seeded bundle tests |
| G-12 | Open | Traceability currently overstates report-catalogue completeness | Reconcile `docs/srs-traceability.md` with actual generic report implementation status |
| G-13 | Partial | Kubernetes is skeleton-only; no Helm chart or richer deployment packaging | If Helm is desired, add `deploy/helm`; otherwise expand `deploy/k8s` with ingress, probes, and secret templates |
| G-14 | Closed | Final SRS coverage audit document was missing | Closed by this file: `docs/final-srs-coverage-audit.md` |

## 6. Missing / partial / incomplete features

| Feature | Status | Why it is not complete | Exact recommendation |
| --- | --- | --- | --- |
| Documents module | Open | Only KYC metadata, portal upload requests, and local document storage exist; no standalone document domain/API/retention workflow module is complete | Implement `src/Cis.Domain/Documents`, `src/Cis.Contracts/Documents`, `src/Cis.Infrastructure/Documents`, `src/Cis.Api/Documents`, migrations, and integration tests |
| Report catalogue semantics | Partial | All catalogue codes are registered, but the query path returns generic summary rows instead of report-shaped datasets | Replace the generic handler/DTO approach in `src/Cis.Infrastructure/Reports/ReportQueryHandlers.cs` and `src/Cis.Contracts/Reports/ReportCatalogueContracts.cs` |
| Report seeded correctness tests | Partial | Current tests verify registration, permissions, and generic row output, not the full data semantics of each report | Add per-family or per-report integration tests in `tests/Cis.Tests.Integration/Reports` |
| Durable background scheduler | Partial | Queue-backed execution exists but is in-process, per `docs/performance-plan.md` follow-on work | Replace `src/Cis.Infrastructure/Operations/BackgroundJobDispatcher.cs` with Hangfire or Quartz persistence |
| Cash module dedicated tests | Partial | Cash behavior is covered through lifecycle/performance tests, but there is no dedicated module suite meeting the module-by-module standard | Add `tests/Cis.Tests.Integration/Cash/CashModuleTests.cs` |

## 7. Tests per module

| Module | Test evidence | Coverage note | Audit view |
| --- | --- | --- | --- |
| Platform/Bootstrap | `tests/Cis.Tests.Integration/Health/HealthEndpointTests.cs` | Health endpoint smoke test | Good |
| Identity | `tests/Cis.Tests.Integration/Identity/IdentityAccessControlTests.cs`, `tests/Cis.Tests.Integration/Security/AuthorizationPolicyTests.cs`, `tests/Cis.Tests.Integration/Security/SecurityHardeningTests.cs`, `tests/Cis.Tests.Unit/Identity/SegregationOfDutiesServiceTests.cs` | Authn/authz/SoD/security cases covered | Good |
| Schemes | `tests/Cis.Tests.Integration/Schemes/SchemesModuleTests.cs` | Happy path, validation, auth, audit | Good |
| Investors/KYC/AML | `tests/Cis.Tests.Integration/Investors/InvestorsKycAmlDocumentsTests.cs` | Happy path, validation, AML, duplicates, audit | Good |
| Dealing | `tests/Cis.Tests.Integration/Dealing/DealingModuleTests.cs` | Subscription/redemption/switch/transfer/lien/recurring plan | Good |
| Unit Register | `tests/Cis.Tests.Integration/UnitRegister/UnitRegisterModuleTests.cs`, `tests/Cis.Tests.Unit/UnitRegister/UnitLedgerEntryTests.cs` | Ledger, holdings, reconstruction, approval | Good |
| Cash | `tests/Cis.Tests.Integration/Lifecycle/CompleteCisLifecycleTests.cs`, `tests/Cis.Tests.Integration/Performance/PerformanceReadinessTests.cs` | Real scenarios exist, but no dedicated module suite | Partial |
| Portfolio | `tests/Cis.Tests.Integration/Portfolio/PortfolioModuleTests.cs` | Instruments, placements, maturity, income, rollover | Good |
| NAV | `tests/Cis.Tests.Integration/NAV/NavModuleTests.cs`, `tests/Cis.Tests.Unit/NAV/NavFormulaEngineTests.cs` | Workflow + formulas | Good |
| Accounting | `tests/Cis.Tests.Integration/Accounting/AccountingModuleTests.cs`, `tests/Cis.Tests.Unit/Accounting/JournalTests.cs` | Journals, period close, reconciliation | Good |
| Fees/Tax/Distribution | `tests/Cis.Tests.Integration/FeesTaxDistribution/FeesTaxDistributionModuleTests.cs`, `tests/Cis.Tests.Unit/FeesTaxDistribution/FeesTaxDistributionFormulaEngineTests.cs` | Accruals, taxes, distributions, formulas | Good |
| Compliance/Risk | `tests/Cis.Tests.Integration/ComplianceRisk/ComplianceRiskModuleTests.cs`, `tests/Cis.Tests.Unit/ComplianceRisk/ComplianceRiskFormulaEngineTests.cs` | Limits, breaches, liquidity/stress, formulas | Good |
| Custody/Reconciliation | `tests/Cis.Tests.Integration/CustodyReconciliation/CustodyReconciliationModuleTests.cs` | Imports, matching, breaks, confirmations | Good |
| Portal | `tests/Cis.Tests.Integration/Portal/PortalModuleTests.cs` | Self-registration, OTP, MFA, self-service, workflows | Good |
| Reports | `tests/Cis.Tests.Integration/Reports/ReportsModuleTests.cs` | Registry, permissions, run/approve/publish, generic query/export loop | Partial |
| Audit/Workflow/Archive | `tests/Cis.Tests.Integration/Platform/AuditWorkflowArchiveTests.cs`, `tests/Cis.Tests.Unit/Workflow/WorkflowInstanceTests.cs`, `tests/Cis.Tests.Unit/Audit/AuditLogTests.cs`, `tests/Cis.Tests.Unit/Archive/ImmutableArchiveRecordTests.cs` | Strong infrastructure evidence | Good |
| Integrations | `tests/Cis.Tests.Integration/Integrations/IntegrationsModuleTests.cs` | Idempotency, failures, retries, local document storage | Good |
| Cases | `tests/Cis.Tests.Integration/Cases/CaseManagementModuleTests.cs` | Complaint lifecycle and reporting source data | Good |
| Data Quality | `tests/Cis.Tests.Integration/DataQuality/DataQualityModuleTests.cs` | Rule execution, assignment, resolution, dashboard | Good |
| Operations | `tests/Cis.Tests.Integration/Operations/OperationsReadinessTests.cs` | Config validation, health, DR records | Good |
| Lifecycle end-to-end | `tests/Cis.Tests.Integration/Lifecycle/CompleteCisLifecycleTests.cs` | Nine end-to-end CIS lifecycle scenarios | Good |
| Performance | `tests/Cis.Tests.Integration/Performance/PerformanceReadinessTests.cs`, `tests/Cis.Tests.Unit/Performance/CalculationBenchmarkTests.cs` | Baseline performance validation | Good |

## 8. Deployment readiness checklist

| Item | Status | Evidence | Remaining action if not ready |
| --- | --- | --- | --- |
| Solution builds | Ready | `README.md`, build/test artifacts referenced across prior work | - |
| PostgreSQL persistence + migrations | Ready | `src/Cis.Infrastructure/Persistence`, migration set present | - |
| Docker Compose dev stack | Ready | `docker-compose.yml`, `README.md` | - |
| Production-like Compose | Ready | `docker-compose.prod.yml` | - |
| Kubernetes skeleton | Partial | `deploy/k8s/*` exists | Expand manifests or add Helm if required |
| Startup config validation | Ready | `src/Cis.Api/Configuration/OperationalReadinessConfigurationValidator.cs` | - |
| Health/readiness/liveness | Ready | `src/Cis.Api/Program.cs`, operations tests | - |
| Metrics endpoint | Ready | `/metrics` in `Program.cs` | - |
| Secured jobs dashboard | Ready | `/jobs` in `Program.cs` | - |
| Backup/restore scripts | Ready | `scripts/backup-postgres.ps1`, `scripts/restore-postgres.ps1`, runbooks | - |
| Migration runner script | Ready | `scripts/run-migrations.ps1` | - |
| Environment-specific config | Ready | `appsettings.Development/Test/Testing/UAT/Production.json` | - |
| Durable background scheduling | Partial | In-process dispatcher only; `docs/performance-plan.md` already flags follow-on work | Replace with Hangfire/Quartz |
| Report/regulator pack completeness | Partial | Generic query handlers mean operationally published packs are structurally weak | Implement real pack projections before go-live |
| Documents module operational completeness | Blocked | `SRS-DOCUMENTS-001` remains open | Complete Documents module before regulated production use |

## 9. Security readiness checklist

| Control | Status | Evidence | Remaining action if not ready |
| --- | --- | --- | --- |
| JWT validation and expiry | Ready | `src/Cis.Api/Program.cs`, `src/Cis.Infrastructure/Identity/JwtTokenService.cs`, security tests | - |
| Refresh-token rotation | Ready | `IdentityService.cs`, security controls doc, identity tests | - |
| Password hashing / policy / lockout | Ready | `IdentityService.cs`, `PasswordPolicyValidator.cs`, security tests | - |
| MFA-ready enforcement | Ready | `PortalService.cs`, identity login checks, portal/security tests | - |
| Permission-based authorization | Ready | `RequirePermissionAttribute`, fallback policy, controller coverage | - |
| Object-level portal authorization | Ready | `PortalService.cs`, `PortalModuleTests.cs`, `SecurityHardeningTests.cs` | - |
| Rate limiting | Ready | `Program.cs` | - |
| Secure headers / HTTPS / HSTS | Ready | `SecureHeadersMiddleware.cs`, `Program.cs` | - |
| ProblemDetails consistency | Ready | `GlobalExceptionHandler.cs`, `Program.cs` | - |
| Security audit logging | Ready | `AuditLogWriter.cs`, `HttpSecurityAuditExtensions.cs`, tests | - |
| File validation | Ready for metadata validation | `FileSecurityValidator.cs` and tests reject malicious uploads | Consider external malware scanner implementation beyond stub before production |
| Secrets handling | Ready with caveat | Production validator blocks placeholders; environment/K8s secret pattern exists | Integrate enterprise secret manager if required by deployment policy |
| Undefined NF ids | Ready | NF-001 to NF-012 are now explicitly defined and classified in traceability and security controls documentation | - |

## 10. Regulator inspection readiness checklist

| Inspection area | Status | Evidence | Remaining action if not ready |
| --- | --- | --- | --- |
| Immutable audit trail | Ready | `AuditLog`, `AuditLogWriter`, append-only guard in `CisDbContext`, tests | - |
| Segregation of duties | Ready | Workflow/identity/portal enforcement and tests | - |
| Immutable archive / published NAV / published reports | Ready | Archive/NAV/report immutability guards and tests | - |
| Investor onboarding evidence | Ready | Investors/KYC/AML persistence + portal self-registration + tests | - |
| Dealing traceability | Ready | Dealing status history, audit logs, lifecycle tests | - |
| Unit register reconstruction | Ready | Historical holding view and tests | - |
| NAV reconstruction | Ready | NAV history/reconstruction APIs and tests | - |
| Custody break evidence | Ready | Break aging/action/evidence persistence and tests | - |
| Operational DR records | Ready | Operations module + runbooks + tests | - |
| Document governance | Not ready | Only partial document capability exists; `SRS-DOCUMENTS-001` is still open | Complete standalone Documents module |
| Report catalogue substance | Partial | Catalogue registration exists, but report content is generic summary-row output | Implement report-specific query datasets before regulator sign-off |
| Numbered SRS completeness | Ready | FR-001 to FR-083 and NF-001 to NF-012 are now explicitly defined and classified in repository traceability artifacts | - |

## Final classification summary

- Strongly complete areas: Identity, Schemes, Investors, Dealing, Unit Register, NAV, Accounting, Fees/Tax/Distribution, Compliance/Risk, Custody/Reconciliation, Portal, Audit/Workflow/Archive, Integrations, Cases, Data Quality, Operations
- Partially complete areas: Cash module evidence depth, report catalogue semantics, durable background-job scheduling, regulator-ready report content
- Open area: Documents module
- Baseline numbering is now explicit for `FR-001` to `FR-083` and `NF-001` to `NF-012`; remaining issues are substance gaps, not numbering gaps

## Exact SRS ids currently supportable as complete from repository evidence

- FR: `FR-001` to `FR-083`
- NF: `NF-001` to `NF-010`, `NF-012`
- Formulas: `D-FML-001` to `D-FML-042`

## Exact SRS ids not supportable as complete from repository evidence

- Partial non-functional coverage: `NF-011`
- Open solution area: `SRS-DOCUMENTS-001`

