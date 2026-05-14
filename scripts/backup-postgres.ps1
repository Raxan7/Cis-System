param(
    [Parameter(Mandatory = $true)]
    [string] $ConnectionString,

    [Parameter(Mandatory = $false)]
    [string] $OutputDirectory = "ops/backups",

    [Parameter(Mandatory = $false)]
    [string] $DatabaseName = "cis"
)

$ErrorActionPreference = "Stop"

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$timestamp = (Get-Date).ToUniversalTime().ToString("yyyyMMddHHmmss")
$backupPath = Join-Path $OutputDirectory "$DatabaseName-$timestamp.dump"

Write-Host "Creating PostgreSQL logical backup at $backupPath"
pg_dump --format=custom --verbose --file="$backupPath" "$ConnectionString"

$hash = Get-FileHash -Algorithm SHA256 -LiteralPath $backupPath
Set-Content -LiteralPath "$backupPath.sha256" -Value "$($hash.Hash)  $(Split-Path -Leaf $backupPath)"
Write-Host "Backup complete. SHA256: $($hash.Hash)"
