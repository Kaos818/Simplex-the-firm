# Local facial verification

Simplex performs one-to-one comparison between the latest acceptable `SA_ID` portrait and a live capture. It never searches unrelated users, approves beneficiaries, or releases trust funds.

Prerequisites: Python 3.12, Tesseract OCR, OpenCV-compatible YuNet/SFace ONNX models, and .NET 10.

Configure secrets:

```powershell
dotnet user-secrets init
dotnet user-secrets set "Verification:ApiKey" "<generated-key>"
$env:SIMPLEX_VERIFICATION_API_KEY="<same-key>"
```

Place approved local models in `simplex-verification-service/models`. Production fails closed when models are unavailable. Raw frames must be processed transiently and must not be logged or permanently stored. Automated results are pre-screening signals; an authorised administrator remains responsible for the final decision.

Run:

```powershell
.\scripts\setup-verification-service.ps1
.\simplex-verification-service\.venv\Scripts\uvicorn.exe app.main:app --host 127.0.0.1 --port 8091
```

The service uses local YuNet face detection and SFace one-to-one comparison when the approved models are installed. Production fails closed if either model is unavailable. Development may use the explicitly enabled Haar fallback for capture-quality checks, but it cannot produce an automated verified identity result. A manual-review route remains available and always requires an authorised administrator's recorded decision.

Check `http://127.0.0.1:8091/health` before enabling the capture flow. It reports `face_matching_ready: true` only when both approved models loaded successfully. SFace matching uses YuNet landmarks to align both faces before comparison; plain bounding-box crops are not used for automatic matching.
