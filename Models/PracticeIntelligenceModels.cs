using System.ComponentModel.DataAnnotations;

namespace SimplexLawFirm.Models;

public enum ForecastResult { Unsuccessful, PartlySuccessful, Successful }
public enum ForecastStatus { Refused, Draft, Locked, Scored }
public enum HandoverStatus { Preparing, Ready, Accepted, Overdue, PendingDirectorReview }
public enum ComplaintStatus { Submitted, Acknowledged, Escalated, Resolved }
public enum ComplaintCategory { Communication, Conduct, Billing, Delay, QualityOfService, Other }
public enum ReassignmentStatus { Approved, HandoverPreparing, Completed, Cancelled }
public enum ForecastRequestStatus { Pending, Fulfilled, Cancelled }

public class CaseForecast
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public Case Case { get; set; } = null!;
    public int AttorneyId { get; set; }
    public ApplicationUser Attorney { get; set; } = null!;
    public ForecastStatus Status { get; set; }
    public decimal? Probability { get; set; }
    public string ProbabilityBand { get; set; } = "";
    public string ConfidenceLevel { get; set; } = "";
    public int ComparableCount { get; set; }
    public string FactorsJson { get; set; } = "[]";
    public string ComparableCasesJson { get; set; } = "[]";
    public string? RefusalReason { get; set; }
    public decimal? AttorneyAssessment { get; set; }
    public bool? AttorneyAgrees { get; set; }
    public string? AttorneyNotes { get; set; }
    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LockedAtUtc { get; set; }
    public ForecastResult? ActualOutcome { get; set; }
    public decimal? AccuracyScore { get; set; }
    public DateTime? ScoredAtUtc { get; set; }
}

public class ForecastCalibration
{
    public int Id { get; set; }
    public int? AttorneyId { get; set; }
    public ApplicationUser? Attorney { get; set; }
    public int ForecastCount { get; set; }
    public decimal MeanAccuracy { get; set; }
    public decimal MeanBias { get; set; }
    public int OptimisticForecastCount { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class ClientForecastRequest
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public Case Case { get; set; } = null!;
    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public ForecastRequestStatus Status { get; set; } = ForecastRequestStatus.Pending;
    public string ClientMessage { get; set; } = "";
    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
    public int? FulfilledByForecastId { get; set; }
    public CaseForecast? FulfilledByForecast { get; set; }
    public DateTime? FulfilledAtUtc { get; set; }
}

public class CaseReassignment
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public Case Case { get; set; } = null!;
    public int OutgoingAttorneyId { get; set; }
    public ApplicationUser OutgoingAttorney { get; set; } = null!;
    public int ReceivingAttorneyId { get; set; }
    public ApplicationUser ReceivingAttorney { get; set; } = null!;
    public int ApprovedByUserId { get; set; }
    public ApplicationUser ApprovedByUser { get; set; } = null!;
    public string Reason { get; set; } = "";
    public ReassignmentStatus Status { get; set; } = ReassignmentStatus.Approved;
    public DateTime ApprovedAtUtc { get; set; } = DateTime.UtcNow;
}

public class CaseHandover
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public Case Case { get; set; } = null!;
    public int OutgoingAttorneyId { get; set; }
    public ApplicationUser OutgoingAttorney { get; set; } = null!;
    public int ReceivingAttorneyId { get; set; }
    public ApplicationUser ReceivingAttorney { get; set; } = null!;
    public HandoverStatus Status { get; set; } = HandoverStatus.Preparing;
    public DateTime DueAtUtc { get; set; }
    public string? Notes { get; set; }
    public string? UrgentMatters { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedForReviewAtUtc { get; set; }
    public DateTime? ReadyAtUtc { get; set; }
    public DateTime? AcceptedAtUtc { get; set; }
    public int CaseReassignmentId { get; set; }
    public CaseReassignment CaseReassignment { get; set; } = null!;
    public ICollection<HandoverItem> Items { get; set; } = new List<HandoverItem>();

    // Director review (the gate between outgoing preparation and receiving acceptance)
    public int? DirectorReviewedByUserId { get; set; }
    public ApplicationUser? DirectorReviewedByUser { get; set; }
    public string? DirectorSummary { get; set; }
    public string? DirectorRiskFlags { get; set; }
    public DateTime? DirectorReviewedAtUtc { get; set; }
    public string? DirectorReturnReason { get; set; }

    // Receiving attorney acceptance
    public bool RiskFlagsAcknowledgedByReceiving { get; set; }
    public string? ReceivingSignature { get; set; }

    public ICollection<HandoverQuery> Queries { get; set; } = new List<HandoverQuery>();
}

