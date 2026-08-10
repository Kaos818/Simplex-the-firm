using Microsoft.EntityFrameworkCore;
using MimeKit;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using System.Net;
using System.Text.RegularExpressions;
namespace SimplexLawFirm.Services.Email;
public sealed class EmailOutboxService(ApplicationDbContext db, IConfiguration? configuration = null) : IEmailService
{
    public async Task QueueAsync(string to, string subject, string html, string text, string key, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("An email deduplication key is required.", nameof(key));
        if (string.IsNullOrWhiteSpace(subject)) throw new ArgumentException("An email subject is required.", nameof(subject));
        try { _ = MailboxAddress.Parse(to.Trim()); }
        catch (ParseException ex) { throw new ArgumentException("The recipient email address is invalid.", nameof(to), ex); }
        if (await db.EmailOutboxMessages.AnyAsync(x => x.DeduplicationKey == key, ct)) return;
        var preparedHtml = PrepareHtml(subject.Trim(), html, text, configuration?["Email:PublicBaseUrl"]);
        db.EmailOutboxMessages.Add(new() { ToAddress = to.Trim(), Subject = subject.Trim(), HtmlBody = preparedHtml, TextBody = text ?? "", DeduplicationKey = key, Status = EmailOutboxStatus.Pending });
        db.AuditEntries.Add(new() { EntityType = "EmailOutboxMessage", EntityId = key, Action = "Email queued" });
    }

    internal static string PrepareHtml(string subject, string? html, string? text, string? publicBaseUrl)
    {
        var body = string.IsNullOrWhiteSpace(html)
            ? $"<p>{WebUtility.HtmlEncode(text ?? string.Empty).Replace("\r\n", "<br>").Replace("\n", "<br>")}</p>"
            : html.Trim();
        var root = publicBaseUrl?.Trim().TrimEnd('/');
        if (Uri.TryCreate(root, UriKind.Absolute, out var rootUri) && rootUri.Scheme is "https" or "http")
            body = Regex.Replace(body, "href=(['\"])(/[^'\"]*)\\1", match =>
                $"href={match.Groups[1].Value}{root}{match.Groups[2].Value}{match.Groups[1].Value}", RegexOptions.IgnoreCase);

        body = Regex.Replace(body, "<a(?![^>]*\\bstyle=)([^>]*\\bhref=)",
            "<a style=\"display:inline-block;background:#b28a3e;color:#ffffff!important;text-decoration:none;font-weight:700;padding:12px 20px;border-radius:8px;margin:6px 4px 6px 0;mso-padding-alt:0\"$1", RegexOptions.IgnoreCase);
        body = Regex.Replace(body, "<a(?![^>]*\\btarget=)([^>]*\\bhref=)", "<a target=\"_blank\"$1", RegexOptions.IgnoreCase);
        body = Regex.Replace(body, "<a(?![^>]*\\brel=)([^>]*\\bhref=)", "<a rel=\"noopener noreferrer\"$1", RegexOptions.IgnoreCase);
        var safeSubject = WebUtility.HtmlEncode(subject);
        return $"""<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>{safeSubject}</title></head><body style="margin:0;background:#f3f4f6;font-family:Arial,Helvetica,sans-serif;color:#172033"><div style="display:none;max-height:0;overflow:hidden;opacity:0">{safeSubject}</div><table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#f3f4f6;padding:24px 10px"><tr><td align="center"><table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:640px;background:#ffffff;border-radius:14px;overflow:hidden;box-shadow:0 8px 28px rgba(16,24,40,.12)"><tr><td style="background:#071827;padding:24px 30px;border-bottom:4px solid #b28a3e"><div style="color:#ffffff;font-family:Georgia,serif;font-size:25px;font-weight:700;letter-spacing:.4px">Simplex</div><div style="color:#d8bd82;font-size:11px;font-weight:700;letter-spacing:1.8px;text-transform:uppercase;margin-top:4px">Attorney Chamber</div></td></tr><tr><td style="padding:30px;font-size:16px;line-height:1.65"><h1 style="font-family:Georgia,serif;color:#071827;font-size:25px;line-height:1.25;margin:0 0 20px">{safeSubject}</h1>{body}</td></tr><tr><td style="background:#f8f6f1;border-top:1px solid #e6dfd1;padding:20px 30px;color:#667085;font-size:12px;line-height:1.55">This is an official communication from Simplex Attorney Chamber. If you did not expect this message, contact the firm directly and do not share secure links.<br><span style="color:#8b6b31">Confidential legal communication</span></td></tr></table></td></tr></table></body></html>""";
    }
}
