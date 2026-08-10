import os
os.environ["SIMPLEX_VERIFICATION_API_KEY"] = "test-key"
os.environ["SIMPLEX_ENVIRONMENT"] = "development"
from fastapi.testclient import TestClient
from app.main import app

client = TestClient(app)

def test_health():
    body = client.get("/health").json()
    assert body["status"] in ("healthy", "degraded")
    assert isinstance(body["face_matching_ready"], bool)

def test_missing_api_key():
    assert client.post("/api/v1/documents/analyse").status_code == 401

def test_invalid_api_key():
    assert client.post("/api/v1/documents/analyse", headers={"X-Api-Key":"wrong"}).status_code == 401

def test_malformed_image():
    response = client.post("/api/v1/documents/analyse", headers={"X-Api-Key":"test-key"}, files={"file":("id.jpg", b"not image")}, data={"requirement_code":"SA_ID","requires_certified_copy":"false","requires_expiry_check":"false"})
    assert response.status_code == 200
    assert response.json()["reason_code"] == "FILE_UNREADABLE"

def test_oversized_request():
    response = client.post("/api/v1/documents/analyse", headers={"X-Api-Key":"test-key"}, files={"file":("id.jpg", b"x"*(10*1024*1024+1))}, data={"requirement_code":"SA_ID","requires_certified_copy":"false","requires_expiry_check":"false"})
    assert response.status_code == 413

def test_face_capture_requires_frame_range():
    response = client.post("/api/v1/faces/verify", headers={"X-Api-Key":"test-key"},
        files=[("reference",("id.jpg",b"bad","image/jpeg")),("frames",("frame.jpg",b"bad","image/jpeg"))],
        data={"payload":'{"session_id":"test","challenges":["BLINK","TURN_LEFT","OPEN_MOUTH"],"timestamps":[1],"stage_indexes":[0]}'})
    assert response.status_code == 400

def test_face_capture_rejects_malformed_metadata():
    files=[("reference",("id.jpg",b"bad","image/jpeg"))]
    files += [("frames",(f"frame-{i}.jpg",b"bad","image/jpeg")) for i in range(20)]
    response = client.post("/api/v1/faces/verify", headers={"X-Api-Key":"test-key"}, files=files, data={"payload":"{"})
    assert response.status_code == 400
