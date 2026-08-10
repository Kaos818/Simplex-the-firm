using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;

namespace SimplexLawFirm.Services.Notifications;
public sealed class NotificationService(ApplicationDbContext db) : INotificationService
{
    public async Task QueueAsync(int userId, string type, string title, string message, string? actionUrl, string? key, CancellationToken ct = default)
    {
        if (actionUrl is not null && (!Uri.TryCreate(actionUrl, UriKind.Relative, out _) || actionUrl.StartsWith("//"))) actionUrl = null;
        if (key is not null && await db.SystemNotifications.AnyAsync(x => x.DeduplicationKey == key, ct)) return;
        db.SystemNotifications.Add(new() { UserId = userId, Type = type, Title = title, Message = message, ActionUrl = actionUrl, DeduplicationKey = key });
    }
}
