using System.ComponentModel.DataAnnotations;

namespace SimplexLawFirm.Models;

public enum ClientSafeguard { Interpreter, SupportPerson, ExtendedMeetingTime }
public enum VulnerableFlagStatus { PendingReview, Confirmed, Escalated, Removed }

public sealed class VulnerableClientFlag
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public ClientSafeguard Safeguard { get; set; }
    [Required, MaxLength(1000)] public string Reason { get; set; } = "";
    [MaxLength(120)] public string? LanguageRequired { get; set; }
    public VulnerableFlagStatus Status { get; set; } = VulnerableFlagStatus.PendingReview;
    public int RaisedByAttorneyId { get; set; }
    public ApplicationUser RaisedByAttorney { get; set; } = null!;
    public DateTime RaisedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ReviewDueAtUtc { get; set; }
    public DateTime? NextReviewAtUtc { get; set; }
    public int? ReviewedByDirectorId { get; set; }
    public ApplicationUser? ReviewedByDirector { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    [MaxLength(1000)] public string? ReviewNote { get; set; }
    public DateTime LastChangedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RemovedAtUtc { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = [];
}

public sealed class VulnerableFlagAcknowledgement
{
    public long Id { get; set; }
    public int VulnerableClientFlagId { get; set; }
    public VulnerableClientFlag Flag { get; set; } = null!;
    public int CaseId { get; set; }
    public int StaffUserId { get; set; }
    public DateTime AcknowledgedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class AppointmentInterpreterAssignment
{
    public int Id { get; set; }
    public int CalendarEventId { get; set; }
    public CalendarEvent CalendarEvent { get; set; } = null!;
    [Required, MaxLength(160)] public string InterpreterName { get; set; } = "";
    [Required, MaxLength(120)] public string Language { get; set; } = "";
    [MaxLength(200)] public string? ContactDetails { get; set; }
    public int AssignedByUserId { get; set; }
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class AppointmentSupportPersonAssignment
{
    public int Id { get; set; }
    public int CalendarEventId { get; set; }
    public CalendarEvent CalendarEvent { get; set; } = null!;
    [Required, MaxLength(160)] public string SupportPersonName { get; set; } = "";
    [MaxLength(120)] public string? Relationship { get; set; }
    public int RecordedByUserId { get; set; }
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class ClientSupportSession
{
    public long Id { get; set; }
    public int ClientId { get; set; }
    public int AuthorisedByStaffUserId { get; set; }
    [Required, MaxLength(160)] public string SupportPersonName { get; set; } = "";
    [Required, MaxLength(500)] public string Purpose { get; set; } = "";
    public DateTime StartsAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
}
