namespace SimplexLawFirm.Models
{
    public class Enums
    {
    }

    public enum UserRole
    {
        Director,
        Lawyer,
        Paralegal,
        Accountant,
        Client,
        Admin = Director
    }

    public enum CaseStatus
    {
        Open,
        Pending,
        Closed,
        Active,
        Draft,
        Archived
    }

   

    public enum RetainerType
    {
        Fixed,
        Hourly,
        Hybrid,
        CaseBased,
        Subscription
    }

    public enum RetainerStatus
    {
        Draft,
        PendingApproval,
        Rejected,
        Approved,
        SentToClient,
        AwaitingSignature,
        AwaitingPayment,
        Active,
        Completed,
        Cancelled,
        Expired
    }

    public enum EventType
    {
        Meeting,
        CourtAppearance,
        Deadline,
        Task,
        Reminder,
        Appointment,
        FilingDeadline,
        Mediation,
        Consultation,
        Deposition,
        Hearing,
        Other
    }

    public enum EventStatus
    {
        Scheduled,
        Confirmed,
        Cancelled,
        Rescheduled,
        Completed
    }

    public enum AttendeeStatus
    {
        Pending,
        Accepted,
        Declined,
        Tentative
    }

    public enum ReminderMethod
    {
        Email,
        Push,
        Both
    }

    public enum TaskPriority
    {
        Low,
        Medium,
        High,
        Urgent
    }

    public enum TaskStatus
    {
        NotStarted,
        InProgress,
        Completed,
        Blocked,
        OnHold,
        Deferred
    }

    public enum RetainerSource
    {
        AdminCreated = 0,
        ClientPortal = 1,
        TemplateCloned = 2
    }

   

    public enum PaymentScheduleStatus
    {
        Pending = 0,
        Paid = 1,
        Overdue = 2,
        Cancelled = 3
    }
}
