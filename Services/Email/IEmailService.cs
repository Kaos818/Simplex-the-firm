namespace SimplexLawFirm.Services.Email;
public interface IEmailService { Task QueueAsync(string to, string subject, string html, string text, string deduplicationKey, CancellationToken cancellationToken = default); }
