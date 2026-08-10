# Beneficiary workflow

Clients create beneficiaries connected to their own client record, then send a random 32-byte invitation. Only the SHA-256 hash is stored. Invitations expire after 72 hours, are single-use, and previous unused invitations are revoked on renewal.

Documents are stored beneath `App_Data/SecureBeneficiaryDocuments`, outside `wwwroot`. PDF/JPEG/PNG signatures, 10 MB size, PDF page count, generated paths, and canonical containment are enforced. Local ML results never assert legal certification authenticity.

Submission and approval require the latest required documents to be present without resubmission status. Facial results must be verified or explicitly overridden by an administrator with a reason. Configure `Verification:BaseUrl`, `Verification:TimeoutSeconds`, and secret `Verification:ApiKey`.

Apply migrations with `dotnet ef database update`. The legacy `SimplexLawFirmLocal` database was originally created without migration history; use a clean database or establish an audited migration baseline before production conversion.

## Development-only Kaotic verification scenario

The Kaotic Being seed is created only when `ASPNETCORE_ENVIRONMENT=Development` and the `Seed:KaoticPortalPassword` user secret is configured. It has a test-only face reference under `Data/SeedAssets`, documentation marked complete, and starts at facial verification. Sign in through `/BeneficiaryPortal/Login`; this creates only the limited beneficiary session and does not grant a client or member account. Never configure this seed password in a production environment.
