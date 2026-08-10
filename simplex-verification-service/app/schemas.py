from pydantic import BaseModel, Field

class DocumentResult(BaseModel):
    decision: str
    document_type: str | None = None
    quality_score: float = 0
    ocr_confidence: float = 0
    certification_wording_detected: bool = False
    stamp_detected: bool = False
    signature_detected: bool = False
    certification_date: str | None = None
    expiry_date: str | None = None
    reason_code: str | None = None
    user_facing_reason: str
    signals: list[str] = Field(default_factory=list)

class FaceResult(BaseModel):
    session_id: str
    decision: str
    liveness_passed: bool
    face_matched: bool
    similarity_score: float | None = None
    valid_frame_ratio: float
    duplicate_frame_ratio: float
    challenge_results: list[dict]
    reason_code: str | None = None
    reason: str | None = None
