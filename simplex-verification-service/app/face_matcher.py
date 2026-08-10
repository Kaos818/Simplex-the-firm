import cv2
from pathlib import Path

_model_path = Path(__file__).resolve().parent.parent / "models" / "face_recognition_sface_2021dec.onnx"
try:
    _recognizer = cv2.FaceRecognizerSF.create(str(_model_path), "") if _model_path.exists() else None
except cv2.error:
    _recognizer = None

def matcher_ready() -> bool:
    return _recognizer is not None

def align(image, detection):
    if _recognizer is None or detection is None:
        return None
    try:
        return _recognizer.alignCrop(image, detection)
    except cv2.error:
        return None

def compare(reference, live) -> float:
    if _recognizer is None:
        return 0.0
    try:
        return float(_recognizer.match(_recognizer.feature(reference), _recognizer.feature(live), cv2.FaceRecognizerSF_FR_COSINE))
    except cv2.error:
        return 0.0
