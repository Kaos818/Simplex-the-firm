import re
import cv2
import pytesseract
import os
from pathlib import Path
from .certification_prescreener import certification_signals
from .image_validation import decode_image, quality
from .schemas import DocumentResult

KEYWORDS = {
    "SA_ID": ("REPUBLIC OF SOUTH AFRICA", "IDENTITY", "IDENTITY DOCUMENT"),
    "PROOF_OF_ADDRESS": ("ADDRESS", "STATEMENT", "MUNICIPAL", "UTILITY"),
    "BANK_CONFIRMATION": ("BANK", "ACCOUNT", "BRANCH"),
    "BIRTH_CERTIFICATE": ("BIRTH", "CERTIFICATE"),
    "GUARDIANSHIP_DOCUMENT": ("GUARDIAN", "COURT", "MINOR"),
    "TRUST_SUPPORTING_DOCUMENT": ("TRUST", "TRUSTEE"),
}

_tesseract = os.getenv("TESSERACT_CMD")
if not _tesseract:
    windows_default = Path(r"C:\Program Files\Tesseract-OCR\tesseract.exe")
    if windows_default.exists():
        _tesseract = str(windows_default)
if _tesseract:
    pytesseract.pytesseract.tesseract_cmd = _tesseract

def _ocr(image):
    try:
        data = pytesseract.image_to_data(image, output_type=pytesseract.Output.DICT)
        words = [word.strip() for word in data["text"] if word.strip()]
        confidence = [float(value) for value in data["conf"] if float(value) >= 0]
        return " ".join(words), (sum(confidence) / len(confidence) / 100 if confidence else 0.0)
    except (pytesseract.TesseractNotFoundError, RuntimeError):
        return "", 0.0

def analyse(data: bytes, requirement: str, certified: bool, expiry_check: bool = False) -> DocumentResult:
    image = decode_image(data)
    if image is None:
        return DocumentResult(decision="RESUBMIT", reason_code="FILE_UNREADABLE", user_facing_reason="The file could not be read. Upload a valid PDF, JPEG, or PNG.")
    height, width = image.shape[:2]
    if width < 600 or height < 400:
        return DocumentResult(decision="RESUBMIT", reason_code="LOW_RESOLUTION", user_facing_reason="The document resolution is too low. Upload a clearer, larger copy.")
    score, reason = quality(image)
    if reason:
        messages = {"DOCUMENT_BLURRY":"The document is too blurry. Upload a clearer copy.","DOCUMENT_TOO_DARK":"The document is too dark. Retake it in better lighting.","DOCUMENT_OVEREXPOSED":"The document is overexposed. Retake it without glare."}
        return DocumentResult(decision="RESUBMIT", quality_score=score, reason_code=reason, user_facing_reason=messages[reason])
    text, confidence = _ocr(image)
    signals = certification_signals(image, text)
    expected = KEYWORDS.get(requirement, ())
    if text and expected and not any(keyword in text.upper() for keyword in expected):
        return DocumentResult(decision="RESUBMIT", document_type=requirement, quality_score=score, ocr_confidence=confidence,
            reason_code="WRONG_DOCUMENT_TYPE", user_facing_reason=f"This does not appear to be the required {requirement.replace('_',' ').lower()} document.")
    id_pattern = bool(re.search(r"\b\d{13}\b", text))
    signal_names = [name for name, present in (("CERTIFICATION_WORDING",signals["wording"]),("STAMP_LIKE_REGION",signals["stamp"]),("SIGNATURE_LIKE_REGION",signals["signature"]),("ID_NUMBER_PATTERN",id_pattern)) if present]
    if certified:
        reason_code = "CERTIFICATION_UNCERTAIN" if signals["wording"] or signals["stamp"] else "CERTIFICATION_MARK_NOT_FOUND"
        reason_text = "Certification wording or a stamp was detected, but an administrator must confirm the certified copy." if signals["wording"] or signals["stamp"] else "A clear certification mark was not detected. An administrator must review the copy."
        return DocumentResult(decision="MANUAL_REVIEW",document_type=requirement,quality_score=score,ocr_confidence=confidence,
            certification_wording_detected=signals["wording"],stamp_detected=signals["stamp"],signature_detected=signals["signature"],
            reason_code=reason_code,user_facing_reason=reason_text,signals=signal_names)
    if text and confidence < 0.35:
        return DocumentResult(decision="MANUAL_REVIEW",document_type=requirement,quality_score=score,ocr_confidence=confidence,
            reason_code="OCR_LOW_CONFIDENCE",user_facing_reason="Some document text could not be read confidently. An administrator will review it.",signals=signal_names)
    return DocumentResult(decision="PASSED",document_type=requirement,quality_score=score,ocr_confidence=confidence,
        certification_wording_detected=signals["wording"],stamp_detected=signals["stamp"],signature_detected=signals["signature"],
        user_facing_reason="The document passed automated pre-screening and remains subject to administrator review.",signals=signal_names)
