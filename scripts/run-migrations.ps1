param(
    [Parameter(Mandatory = $true)]
    [string] $ConnectionString,

    [Parameter(Mandatory = $false)]
    [string] $Environment = "UAT"
)

$ErrorActionPreference = "Stop"
$env:ConnectionStrings__CisDb = $ConnectionString
$env:ASPNETCORE_ENVIRONMENT = $Environment
$env:DOTNET_ROLL_FORWARD = "Major"

dotnet ef database update --project src/Cis.Infrastructure/Cis.Infrastructure.csproj --startup-project src/Cis.Api/Cis.Api.csproj
