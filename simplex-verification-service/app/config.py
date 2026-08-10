import os
from dataclasses import dataclass

@dataclass(frozen=True)
class Settings:
    api_key: str = os.getenv("SIMPLEX_VERIFICATION_API_KEY", "")
    environment: str = os.getenv("SIMPLEX_ENVIRONMENT", "production")
    max_bytes: int = 10 * 1024 * 1024
    face_match: float = float(os.getenv("FACE_MATCH_THRESHOLD", "0.48"))
    manual_review: float = float(os.getenv("FACE_MANUAL_REVIEW_THRESHOLD", "0.36"))

settings = Settings()
