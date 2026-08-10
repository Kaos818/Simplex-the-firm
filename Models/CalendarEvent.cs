using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimplexLawFirm.Models
{
    public class CalendarEvent
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Event Title")]
        [MaxLength(200)]
        public string Title { get; set; }

        [Display(Name = "Description")]
        [DataType(DataType.MultilineText)]
        [MaxLength(2000)]
        public string Description { get; set; }

        [Display(Name = "Location")]
        [MaxLength(200)]
        public string Location { get; set; }

        [Required]
        [Display(Name = "Start Date & Time")]
        public DateTime StartDateTime { get; set; }

        [Required]
        [Display(Name = "End Date & Time")]
        public DateTime EndDateTime { get; set; }

        [Display(Name = "All Day Event")]
        public bool IsAllDay { get; set; }

        [Display(Name = "Event Type")]
        public EventType Type { get; set; }

        [Display(Name = "Event Status")]
        public EventStatus Status { get; set; }

        [Display(Name = "Color Code")]
        [MaxLength(20)]
        public string Color { get; set; } = "#16a394";

        [Display(Name = "Created By")]
        public int? CreatedByUserId { get; set; }

        [ForeignKey("CreatedByUserId")]
        public ApplicationUser CreatedByUser { get; set; }

        [Display(Name = "Assigned To")]
        public int? AssignedToUserId { get; set; }

        [ForeignKey("AssignedToUserId")]
        public ApplicationUser AssignedToUser { get; set; }

        [Display(Name = "Related Case")]
        public int? CaseId { get; set; }

        [ForeignKey("CaseId")]
        public Case Case { get; set; }

        [Display(Name = "Related Retainer")]
        public int? RetainerId { get; set; }

        [ForeignKey("RetainerId")]
        public Retainer Retainer { get; set; }

        public int? ClientId { get; set; }
        public Client? Client { get; set; }
        public decimal? AppointmentFee { get; set; }
        public int PaymentDueDays { get; set; } = 7;
        public LatePenaltyType LatePenaltyType { get; set; }
        public decimal LatePenaltyValue { get; set; }
        public int LatePenaltyGraceDays { get; set; }
        public AppointmentResponseStatus ClientResponseStatus { get; set; }
        public AppointmentApprovalStatus LawyerApprovalStatus { get; set; } = AppointmentApprovalStatus.NotRequired;
        public DateTime? ClientRespondedAtUtc { get; set; }
        public string? ClientResponseComments { get; set; }
        public bool BillingProcessed { get; set; }
        public DateTime? BillingProcessedAtUtc { get; set; }
        public int? GeneratedInvoiceId { get; set; }
        public Invoice? GeneratedInvoice { get; set; }
        [Timestamp] public byte[] RowVersion { get; set; } = [];

        [Display(Name = "Parent Event")]
        public int? ParentEventId { get; set; }

        [ForeignKey("ParentEventId")]
        public CalendarEvent ParentEvent { get; set; }

        [Display(Name = "Recurrence Rule")]
        [MaxLength(500)]
        public string RecurrenceRule { get; set; } = ""; // For recurring events (RRULE format)

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Updated At")]
        public DateTime? UpdatedAt { get; set; }

        [Display(Name = "Reminder Sent At")]
        public DateTime? ReminderSentAt { get; set; }

        [Display(Name = "Actual Start Time")]
        public DateTime? ActualStartTime { get; set; }

        [Display(Name = "Actual End Time")]
        public DateTime? ActualEndTime { get; set; }

        [Display(Name = "Meeting Link")]
        [MaxLength(500)]
        public string MeetingLink { get; set; } = "";

        [Display(Name = "Is Private")]
        public bool IsPrivate { get; set; }

        [Display(Name = "Completion Notes")]
        [DataType(DataType.MultilineText)]
        [MaxLength(1000)]
        public string CompletionNotes { get; set; } = "";

        // Navigation properties
        public ICollection<EventAttendee> Attendees { get; set; }
        public ICollection<EventReminder> Reminders { get; set; }

        [InverseProperty("ParentEvent")]
        public ICollection<CalendarEvent> ChildEvents { get; set; }
    }

    public class EventAttendee
    {
        public int Id { get; set; }

        public int EventId { get; set; }
        public CalendarEvent Event { get; set; }

        public int UserId { get; set; }
        public ApplicationUser User { get; set; }

        public AttendeeStatus Status { get; set; }

        public DateTime? RespondedAt { get; set; }

        [Display(Name = "Response Status")]
        [MaxLength(50)]
        public string ResponseStatus { get; set; } = "Pending";

        [Display(Name = "Response Date")]
        public DateTime? ResponseDate { get; set; }

        [Display(Name = "Comments")]
        [MaxLength(500)]
        public string Comments { get; set; } = "";

        [Display(Name = "Is Optional")]
        public bool IsOptional { get; set; }

        [Display(Name = "Invited At")]
        public DateTime InvitedAt { get; set; } = DateTime.Now;

        [Display(Name = "Reminder Sent")]
        public bool ReminderSent { get; set; }
    }

    public class EventReminder
    {
        public int Id { get; set; }

        public int EventId { get; set; }
        public CalendarEvent Event { get; set; }
        [Display(Name = "Reminder Minutes Before")]

        public int ReminderMinutesBefore { get; set; }

        public int MinutesBefore { get; set; }

        public ReminderMethod Method { get; set; }

        [Display(Name = "Is Sent")]
        public bool IsSent { get; set; }

        [Display(Name = "Sent At")]
        public DateTime? SentAt { get; set; }

        [Display(Name = "Sent To")]
        [MaxLength(500)]
        public string SentTo { get; set; } = "";

        [Display(Name = "Reminder Type")]
        [MaxLength(50)]
        public string ReminderType { get; set; } = "Email";

        [Display(Name = "Error Message")]
        [MaxLength(500)]
        public string ErrorMessage { get; set; } = "";
    }

    
    public class TaskComment
    {
        public int Id { get; set; }

        public int TaskId { get; set; }
        public TaskItem Task { get; set; }

        public int UserId { get; set; }
        public ApplicationUser User { get; set; }

        public string Comment { get; set; } = "";

        public DateTime CreatedAt { get; set; }
        [Display(Name = "Is Internal")]
        public bool IsInternal { get; set; }

    }

    public class TaskAttachment
    {
        public int Id { get; set; }

        public int TaskId { get; set; }
        public TaskItem Task { get; set; }

        public string FileName { get; set; } = "";

        public string FilePath { get; set; } = "";

        public DateTime UploadedAt { get; set; }
    }

    

    
}
