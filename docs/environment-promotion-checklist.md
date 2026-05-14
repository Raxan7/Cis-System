# Environment Promotion Checklist

## Development To Test

- Build succeeds.
- Unit and integration tests pass.
- Migration creates successfully.
- `appsettings.Test.json` has correct non-secret values.
- `/health/ready` passes against Test PostgreSQL and storage.

## Test To UAT

- Migration reviewed.
- Seed data reviewed.
- Backup and restore scripts tested.
- UAT configuration values reviewed.
- Access permissions verified for Operations endpoints.

## UAT To Production

- Production secrets supplied through the deployment secret store.
- `ASPNETCORE_ENVIRONMENT=Production`.
- Startup validation passes with no placeholder values.
- Pre-deployment backup completed and checksum verified.
- Migration run approved and recorded.
- Kubernetes readiness/liveness probes enabled.
- Rollback and restore decision points confirmed.

## Post-Deployment

- `/health/live`, `/health/ready`, and `/api/operations/health/deep` checked.
- `/metrics` reachable by `SystemAdmin`.
- Background job dashboard status checked by `SystemAdmin`.
- Audit logs reviewed for deployment-era operational actions.
