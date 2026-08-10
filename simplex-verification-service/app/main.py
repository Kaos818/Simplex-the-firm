import json
from fastapi import Depends, FastAPI, File, Form, HTTPException, UploadFile
from .config import settings
from .document_analyser import analyse
from .security import require_api_key
from .face_verifier import verify
from .face_detector import detector_ready
from .face_matcher import matcher_ready

docs = "/docs" if settings.environment == "development" else None
app = FastAPI(title="Simplex Local Verification", docs_url=docs, redoc_url=None, openapi_url="/openapi.json" if docs else None)

@app.get("/health")
def health():
    models_ready = detector_ready() and matcher_ready()
    return {"status": "healthy" if models_ready else "degraded", "service": "simplex-verification", "face_matching_ready": models_ready}

@app.post("/api/v1/documents/analyse", dependencies=[Depends(require_api_key)])
async def analyse_document(file: UploadFile = File(...), requirement_code: str = Form(...), requires_certified_copy: bool = Form(...), requires_expiry_check: bool = Form(...)):
    data = await file.read(settings.max_bytes + 1)
    if len(data) > settings.max_bytes:
        raise HTTPException(413, "Request too large")
    return analyse(data, requirement_code, requires_certified_copy, requires_expiry_check)

@app.post("/api/v1/faces/verify", dependencies=[Depends(require_api_key)])
async def verify_faces(payload: str = Form(...), reference: UploadFile = File(...), frames: list[UploadFile] = File(...)):
    try:
        request = json.loads(payload)
    except json.JSONDecodeError:
        raise HTTPException(400, "Invalid capture metadata")
    if len(frames) not in range(20, 61):
        raise HTTPException(400, "Capture requires 20 to 60 frames")
    reference_data = await reference.read(settings.max_bytes + 1)
    if len(reference_data) > settings.max_bytes:
        raise HTTPException(413, "Reference image too large")
    frame_data, total = [], 0
    for frame in frames:
        data = await frame.read(1_000_001)
        if len(data) > 1_000_000:
            raise HTTPException(413, "Camera frame too large")
        total += len(data)
        if total > 30 * 1024 * 1024:
            raise HTTPException(413, "Capture too large")
        frame_data.append(data)
    return verify(reference_data, frame_data, request)
