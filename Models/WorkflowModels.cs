using System.ComponentModel.DataAnnotations;

namespace SimplexLawFirm.Models;

public enum EmailOutboxStatus { Pending, Sending, Sent, RetryScheduled, PermanentlyFailed }
public enum AppointmentResponseStatus { NotRequired, Pending, Accepted, Rejected, Expired }
public enum AppointmentApprovalStatus { NotRequired, Pending, Approved, Rejected }
public enum LatePenaltyType { None, FixedAmount, Percentage }
public enum AppointmentBillingStatus { Pending, TrustDeducted, Invoiced, Completed, Failed }

public class SystemNotification
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public string Type { get; set; } = "";
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string? ActionUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAtUtc { get; set; }
    public string? DeduplicationKey { get; set; }
}

public class EmailOutboxMessage
{
    public long Id { get; set; }
    public string ToAddress { get; set; } = "";
    public string Subject { get; set; } = "";
    public string HtmlBody { get; set; } = "";
    public string TextBody { get; set; } = "";
    public EmailOutboxStatus Status { get; set; }
    public int AttemptCount { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? NextAttemptAtUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public string? LastError { get; set; }
    public string DeduplicationKey { get; set; } = "";
}

public class AppointmentInvitation
{
    public long Id { get; set; }
    public int CalendarEventId { get; set; }
    public CalendarEvent CalendarEvent { get; set; } = null!;
    public string TokenHash { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
}

public class AppointmentBillingRecord
{
    public long Id { get; set; }
    public int CalendarEventId { get; set; }
    public CalendarEvent CalendarEvent { get; set; } = null!;
    public string IdempotencyKey { get; set; } = "";
    public decimal AppointmentFee { get; set; }
    public decimal CoveredAmount { get; set; }
    public decimal InvoicedAmount { get; set; }
    public AppointmentBillingStatus Status { get; set; }
    public int? TrustTransactionId { get; set; }
    public int? InvoiceId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public string? FailureReason { get; set; }
}

public class InvoicePenalty
{
    public long Id { get; set; }
    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;
    public int CalendarEventId { get; set; }
    public LatePenaltyType Type { get; set; }
    public decimal BasisAmount { get; set; }
    public decimal PenaltyValue { get; set; }
    public decimal Amount { get; set; }
    public DateTime AppliedAtUtc { get; set; }
    public string IdempotencyKey { get; set; } = "";
    public int AppliedByAccountantId { get; set; }
    public string Reason { get; set; } = "";
}

public class AuditEntry
{
    public long Id { get; set; }
    public int? ActorUserId { get; set; }
    public string EntityType { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string Action { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? SafeMetadataJson { get; set; }
}
