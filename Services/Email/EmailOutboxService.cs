using Microsoft.EntityFrameworkCore;
using MimeKit;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
namespace SimplexLawFirm.Services.Email;
public sealed class EmailOutboxService(ApplicationDbContext db) : IEmailService
{
    public async Task QueueAsync(string to, string subject, string html, string text, string key, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("An email deduplication key is required.", nameof(key));
        if (string.IsNullOrWhiteSpace(subject)) throw new ArgumentException("An email subject is required.", nameof(subject));
        try { _ = MailboxAddress.Parse(to.Trim()); }
        catch (ParseException ex) { throw new ArgumentException("The recipient email address is invalid.", nameof(to), ex); }
        if (await db.EmailOutboxMessages.AnyAsync(x => x.DeduplicationKey == key, ct)) return;
        db.EmailOutboxMessages.Add(new() { ToAddress = to.Trim(), Subject = subject.Trim(), HtmlBody = html ?? "", TextBody = text ?? "", DeduplicationKey = key, Status = EmailOutboxStatus.Pending });
        db.AuditEntries.Add(new() { EntityType = "EmailOutboxMessage", EntityId = key, Action = "Email queued" });
    }
}
