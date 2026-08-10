$ErrorActionPreference = "Stop"
$serviceRoot = Join-Path $PSScriptRoot "..\simplex-verification-service"
$python = Get-Command py -ErrorAction SilentlyContinue
if ($python) { $pythonArgs = @('-3') } else { $python = Get-Command python -ErrorAction Stop; $pythonArgs = @() }
& $python.Source @pythonArgs -m venv (Join-Path $serviceRoot ".venv")
& (Join-Path $serviceRoot ".venv\Scripts\python.exe") -m pip install -r (Join-Path $serviceRoot "requirements.txt")
$modelRoot = Join-Path $serviceRoot "models"
New-Item -ItemType Directory -Force -Path $modelRoot | Out-Null
function Install-OpenCvModel([string]$Name, [string]$Url, [string]$ExpectedSha256) {
    $destination = Join-Path $modelRoot $Name
    if (-not (Test-Path $destination)) { Invoke-WebRequest -Uri $Url -OutFile $destination }
    $actual = (Get-FileHash -Algorithm SHA256 -LiteralPath $destination).Hash
    if ($ExpectedSha256 -and $actual -ne $ExpectedSha256.ToUpperInvariant()) { Remove-Item -LiteralPath $destination -Force; throw "Hash verification failed for $Name" }
    Write-Host "$Name SHA-256: $actual"
}
Install-OpenCvModel "face_detection_yunet_2023mar.onnx" "https://github.com/opencv/opencv_zoo/raw/main/models/face_detection_yunet/face_detection_yunet_2023mar.onnx" $env:SIMPLEX_YUNET_SHA256
Install-OpenCvModel "face_recognition_sface_2021dec.onnx" "https://github.com/opencv/opencv_zoo/raw/main/models/face_recognition_sface/face_recognition_sface_2021dec.onnx" $env:SIMPLEX_SFACE_SHA256
$keyBytes = New-Object byte[] 32
[System.Security.Cryptography.RandomNumberGenerator]::Fill($keyBytes)
$key = [Convert]::ToBase64String($keyBytes).TrimEnd('=').Replace('+','-').Replace('/','_')
Write-Host "Set for this terminal: `$env:SIMPLEX_VERIFICATION_API_KEY='$key'"
Write-Host "Matching .NET command: dotnet user-secrets set `"Verification:ApiKey`" `"$key`""
Write-Host "Models installed locally. Set SIMPLEX_YUNET_SHA256 and SIMPLEX_SFACE_SHA256 to enforce published hashes during setup."
