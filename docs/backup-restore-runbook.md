# Backup And Restore Runbook

## Backup

1. Confirm the target environment and approved maintenance window.
2. Run:

```powershell
./scripts/backup-postgres.ps1 -ConnectionString "Host=...;Database=cis;Username=...;Password=..." -OutputDirectory "ops/backups" -DatabaseName "cis"
```

3. Verify the generated `.sha256` file.
4. Store the backup in durable storage matching the environment RPO policy.
5. Record the backup location, size, checksum, operator, and timestamp as a `BackupRunRecord`.

## Restore Test

1. Provision an isolated restore target.
2. Run:

```powershell
./scripts/restore-postgres.ps1 -ConnectionString "Host=...;Database=cis_restore;Username=...;Password=..." -BackupPath "ops/backups/cis-YYYYMMDDHHMMSS.dump" -Clean
```

3. Start the API against the restored database in `Test` or `UAT`.
4. Capture `/health/ready`, login, report catalogue, and sample investor/dealing smoke-test evidence.
5. Record the evidence and validation summary as a `RestoreTestRecord`.

## Controls

- Production restores require incident/change approval.
- Restores must never overwrite production unless the disaster recovery incident commander approves the failover.
- Checksums and evidence references are mandatory for audit review.
