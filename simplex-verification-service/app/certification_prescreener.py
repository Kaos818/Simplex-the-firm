import cv2
import re

CERTIFICATION_TERMS = (
    "CERTIFIED COPY", "CERTIFIED A TRUE COPY", "COMMISSIONER OF OATHS",
    "COMMISSIONER OF OATH", "TRUE COPY OF THE ORIGINAL",
)

def certification_signals(image, text: str) -> dict:
    upper = text.upper()
    wording = any(term in upper for term in CERTIFICATION_TERMS)
    gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
    edges = cv2.Canny(gray, 80, 180)
    contours, _ = cv2.findContours(edges, cv2.RETR_LIST, cv2.CHAIN_APPROX_SIMPLE)
    height, width = gray.shape
    stamp = any(
        0.002 * width * height < cv2.contourArea(contour) < 0.12 * width * height
        and 0.65 <= (lambda box: box[2] / max(1, box[3]))(cv2.boundingRect(contour)) <= 1.45
        for contour in contours
    )
    signature = any(
        (lambda box: box[2] > box[3] * 2.5 and box[2] > width * 0.12 and box[3] < height * 0.12)(cv2.boundingRect(contour))
        for contour in contours
    )
    dates = re.findall(r"\b(?:0?[1-9]|[12]\d|3[01])[-/.](?:0?[1-9]|1[0-2])[-/.](?:19|20)\d{2}\b", text)
    return {"wording": wording, "stamp": stamp, "signature": signature, "dates": dates}
