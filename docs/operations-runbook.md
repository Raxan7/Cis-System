# Operations Runbook

## Service Endpoints

- Liveness: `GET /health/live`
- Readiness: `GET /health/ready`
- Basic health: `GET /health`
- Deep health: `GET /api/operations/health/deep` with `Operations.Health.Read`
- Metrics: `GET /metrics` with `SystemAdmin`
- Background job dashboard status: `GET /jobs` with `SystemAdmin`

## Startup Configuration

The API validates configuration during startup. Production fails fast when database credentials, JWT signing key, bootstrap administrator credentials, backup directory, storage path, or background job settings are missing or left as placeholders.

Required production secrets are supplied through environment variables or Kubernetes secrets:

- `ConnectionStrings__CisDb`
- `Jwt__SigningKey`
- `Identity__BootstrapAdmin__Email`
- `Identity__BootstrapAdmin__Password`

## Migration Procedure

Run migrations as a controlled deployment step:

```powershell
./scripts/run-migrations.ps1 -ConnectionString "Host=...;Database=cis;Username=...;Password=..." -Environment UAT
```

For production, take a verified backup before running migrations, record the migration ticket, then capture `/health/ready` and application smoke-test evidence after completion.

## Operational Records

RTO/RPO targets are seeded per environment in the `operations.rto_rpo_configurations` table. DR tests are recorded through `POST /api/operations/dr-tests`, and backup/restore run records are retained in the Operations module for audit review.

## Escalation

Any failed readiness dependency is an incident candidate. Database failures route to infrastructure/database support; storage failures route to platform operations; background job health failures route to application support.
