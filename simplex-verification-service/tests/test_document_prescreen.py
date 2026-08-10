from io import BytesIO

import cv2
import numpy as np
from PIL import Image

from app.document_analyser import analyse


def encoded(image):
    ok, value = cv2.imencode(".png", image)
    assert ok
    return value.tobytes()


def document(text="REPUBLIC OF SOUTH AFRICA IDENTITY DOCUMENT 9001015009087"):
    image = np.full((900, 1200, 3), 245, dtype=np.uint8)
    cv2.rectangle(image, (30, 30), (1170, 870), (10, 10, 10), 4)
    cv2.putText(image, text, (70, 300), cv2.FONT_HERSHEY_SIMPLEX, 1.2, (0, 0, 0), 3, cv2.LINE_AA)
    cv2.line(image, (650, 700), (1050, 680), (0, 0, 0), 4)
    return image


def test_dark_document_requests_resubmission():
    result = analyse(encoded(np.zeros((800, 1000, 3), dtype=np.uint8)), "SA_ID", False)
    assert result.decision == "RESUBMIT"
    assert result.reason_code == "DOCUMENT_TOO_DARK"


def test_low_resolution_requests_resubmission():
    result = analyse(encoded(np.full((200, 300, 3), 200, dtype=np.uint8)), "SA_ID", False)
    assert result.reason_code == "LOW_RESOLUTION"


def test_wrong_document_type_requests_resubmission():
    result = analyse(encoded(document("UNRELATED SHOPPING RECEIPT")), "BIRTH_CERTIFICATE", False)
    assert result.reason_code == "WRONG_DOCUMENT_TYPE"


def test_certified_copy_is_sent_to_manual_review():
    result = analyse(encoded(document("REPUBLIC OF SOUTH AFRICA IDENTITY DOCUMENT CERTIFIED COPY COMMISSIONER OF OATHS")), "SA_ID", True)
    assert result.decision == "MANUAL_REVIEW"
    assert "administrator" in result.user_facing_reason.lower()


def test_pdf_first_page_is_rendered_and_screened():
    rgb = cv2.cvtColor(document(), cv2.COLOR_BGR2RGB)
    output = BytesIO()
    Image.fromarray(rgb).save(output, format="PDF")
    result = analyse(output.getvalue(), "SA_ID", False)
    assert result.reason_code != "FILE_UNREADABLE"
