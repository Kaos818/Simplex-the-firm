using Azure;
using Azure.Communication.Email;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;

namespace SimplexLawFirm.Services.Email;
public sealed class EmailOutboxWorker(IServiceScopeFactory scopes, IOptions<EmailOptions> settings, IWebHostEnvironment environment, ILogger<EmailOutboxWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await DeliverOnceAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Email outbox cycle failed."); }
            try { await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
    internal async Task DeliverOnceAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var batch = await db.EmailOutboxMessages.Where(x => (x.Status == EmailOutboxStatus.Pending || x.Status == EmailOutboxStatus.RetryScheduled) && (x.NextAttemptAtUtc == null || x.NextAttemptAtUtc <= DateTime.UtcNow)).OrderBy(x => x.Id).Take(10).ToListAsync(ct);
        foreach (var item in batch)
        {
            try
            {
                var o = settings.Value;
                if (environment.IsDevelopment() && string.IsNullOrWhiteSpace(o.Host))
                {
                    var root = Path.Combine(environment.ContentRootPath, "App_Data", "EmailSink");
                    Directory.CreateDirectory(root);
                    await File.WriteAllTextAsync(Path.Combine(root, $"{item.Id}.eml"), $"To: {item.ToAddress}\nSubject: {item.Subject}\n\n{item.TextBody}", ct);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(o.FromAddress)) throw new InvalidOperationException("Email sender address is not configured.");
                    if (!string.IsNullOrWhiteSpace(o.AzureCommunicationConnectionString))
                    {
                        var client = new EmailClient(o.AzureCommunicationConnectionString);
                        await client.SendAsync(WaitUntil.Completed, o.FromAddress, item.ToAddress, item.Subject,
                            item.HtmlBody, item.TextBody, ct);
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(o.Host)) throw new InvalidOperationException("Email transport is not configured.");
                        var message = new MimeMessage();
                        message.From.Add(new MailboxAddress(o.FromName, o.FromAddress)); message.To.Add(MailboxAddress.Parse(item.ToAddress)); message.Subject = item.Subject;
                        message.Date = DateTimeOffset.UtcNow;
                        if (!string.IsNullOrWhiteSpace(o.ReplyToAddress)) message.ReplyTo.Add(MailboxAddress.Parse(o.ReplyToAddress));
                        message.Body = new BodyBuilder { HtmlBody = item.HtmlBody, TextBody = item.TextBody }.ToMessageBody();
                        using var smtp = new SmtpClient();
                        await smtp.ConnectAsync(o.Host, o.Port, o.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto, ct);
                        if (!string.IsNullOrWhiteSpace(o.Username)) await smtp.AuthenticateAsync(o.Username, o.Password, ct);
                        await smtp.SendAsync(message, ct); await smtp.DisconnectAsync(true, ct);
                    }
                }
                item.Status = EmailOutboxStatus.Sent; item.SentAtUtc = DateTime.UtcNow; item.LastError = null;
                db.AuditEntries.Add(new() { EntityType = "EmailOutboxMessage", EntityId = item.Id.ToString(), Action = "Email sent" });
            }
            catch (Exception ex)
            {
                item.AttemptCount++;
                item.LastError = Sanitize(ex.Message, settings.Value);
                var permanent = IsPermanentDeliveryFailure(ex);
                item.Status = permanent || item.AttemptCount >= 5 ? EmailOutboxStatus.PermanentlyFailed : EmailOutboxStatus.RetryScheduled;
                item.NextAttemptAtUtc = item.Status == EmailOutboxStatus.PermanentlyFailed ? null : DateTime.UtcNow.AddMinutes(Math.Pow(2, item.AttemptCount));
                if (item.Status == EmailOutboxStatus.PermanentlyFailed)
                    db.AuditEntries.Add(new() { EntityType = "EmailOutboxMessage", EntityId = item.Id.ToString(), Action = "Email permanently failed" });
                logger.LogWarning(ex, "Email delivery failed for outbox message {EmailOutboxMessageId}. Permanent: {Permanent}", item.Id, permanent);
            }
            await db.SaveChangesAsync(ct);
        }
    }
    internal static string Sanitize(string value, EmailOptions options)
    {
        foreach (var secret in new[] { options.Password, options.Username, options.AzureCommunicationConnectionString })
            if (!string.IsNullOrWhiteSpace(secret)) value = value.Replace(secret, "[redacted]", StringComparison.Ordinal);
        return value.Length > 900 ? value[..900] : value;
    }
    internal static bool IsPermanentDeliveryFailure(Exception ex)
    {
        // SMTP 5xx responses mean the server will not accept the message without a corrective action
        // (for example, a disabled/non-existent recipient or a policy rejection). Retrying them wastes
        // quota and obscures the actionable error in the outbox.
        if (ex is SmtpCommandException smtp) return (int)smtp.StatusCode is >= 500 and < 600;
        return false;
    }
}
