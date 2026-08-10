import cv2
import numpy as np
import pytest

from app import face_verifier

CHALLENGES = ["BLINK", "TURN_LEFT", "OPEN_MOUTH"]

def jpeg(value: int = 120) -> bytes:
    image = np.full((180, 180, 3), value, dtype=np.uint8)
    ok, encoded = cv2.imencode(".jpg", image)
    assert ok
    return encoded.tobytes()

def capture(count: int = 20):
    frames = [jpeg(30 + (index * 9) % 190) for index in range(count)]
    return frames, {"session_id": "test-session", "challenges": CHALLENGES,
                    "timestamps": list(range(1_000, 1_000 + count)),
                    "stage_indexes": [min(2, index // 7) for index in range(count)]}

def valid_pipeline(monkeypatch, score: float = 0.6):
    detection = np.array([40, 35, 90, 100, 55, 65, 105, 65, 80, 85, 62, 110, 98, 110, 0.99], dtype=np.float32)
    monkeypatch.setattr(face_verifier, "detector_ready", lambda: True)
    monkeypatch.setattr(face_verifier, "matcher_ready", lambda: True)
    monkeypatch.setattr(face_verifier, "detections", lambda image: [detection])
    monkeypatch.setattr(face_verifier, "align", lambda image, detection: image)
    monkeypatch.setattr(face_verifier, "evaluate_challenge", lambda challenge, images, boxes:
                        {"challenge": challenge, "passed": True, "confidence": 1.0, "reason": None})
    monkeypatch.setattr(face_verifier, "compare", lambda reference, live: score)

def test_zero_reference_faces_requires_manual_review(monkeypatch):
    monkeypatch.setattr(face_verifier, "detector_ready", lambda: True)
    monkeypatch.setattr(face_verifier, "matcher_ready", lambda: True)
    monkeypatch.setattr(face_verifier, "detections", lambda image: [])
    frames, payload = capture()
    result = face_verifier.verify(jpeg(), frames, payload)
    assert result["decision"] == "MANUAL_REVIEW"
    assert result["face_matched"] is False

def test_missing_approved_model_requires_manual_review(monkeypatch):
    monkeypatch.setattr(face_verifier, "detector_ready", lambda: False)
    frames, payload = capture()
    result = face_verifier.verify(jpeg(), frames, payload)
    assert result["decision"] == "MANUAL_REVIEW"
    assert result["reason_code"] == "LOCAL_MODEL_UNAVAILABLE"

def test_multiple_live_faces_are_rejected(monkeypatch):
    calls = {"count": 0}
    def detected(_):
        calls["count"] += 1
        return [detection] if calls["count"] == 1 else [detection, detection]
    detection = np.array([40, 35, 90, 100, 55, 65, 105, 65, 80, 85, 62, 110, 98, 110, 0.99], dtype=np.float32)
    monkeypatch.setattr(face_verifier, "detector_ready", lambda: True)
    monkeypatch.setattr(face_verifier, "matcher_ready", lambda: True)
    monkeypatch.setattr(face_verifier, "detections", detected)
    frames, payload = capture()
    result = face_verifier.verify(jpeg(), frames, payload)
    assert result["decision"] == "INVALID_CAPTURE"
    assert result["reason_code"] == "MULTIPLE_FACES"

def test_duplicate_frames_fail_liveness(monkeypatch):
    valid_pipeline(monkeypatch)
    frames, payload = capture()
    frames = [jpeg(100)] * len(frames)
    result = face_verifier.verify(jpeg(), frames, payload)
    assert result["decision"] == "FAILED_LIVENESS"
    assert result["reason_code"] == "STATIC_OR_DUPLICATE_FRAMES"

@pytest.mark.parametrize("failed_challenge", ["BLINK", "TURN_LEFT", "TURN_RIGHT", "OPEN_MOUTH"])
def test_each_failed_active_challenge_fails_liveness(monkeypatch, failed_challenge):
    challenges = [failed_challenge, "BLINK" if failed_challenge != "BLINK" else "TURN_LEFT", "OPEN_MOUTH" if failed_challenge != "OPEN_MOUTH" else "TURN_RIGHT"]
    assert len(set(challenges)) == 3
    valid_pipeline(monkeypatch)
    monkeypatch.setattr(face_verifier, "evaluate_challenge", lambda challenge, images, boxes:
                        {"challenge": challenge, "passed": challenge != failed_challenge, "confidence": 0.0, "reason": "NOT_DETECTED"})
    frames, payload = capture()
    payload["challenges"] = challenges
    result = face_verifier.verify(jpeg(), frames, payload)
    assert result["decision"] == "FAILED_LIVENESS"
    assert result["liveness_passed"] is False

@pytest.mark.parametrize("score,decision", [(0.2, "FACE_NOT_MATCHED"), (0.4, "MANUAL_REVIEW"), (0.7, "VERIFIED")])
def test_one_to_one_match_thresholds_map_safely(monkeypatch, score, decision):
    valid_pipeline(monkeypatch, score)
    frames, payload = capture()
    result = face_verifier.verify(jpeg(), frames, payload)
    assert result["decision"] == decision
    assert result["liveness_passed"] is True
    assert result["face_matched"] is (decision == "VERIFIED")

def test_discontinuous_face_movement_fails_liveness(monkeypatch):
    calls = {"count": 0}
    def moving(_):
        calls["count"] += 1
        if calls["count"] == 1:
            return [np.array([40, 35, 60, 80, 50, 55, 90, 55, 70, 75, 55, 95, 85, 95, 0.99], dtype=np.float32)]
        x = 5 if calls["count"] % 2 else 110
        return [np.array([x, 35, 60, 80, x + 10, 55, x + 50, 55, x + 30, 75, x + 15, 95, x + 45, 95, 0.99], dtype=np.float32)]
    monkeypatch.setattr(face_verifier, "detector_ready", lambda: True)
    monkeypatch.setattr(face_verifier, "matcher_ready", lambda: True)
    monkeypatch.setattr(face_verifier, "detections", moving)
    monkeypatch.setattr(face_verifier, "align", lambda image, detection: image)
    monkeypatch.setattr(face_verifier, "compare", lambda reference, live: 0.7)
    frames, payload = capture()
    result = face_verifier.verify(jpeg(), frames, payload)
    assert result["decision"] == "FAILED_LIVENESS"
    assert result["reason_code"] == "DISCONTINUOUS_FACE_MOVEMENT"

def test_browser_altered_challenge_list_is_rejected(monkeypatch):
    valid_pipeline(monkeypatch)
    frames, payload = capture()
    payload["challenges"] = ["BLINK", "BLINK", "OPEN_MOUTH"]
    result = face_verifier.verify(jpeg(), frames, payload)
    assert result["decision"] == "INVALID_CAPTURE"
