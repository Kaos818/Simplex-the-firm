$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $projectRoot

$env:ASPNETCORE_ENVIRONMENT = "Development"
Write-Host "Starting Simplex Law at http://localhost:5203" -ForegroundColor Cyan
dotnet restore
dotnet run --launch-profile http
