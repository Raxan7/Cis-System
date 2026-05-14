param(
    [Parameter(Mandatory = $true)]
    [string] $ConnectionString,

    [Parameter(Mandatory = $true)]
    [string] $BackupPath,

    [Parameter(Mandatory = $false)]
    [switch] $Clean
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $BackupPath)) {
    throw "Backup file was not found: $BackupPath"
}

$cleanArgument = @()
if ($Clean) {
    $cleanArgument = @("--clean", "--if-exists")
}

Write-Host "Restoring PostgreSQL backup from $BackupPath"
pg_restore @cleanArgument --verbose --dbname="$ConnectionString" "$BackupPath"
Write-Host "Restore complete. Run application smoke tests and record evidence in a RestoreTestRecord."
