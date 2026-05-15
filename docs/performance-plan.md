# Performance Readiness Plan

## Scope

This pass focuses on operational performance readiness for the CIS modular monolith without weakening domain correctness or auditability.

Implemented in this pass:

- pagination metadata and page slicing on list endpoints
- database indexes for high-frequency filtering and sorting paths
- read-side query shaping for investor, scheme, user, audit, and report listing
- in-memory caching for stable reference data:
  - active report definitions
  - approval policies
- queue-backed execution path for heavy:
  - report runs
  - bank statement imports
  - custodian holdings imports
  - custodian cash imports
  - custody reconciliation runs
  - NAV creation and calculation runs
- explicit transaction boundaries for bulk import paths
- bulk insert friendly import processing with reduced per-line lookups
- baseline calculation benchmarks for NAV, fees/tax/distribution, and compliance formulas
- k6 load script for investor search and login-backed API traffic

## Design Notes

### Pagination

All list endpoints now accept the shared pagination contract:

- `pageNumber`
- `pageSize`
- `search`
- `sortBy`
- `sortDirection`

Responses include `meta.pagination` with:

- `pageNumber`
- `pageSize`
- `totalCount`
- `totalPages`

### Query Optimization

The most expensive list paths were changed to avoid table-wide aggregate materialization before paging:

- investors
- schemes
- users
- audit logs
- report definitions
- report runs

NAV history filtering was also pushed down to the database.

### Import Throughput

Bank statement and custody import processing now:

- preloads candidate lookups in batches
- avoids per-line existence queries
- batches entity creation with `AddRange`
- uses explicit database transactions
- temporarily disables EF Core automatic change detection during large inserts

### Background Execution

Heavy workloads run through a bounded in-process background dispatcher. The current API contract remains synchronous from the caller’s perspective, but the actual work is pushed through a queue-backed execution pipeline so the platform has a single place to observe pressure and evolve into a dedicated scheduler later.

Dashboard endpoint:

- `GET /jobs`

Health visibility:

- background queue state is included in readiness/deep health output

### Caching

Stable reference data is cached with short TTLs to reduce repeated reads while keeping behavior deterministic:

- active report definitions
- approval policies by workflow type

## Local Baseline Targets

These are practical local-engineering baselines, not production SLOs:

- investor search returns a paginated page from a seeded large dataset within a low-seconds threshold
- bank statement import handles 1,000+ rows idempotently
- heavy reports increment background job completion counters
- NAV history avoids in-memory filtering over the full publication table

## Load Test

Script:

- [load-tests/investor-search.js](</C:/Users/WINDOWS 11/Documents/Projects/CIS-SYSTEM/load-tests/investor-search.js>)

Example:

```powershell
k6 run `
  -e BASE_URL=http://localhost:8080 `
  -e LOGIN_EMAIL=admin.tests@victoryfs.local `
  -e LOGIN_PASSWORD=ChangeMe123! `
  .\load-tests\investor-search.js
```

## Follow-On Work

Recommended next steps for a later pass:

- replace the in-process queue with Hangfire or Quartz persisted jobs
- add streaming CSV parsing for very large imports
- add materialized report snapshots for recurring regulatory packs
- add Prometheus-style database query duration metrics
- add NBomber scenario suites against docker-compose environments
