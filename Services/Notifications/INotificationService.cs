namespace SimplexLawFirm.Services.Notifications;
public interface INotificationService
{
    Task QueueAsync(int userId, string type, string title, string message, string? actionUrl, string? deduplicationKey, CancellationToken cancellationToken = default);

    // Compatibility entry point for workflows that do not need a deep link.
    Task SendAsync(int recipientUserId, string title, string message, CancellationToken cancellationToken = default) =>
        QueueAsync(recipientUserId, "Workflow", title, message, null, null, cancellationToken);
}
