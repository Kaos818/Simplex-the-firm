$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$service = Join-Path $root "simplex-verification-service"
$env:SIMPLEX_VERIFICATION_API_KEY = "simplex-local-prototype-key-2026"
$env:SIMPLEX_ENVIRONMENT = "development"
Start-Process -FilePath (Join-Path $service ".venv\Scripts\uvicorn.exe") -ArgumentList "app.main:app --host 127.0.0.1 --port 8091" -WorkingDirectory $service -WindowStyle Hidden
dotnet run --project (Join-Path $root "SimplexLawFirm.csproj")
