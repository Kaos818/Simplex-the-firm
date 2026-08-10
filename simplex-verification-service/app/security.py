import hmac
from fastapi import Header, HTTPException
from .config import settings

def require_api_key(x_api_key: str | None = Header(default=None)) -> None:
    if not settings.api_key or x_api_key is None or not hmac.compare_digest(x_api_key.encode(), settings.api_key.encode()):
        raise HTTPException(status_code=401, detail="Invalid API key")
