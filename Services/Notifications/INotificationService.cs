namespace SimplexLawFirm.Services.Notifications;
public interface INotificationService { Task QueueAsync(int userId, string type, string title, string message, string? actionUrl, string? deduplicationKey, CancellationToken cancellationToken = default); }
