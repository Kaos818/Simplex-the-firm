# Simplex local verification service

This service performs local pre-screening only. It never makes the final beneficiary decision and never releases funds.

Set `SIMPLEX_VERIFICATION_API_KEY`, then run:

`uvicorn app.main:app --host 127.0.0.1 --port 8091`

Production facial matching fails closed until approved YuNet and SFace model files are installed in `models/`.
