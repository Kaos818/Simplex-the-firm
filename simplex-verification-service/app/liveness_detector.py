import cv2
import numpy as np

ALLOWED_CHALLENGES = {"BLINK", "TURN_LEFT", "TURN_RIGHT", "OPEN_MOUTH"}
_eyes = cv2.CascadeClassifier(cv2.data.haarcascades + "haarcascade_eye_tree_eyeglasses.xml")

def validate_challenges(expected: list[str], submitted: list[str]) -> bool:
    return len(expected) == 3 and expected == submitted and len(set(expected)) == 3 and all(x in ALLOWED_CHALLENGES for x in expected)

def _face_crop(image, box):
    x, y, w, h = map(int, box)
    return image[max(0,y):y+h, max(0,x):x+w]

def evaluate_challenge(challenge: str, images: list, boxes: list) -> dict:
    if len(images) < 5:
        return {"challenge": challenge, "passed": False, "confidence": 0.0, "reason": "TOO_FEW_VALID_FRAMES"}
    if challenge in ("TURN_LEFT", "TURN_RIGHT"):
        centers = np.array([x + w / 2 for x, _, w, _ in boxes])
        widths = np.array([w for _, _, w, _ in boxes])
        normalized_shift = float((centers.max() - centers.min()) / max(1.0, widths.mean()))
        width_change = float((widths.max() - widths.min()) / max(1.0, widths.mean()))
        confidence = max(normalized_shift / 0.12, width_change / 0.10)
        return {"challenge": challenge, "passed": confidence >= 1.0, "confidence": min(1.0, confidence), "reason": None if confidence >= 1 else "HEAD_TURN_NOT_DETECTED"}
    if challenge == "BLINK":
        eye_counts = []
        for image, box in zip(images, boxes):
            crop = _face_crop(image, box)
            upper = crop[:max(1, int(crop.shape[0] * 0.62))]
            eye_counts.append(len(_eyes.detectMultiScale(cv2.cvtColor(upper, cv2.COLOR_BGR2GRAY), 1.1, 4, minSize=(12, 8))))
        saw_open = max(eye_counts) >= 1
        saw_closed = min(eye_counts) == 0
        transitions = sum((eye_counts[i] == 0) != (eye_counts[i - 1] == 0) for i in range(1, len(eye_counts)))
        passed = saw_open and saw_closed and transitions >= 1
        return {"challenge": challenge, "passed": passed, "confidence": 1.0 if passed else 0.25, "reason": None if passed else "BLINK_NOT_DETECTED"}
    mouth_scores = []
    for image, box in zip(images, boxes):
        crop = _face_crop(image, box)
        lower = cv2.cvtColor(crop[int(crop.shape[0] * 0.55):int(crop.shape[0] * 0.92)], cv2.COLOR_BGR2GRAY)
        if lower.size:
            threshold = min(80, float(np.percentile(lower, 25)))
            mouth_scores.append(float((lower < threshold).mean()))
    spread = max(mouth_scores, default=0) - min(mouth_scores, default=0)
    passed = bool(mouth_scores) and max(mouth_scores) > 0.06 and spread > 0.018
    confidence = min(1.0, spread / 0.018) if mouth_scores else 0.0
    return {"challenge": challenge, "passed": passed, "confidence": confidence, "reason": None if passed else "MOUTH_OPENING_NOT_DETECTED"}