public class HandoverItem
{
    public int Id { get; set; }
    public int CaseHandoverId { get; set; }
    public CaseHandover CaseHandover { get; set; } = null!;
    public string Type { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsMandatory { get; set; }
    public bool IsResolved { get; set; }
    public string? ResolutionNote { get; set; }
    public string SourceFingerprint { get; set; } = "";
    public bool AcknowledgedByReceiving { get; set; }

    // The Director may re-open a specific item the outgoing attorney marked resolved, stating what's
    // actually missing. This reopens just that item (not the whole handover's checklist) and notifies
    // the outgoing attorney with the reason, rather than a generic whole-handover return.
    public string? DirectorDisputeNote { get; set; }
}

public class HandoverQuery
{
    public int Id { get; set; }
    public int CaseHandoverId { get; set; }
    public CaseHandover CaseHandover { get; set; } = null!;
    public int RaisedByUserId { get; set; }
    public ApplicationUser RaisedByUser { get; set; } = null!;
    public string Question { get; set; } = "";
    public DateTime RaisedAtUtc { get; set; } = DateTime.UtcNow;
}

public class ClientCorrespondence
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public Case Case { get; set; } = null!;
    public string Subject { get; set; } = "";
    public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? AnsweredAtUtc { get; set; }
}

public enum ComplaintResolutionOutcome { Upheld, PartiallyUpheld, NotUpheld }
public enum AppointmentFormat { InPerson, Video, Phone }
public enum ComplaintAppointmentStatus { Scheduled, Cancelled }

public class ServiceComplaint
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = "";
    public int CaseId { get; set; }
    public Case Case { get; set; } = null!;
    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public ComplaintCategory Category { get; set; }
    public string Description { get; set; } = "";
    public ComplaintStatus Status { get; set; } = ComplaintStatus.Submitted;
    public int RoutedToUserId { get; set; }
    public ApplicationUser RoutedToUser { get; set; } = null!;
    public string RestrictedUserIds { get; set; } = "";
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ResponseDueAtUtc { get; set; }
    public DateTime? AcknowledgedAtUtc { get; set; }
    public bool DuplicateWarningAcknowledged { get; set; }
    public ICollection<ComplaintAttachment> Attachments { get; set; } = new List<ComplaintAttachment>();
    public ICollection<ComplaintAppointment> Appointments { get; set; } = new List<ComplaintAppointment>();

    // Resolution
    public ComplaintResolutionOutcome? Outcome { get; set; }
    public string? MediationSteps { get; set; }
    public string? FormalResponse { get; set; }
    public string? Remedy { get; set; }
    public int? ResolvedByUserId { get; set; }
    public ApplicationUser? ResolvedByUser { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public bool ClientNotifiedOfResolution { get; set; }
}

public class ComplaintAppointment
{
    public int Id { get; set; }
    public int ServiceComplaintId { get; set; }
    public ServiceComplaint ServiceComplaint { get; set; } = null!;
    public DateTime ScheduledAtUtc { get; set; }
    public AppointmentFormat Format { get; set; }
    public string? Notes { get; set; }
    public int BookedByUserId { get; set; }
    public ApplicationUser BookedByUser { get; set; } = null!;
    public DateTime BookedAtUtc { get; set; } = DateTime.UtcNow;
    public ComplaintAppointmentStatus Status { get; set; } = ComplaintAppointmentStatus.Scheduled;
}

public class ComplaintAttachment
{
    public int Id { get; set; }
    public int ServiceComplaintId { get; set; }
    public ServiceComplaint ServiceComplaint { get; set; } = null!;
    public string OriginalFileName { get; set; } = "";
    public string RelativePath { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long SizeBytes { get; set; }
    public string Sha256Hash { get; set; } = "";
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
}

public class StaffServiceRecordEntry
{
    public int Id { get; set; }
    public int StaffUserId { get; set; }
    public ApplicationUser StaffUser { get; set; } = null!;
    public int ServiceComplaintId { get; set; }
    public ServiceComplaint ServiceComplaint { get; set; } = null!;
    public int CaseId { get; set; }
    public ComplaintCategory Category { get; set; }
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
}
