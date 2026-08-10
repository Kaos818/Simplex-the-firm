from pathlib import Path
import cv2

from .config import settings

_model_path = Path(__file__).resolve().parent.parent / "models" / "face_detection_yunet_2023mar.onnx"
_yunet = cv2.FaceDetectorYN.create(str(_model_path), "", (320, 320), 0.85, 0.3, 5000) if _model_path.exists() else None
_cascade = cv2.CascadeClassifier(cv2.data.haarcascades + "haarcascade_frontalface_default.xml")

def detector_ready() -> bool:
    return _yunet is not None

def detections(image):
    """Return YuNet detections, including landmarks, for biometric matching."""
    if image is None or image.size == 0 or _yunet is None:
        return []
    height, width = image.shape[:2]
    _yunet.setInputSize((width, height))
    _, found = _yunet.detect(image)
    return [] if found is None else list(found)

def faces(image):
    """Return face boxes; YuNet is authoritative and the Haar fallback is development-only."""
    if image is None or image.size == 0:
        return []
    if _yunet is not None:
        return [tuple(map(int, row[:4])) for row in detections(image)]
    if settings.environment != "development":
        return []
    gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
    return list(_cascade.detectMultiScale(gray, 1.1, 5, minSize=(60, 60)))

def face_count(image) -> int:
    return len(faces(image))
