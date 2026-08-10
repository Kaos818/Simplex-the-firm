import cv2
import numpy as np
import pypdfium2 as pdfium

MAX_IMAGE_PIXELS = 20_000_000

def decode_image(data: bytes):
    if not data:
        return None
    if data.startswith(b"%PDF-"):
        try:
            document = pdfium.PdfDocument(data)
            if len(document) < 1 or len(document) > 20:
                return None
            bitmap = document[0].render(scale=2)
            rgb = np.asarray(bitmap.to_pil().convert("RGB"))
            return cv2.cvtColor(rgb, cv2.COLOR_RGB2BGR)
        except Exception:
            return None
    image = cv2.imdecode(np.frombuffer(data, np.uint8), cv2.IMREAD_COLOR)
    if image is None or image.size == 0 or image.shape[0] * image.shape[1] > MAX_IMAGE_PIXELS:
        return None
    return image

def quality(image) -> tuple[float, str | None]:
    gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
    brightness = float(gray.mean())
    contrast = float(gray.std())
    clipped_highlights = float((gray >= 252).mean())
    blur = float(cv2.Laplacian(gray, cv2.CV_64F).var())
    if brightness < 35: return 0.0, "DOCUMENT_TOO_DARK"
    if brightness > 248 and contrast < 12 and clipped_highlights > 0.95: return 0.0, "DOCUMENT_OVEREXPOSED"
    if blur < 45: return 0.0, "DOCUMENT_BLURRY"
    return min(1.0, blur / 500), None
