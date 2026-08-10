# Production email delivery

The application uses SMTP with a durable database outbox. The SMTP configuration must be supplied as Azure App Service application settings; no credentials belong in `appsettings.json`.

Set these production application settings:

```
ConnectionStrings__DefaultConnection=<Azure SQL connection string>
Email__Host=<transactional provider SMTP host>
Email__Port=587
Email__UseStartTls=true
Email__Username=<SMTP username>
Email__Password=<SMTP password or API key>
Email__FromAddress=<a verified sender address>
Email__FromName=Simplex Law Firm
Email__ReplyToAddress=<monitored support address>
Email__PublicBaseUrl=https://simplexlawfirm-fvcdh3edbdadbuft.southafricanorth-01.azurewebsites.net
Verification__BaseUrl=http://127.0.0.1:8091
Verification__ApiKey=<verification service key>
```

Use a transactional provider with a verified Simplex-owned domain, SPF, DKIM and DMARC in place. Do not use a personal Gmail mailbox as the production sender: it cannot establish a business sending domain and Gmail can reject mail under recipient or anti-abuse policy.

Before publishing, rotate the previously committed database password and SMTP app password. Add the replacement values only in Azure App Service settings. After deployment, submit one confirmation email to a controlled external mailbox, verify the confirmation link uses the HTTPS public URL, and check the Email outbox page: it must show `Sent`. A `PermanentlyFailed` message contains a sanitised provider error and needs a configuration, sender reputation, or recipient-address correction; it is intentionally not retried.
