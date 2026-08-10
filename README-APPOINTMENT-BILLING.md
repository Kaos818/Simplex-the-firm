# Appointment invitations and billing

Client appointments require a client, positive fee, end after start, payment terms, and either a location or meeting link. Invitation GET requests only show confirmation; the anti-forgery protected POST records a single response.

Billing considers only accepted, ended, non-cancelled, positive-fee, unprocessed appointments. A serializable transaction and unique appointment/idempotency records prevent duplicate billing. Sufficient active-retainer trust funds are deducted without allowing a negative balance; otherwise the full fee is invoiced.

Late penalties are one-time, explicit fixed or percentage amounts after the configured grace period. No penalty is invented when the type is `None`.

Configure SMTP through `Email:*` environment variables or user secrets. Development can use `App_Data/EmailSink`; production requires SMTP. Never commit passwords.
