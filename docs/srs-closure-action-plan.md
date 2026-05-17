# SRS Closure Action Plan

Date: 2026-05-17  
Source audit: [final-srs-coverage-audit.md](</C:/Users/WINDOWS 11/Documents/Projects/CIS-SYSTEM/docs/final-srs-coverage-audit.md>)

## Goal

Move the repository from `audited` to `closure-ready` with the least thrash by:

1. fixing release-blocking compliance gaps first
2. closing evidence gaps that weaken regulator confidence
3. delaying lower-risk structural improvements until after the release baseline is sound

## Planning assumptions

- We do not destabilize working modules just to improve aesthetics.
- We prefer traceability and report-shape correctness over broad refactors.
- We treat regulated-release blockers differently from post-release hardening items.
- We only call an item closed when code, persistence, API/background entry point, authorization, audit, tests, and traceability are all aligned.

## Risk tiers

- `P0 Release blocker`: do before regulated production release
- `P1 Release critical`: should land before release sign-off, but can follow P0 in the same stream
- `P2 Stabilization`: important, but can land after sign-off if the release decision explicitly accepts the risk
- `P3 Improvement`: useful cleanup and scale work, not a blocker

## Recommended execution order

1. Normalize the SRS baseline numbering so the team is not building against invisible requirement ids
2. Finish the Documents module
3. Replace generic report catalogue outputs with report-shaped projections and evidence tests
4. Add missing cash-module direct evidence
5. Reconcile traceability overstating reports completeness
6. Decide whether durable scheduling is a release requirement or a controlled post-release improvement

## Workstream 1: Baseline repair

### P0-1 Define missing FR ids and NF ids

Status update:
- `Completed on 2026-05-17` by extending `docs/srs-traceability.md`, `docs/security-controls.md`, and `docs/final-srs-coverage-audit.md`

Why first:
- the original audit found `FR-034` to `FR-042` and `NF-003`, `NF-005` to `NF-011` missing from the baseline
- until those ids existed, the repository could not honestly claim complete classification

Files to update:
- [docs/srs-traceability.md](</C:/Users/WINDOWS 11/Documents/Projects/CIS-SYSTEM/docs/srs-traceability.md>)
- [docs/security-controls.md](</C:/Users/WINDOWS 11/Documents/Projects/CIS-SYSTEM/docs/security-controls.md>)

Recommended action:
- add exact requirement rows for `FR-034` to `FR-042`
- map those ids to the cash and portfolio capabilities already present
- add exact requirement rows for `NF-003`, `NF-005` to `NF-011`
- either map those ids to implemented controls or mark them open

Definition of done:
- no FR or NF id in the contractual range is undefined
- every id is classified as complete, partial, open, or not-applicable by explicit note
- final audit and traceability tell the same story

Release risk if skipped:
- formal inspection challenge on incomplete requirements baseline
- inability to prove "no requirement left unclassified"

## Workstream 2: Documents module closure

### P0-2 Complete `SRS-DOCUMENTS-001`

Why now:
- this is the only clearly open solution-area SRS in the current audit
- it affects regulator confidence around document governance, retention, and official storage controls

Current state:
- KYC document metadata exists
- portal upload requests exist
- local storage abstraction exists
- standalone Documents module does not

Files/modules to add or expand:
- `src/Cis.Domain/Documents/*`
- `src/Cis.Contracts/Documents/*`
- `src/Cis.Application/Common/Interfaces/*` for document services if needed
- `src/Cis.Infrastructure/Documents/*`
- `src/Cis.Api/Documents/*`
- `src/Cis.Infrastructure/Persistence/Configurations/*`
- new EF migration under `src/Cis.Infrastructure/Persistence/Migrations/*`
- tests under `tests/Cis.Tests.Integration/Documents/*`

Minimum functional scope:
- document aggregate and metadata model
- document category / retention / status model
- persistence and constraints
- API for document registration, retrieval metadata, retention controls, archive/legal-hold behavior if in scope
- authorization policies
- audit trail for document create/update/archive actions
- tests for happy path, validation, auth, audit
- traceability update

Definition of done:
- `SRS-DOCUMENTS-001` can move from open to complete or partial with explicit evidence

Release risk if skipped:
- regulator inspection weakness around document governance
- audit trail exists around KYC metadata, but document lifecycle remains incomplete

## Workstream 3: Report catalogue substance

### P0-3 Replace generic report summary outputs with report-shaped query projections

Why this is release-blocking:
- the report catalogue is currently registered and permissioned, but not semantically complete
- the system claims a full regulator and management catalogue; today it mostly returns generic one-row summaries

