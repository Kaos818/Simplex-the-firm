using System.ComponentModel.DataAnnotations;

namespace SimplexLawFirm.Models;

public enum CaseDocumentRequestStatus { Requested, Submitted, Accepted, MoreInformationRequired, Overdue }
public enum ClientResponseRequestType { Information, Document, Approval, Decision }
public enum ClientResponseDelayAction { FollowUp, Escalated, DeadlineExtended }

public sealed class ClientResponseSlaRule
{
    public int Id { get; set; }
    public ClientResponseRequestType RequestType { get; set; }
    [Range(1, 90)] public int ResponseDays { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public int? UpdatedByUserId { get; set; }
    public ApplicationUser? UpdatedByUser { get; set; }
}

public class CaseDocumentRequest
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public Case Case { get; set; } = null!;
    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public int RequestedByAttorneyId { get; set; }
    public ApplicationUser RequestedByAttorney { get; set; } = null!;
    [MaxLength(200)] public string Title { get; set; } = "";
    [MaxLength(2000)] public string Instructions { get; set; } = "";
    [MaxLength(1000)] public string RequiredDocuments { get; set; } = "";
    public ClientResponseRequestType RequestType { get; set; } = ClientResponseRequestType.Document;
    public int ResponseSlaDaysSnapshot { get; set; }
    public DateTime DueAtUtc { get; set; }
    public CaseDocumentRequestStatus Status { get; set; } = CaseDocumentRequestStatus.Requested;
    [MaxLength(2000)] public string? ReviewFeedback { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public DateTime? OverdueAtUtc { get; set; }
    public DateTime? LastDelayActionAtUtc { get; set; }
    public ClientResponseDelayAction? LastDelayAction { get; set; }
    [MaxLength(2000)] public string? LastDelayActionNote { get; set; }
    [MaxLength(4000)] public string? ClientResponseText { get; set; }
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}

public enum HandoverRequestStatus { PendingDirector, Approved, Declined }

public class CaseHandoverRequest
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public Case Case { get; set; } = null!;
    public int RequestedByAttorneyId { get; set; }
    public ApplicationUser RequestedByAttorney { get; set; } = null!;
    [MaxLength(2000)] public string Reason { get; set; } = "";
    [MaxLength(2000)] public string? DirectorResponse { get; set; }
    public HandoverRequestStatus Status { get; set; } = HandoverRequestStatus.PendingDirector;
    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAtUtc { get; set; }
    public int? ReviewedByDirectorId { get; set; }
    public ApplicationUser? ReviewedByDirector { get; set; }
    public int? CaseHandoverId { get; set; }
    public CaseHandover? CaseHandover { get; set; }
}
