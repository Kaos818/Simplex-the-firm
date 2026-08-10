import json
import os
import sys
from pathlib import Path

import cv2
from fastapi.testclient import TestClient

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))
os.environ.setdefault("SIMPLEX_VERIFICATION_API_KEY", "test-key")
os.environ.setdefault("SIMPLEX_ENVIRONMENT", "development")

from app.face_detector import faces
from app.face_matcher import compare
from app.image_validation import quality
from app.main import app

reference_path, live_path = map(Path, sys.argv[1:3])
reference_image = cv2.imread(str(reference_path))
live_image = cv2.imread(str(live_path))
if reference_image is None or live_image is None:
    raise SystemExit("Both inputs must be readable images.")

print(json.dumps({
    "reference_shape": list(reference_image.shape),
    "reference_faces": len(faces(reference_image)),
    "reference_quality": quality(reference_image),
    "live_shape": list(live_image.shape),
    "live_faces": len(faces(live_image)),
    "live_quality": quality(live_image),
}))

reference_box = faces(reference_image)[0]
live_box = faces(live_image)[0]
rx, ry, rw, rh = reference_box
lx, ly, lw, lh = live_box
reference_crop = cv2.resize(reference_image[ry:ry + rh, rx:rx + rw], (112, 112))
live_crop = cv2.resize(live_image[ly:ly + lh, lx:lx + lw], (112, 112))
print(json.dumps({
    "same_image_similarity": compare(reference_crop, reference_crop),
    "cross_image_similarity": compare(reference_crop, live_crop),
}))

files = [("reference", ("reference.jpg", reference_path.read_bytes(), "image/jpeg"))]
for index in range(30):
    files.append(("frames", (f"frame-{index}.jpg", live_path.read_bytes(), "image/jpeg")))
payload = {
    "session_id": "manual-image-probe",
    "challenges": ["TURN_LEFT", "BLINK", "OPEN_MOUTH"],
    "timestamps": list(range(1000, 31000, 1000)),
    "stage_indexes": [0] * 10 + [1] * 10 + [2] * 10,
}
response = TestClient(app).post(
    "/api/v1/faces/verify",
    headers={"X-Api-Key": os.environ["SIMPLEX_VERIFICATION_API_KEY"]},
    files=files,
    data={"payload": json.dumps(payload)},
)
print(response.status_code)
print(json.dumps(response.json(), indent=2))
