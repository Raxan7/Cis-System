# Disaster Recovery Test Plan

## Objectives

- Prove the configured RTO and RPO are achievable.
- Verify backup integrity and restore repeatability.
- Confirm application health checks, storage, database, and background job readiness after recovery.

## Test Scenarios

- Database logical backup restore to isolated target.
- API redeployment using production-like configuration.
- Storage mount replacement and health probe validation.
- Background job dashboard access by `SystemAdmin`.

## Evidence

Each DR test must capture:

- Planned time and actual start/end UTC.
- Backup artifact and checksum.
- Restore command output location.
- `/health/ready` response.
- Smoke-test screenshots or logs.
- RTO/RPO achieved values.
- Findings, remediation actions, and approver notes.

## Recording

Use `POST /api/operations/dr-tests` to schedule or record DR test evidence. Completed restore validation should be persisted as a `RestoreTestRecord` through a controlled operational data load until a dedicated restore-test API is added.