Current weak points:
- generic registry in [ReportQueryHandlers.cs](</C:/Users/WINDOWS 11/Documents/Projects/CIS-SYSTEM/src/Cis.Infrastructure/Reports/ReportQueryHandlers.cs>)
- generic DTO rows in [ReportCatalogueContracts.cs](</C:/Users/WINDOWS 11/Documents/Projects/CIS-SYSTEM/src/Cis.Contracts/Reports/ReportCatalogueContracts.cs>)
- tests confirm registration and generic output, not report-specific correctness

Files to change:
- [src/Cis.Infrastructure/Reports/ReportQueryHandlers.cs](</C:/Users/WINDOWS 11/Documents/Projects/CIS-SYSTEM/src/Cis.Infrastructure/Reports/ReportQueryHandlers.cs>)
- [src/Cis.Contracts/Reports/ReportCatalogueContracts.cs](</C:/Users/WINDOWS 11/Documents/Projects/CIS-SYSTEM/src/Cis.Contracts/Reports/ReportCatalogueContracts.cs>)
- [src/Cis.Infrastructure/Reports/ReportService.cs](</C:/Users/WINDOWS 11/Documents/Projects/CIS-SYSTEM/src/Cis.Infrastructure/Reports/ReportService.cs>)
- [docs/report-catalogue.md](</C:/Users/WINDOWS 11/Documents/Projects/CIS-SYSTEM/docs/report-catalogue.md>)
- [docs/srs-traceability.md](</C:/Users/WINDOWS 11/Documents/Projects/CIS-SYSTEM/docs/srs-traceability.md>)

Recommended implementation sequence:
1. keep the registry for metadata
2. split query logic by report family: `INV`, `SCH`, `CB`, `PF`, `NAV`, `FIN`, `CMP`, `CUS`, `MGT`, `REG`, `DGT`
3. introduce report-shaped DTOs per family or per report
4. preserve CSV export, but generate from the real result set
5. for regulator packs, compose meaningful rows and pack metadata instead of source-count summaries

Definition of done:
- each report code returns domain-relevant rows
- permissions still enforced
- CSV export reflects the actual report data
- traceability no longer overstates catalogue completeness

Release risk if skipped:
- catalogued reports exist "by name" but not in regulator-usable form
- this is a direct credibility problem in UAT/sign-off

### P1-1 Add report correctness tests by family

Files to add or expand:
- [tests/Cis.Tests.Integration/Reports/ReportsModuleTests.cs](</C:/Users/WINDOWS 11/Documents/Projects/CIS-SYSTEM/tests/Cis.Tests.Integration/Reports/ReportsModuleTests.cs>)
- optionally split into:
  - `tests/Cis.Tests.Integration/Reports/InvestorReportsTests.cs`
  - `tests/Cis.Tests.Integration/Reports/SchemeReportsTests.cs`
  - `tests/Cis.Tests.Integration/Reports/RegulatoryReportsTests.cs`

Recommended test style:
- seed targeted records
- call report endpoint
- assert row-level correctness, not just status code and presence of one row
- cover permissions and CSV format

Definition of done:
- each report family has seeded correctness assertions
- regulator packs have explicit content assertions

## Workstream 4: Cash traceability and direct evidence

### P1-2 Add a dedicated cash module test suite

Why:
- cash behavior exists, but the evidence is indirect through lifecycle and performance tests
- your module standard wants each module to stand on its own

Files to add:
- `tests/Cis.Tests.Integration/Cash/CashModuleTests.cs`

Recommended scenarios:
- bank import happy path
- duplicate idempotency replay
- mismatched idempotency payload rejection
- suspense creation and resolution
- payment instruction creation
- payment status update
- reversal request and approval
- unauthorized access
- audit trail assertions

Definition of done:
- cash no longer depends on lifecycle tests as its primary evidence surface
- `SRS-CASH-001` has direct module evidence

### P1-3 Backfill traceability for cash and portfolio FR ids after baseline repair

Files to update:
- [docs/srs-traceability.md](</C:/Users/WINDOWS 11/Documents/Projects/CIS-SYSTEM/docs/srs-traceability.md>)

Definition of done:
- `FR-034` to `FR-042` point to concrete cash/portfolio evidence rows

## Workstream 5: Traceability truthfulness

### P1-4 Reconcile overstated report claims

Why:
- the current traceability marks `SRS-REPORTS-002` as covered
- the audit found the report implementation is still generic in substance

Files to update:
- [docs/srs-traceability.md](</C:/Users/WINDOWS 11/Documents/Projects/CIS-SYSTEM/docs/srs-traceability.md>)
- [docs/final-srs-coverage-audit.md](</C:/Users/WINDOWS 11/Documents/Projects/CIS-SYSTEM/docs/final-srs-coverage-audit.md>) if needed after fixes

Recommended rule:
- if report-family work has not landed yet, mark `SRS-REPORTS-002` as `Partial`
- once report-shaped outputs and family correctness tests land, upgrade it back to `Covered`

Definition of done:
- traceability and code reality match

## Workstream 6: Scheduling and performance hardening

### P2-1 Decide if durable scheduling is release-critical

Current state:
- in-process dispatcher exists in [BackgroundJobDispatcher.cs](</C:/Users/WINDOWS 11/Documents/Projects/CIS-SYSTEM/src/Cis.Infrastructure/Operations/BackgroundJobDispatcher.cs>)
- config says `"Quartz"` in appsettings, but there is no actual Quartz job store implementation
- [performance-plan.md](</C:/Users/WINDOWS 11/Documents/Projects/CIS-SYSTEM/docs/performance-plan.md>) already calls this follow-on work

Decision gate:
- if production operations require durable retries, visibility across restarts, or scheduled regulator packs, this becomes `P1`
- if the release is controlled and low-scale, it can remain `P2` with explicit risk acceptance

If promoted to implementation:
- replace in-process dispatcher with Hangfire or Quartz persisted jobs
- align:
  - [src/Cis.Api/Program.cs](</C:/Users/WINDOWS 11/Documents/Projects/CIS-SYSTEM/src/Cis.Api/Program.cs>)
  - [src/Cis.Infrastructure/Operations/BackgroundJobDispatcher.cs](</C:/Users/WINDOWS 11/Documents/Projects/CIS-SYSTEM/src/Cis.Infrastructure/Operations/BackgroundJobDispatcher.cs>)
  - [src/Cis.Application/Common/Interfaces/IBackgroundJobDispatcher.cs](</C:/Users/WINDOWS 11/Documents/Projects/CIS-SYSTEM/src/Cis.Application/Common/Interfaces/IBackgroundJobDispatcher.cs>)
  - operations docs

Definition of done:
- background work survives restart
- job status is queryable
- health checks reflect actual scheduler state

### P2-2 Add explicit async job submission/status APIs for heavy operations

Reason:
- current caller contract is synchronous even when work is queued
- that is fine for local baseline validation, but weak for real operations

Targets:
- reports
- bank imports
- custody imports
- reconciliation runs
- NAV runs

## Workstream 7: Deployment packaging maturity

### P2-3 Deepen Kubernetes packaging

Current state:
- `deploy/k8s` is a usable skeleton
- not yet a full release package

Options:
- expand the raw manifests with ingress, resources, network policy, external secret wiring
- or add a Helm chart under `deploy/helm`

Decision:
- choose one packaging strategy; do not maintain both unless required

## Suggested release board

### Release blocker board (`P0`)

1. Define missing FR/NF baseline ids
2. Complete Documents module
3. Replace generic report catalogue outputs with report-shaped results

### Release critical board (`P1`)

1. Add report correctness tests by family
2. Add dedicated cash module tests
3. Backfill traceability for cash/portfolio FR ids
4. Reconcile overstated reports traceability

### Stabilization board (`P2`)

1. Decide and possibly implement durable scheduling
2. Add explicit async job APIs
3. Deepen Kubernetes packaging

### Improvement board (`P3`)

1. streaming CSV parsing for very large imports
2. materialized recurring report snapshots
3. richer metrics and NBomber suites

## Low-thrash delivery sequence

### Sequence A: documentation and baseline

- update `docs/srs-traceability.md`
- update `docs/security-controls.md`
- refresh the final audit if classifications change

This should happen first because it stabilizes the target.

### Sequence B: documents and reports

- build Documents module
- refactor report DTOs/handlers
- add report correctness tests

This is the most important product closure stream.

### Sequence C: evidence hardening

- add direct cash tests
- backfill cash/portfolio FR mapping
- align traceability statuses

### Sequence D: platform hardening

- durable scheduler decision
- async job contracts if needed
- deployment packaging maturity

## Exit criteria for "closure-ready"

We should call the repository closure-ready only when all of the following are true:

1. `FR-001` to `FR-083` are all explicitly classified with no numbering gaps
2. `NF-001` to `NF-012` are all explicitly classified with no numbering gaps
3. `SRS-DOCUMENTS-001` is no longer open
4. report catalogue outputs are report-shaped, not generic summary rows
5. direct cash module tests exist
6. traceability does not overstate completeness
7. final audit and traceability agree

## Recommended immediate next ticket set

If we want the cleanest next move after `TRACE-01`, I'd start with these next three tickets:

1. `DOCS-01`: Implement standalone Documents module skeleton with persistence, auth, audit, and tests
2. `RPT-01`: Replace generic report query rows for `REG`, `INV`, and `NAV` families first
3. `CASH-01`: Add dedicated `CashModuleTests`

That gives us the biggest reduction in release risk per unit of work.


