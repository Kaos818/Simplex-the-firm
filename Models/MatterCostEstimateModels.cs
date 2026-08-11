namespace SimplexLawFirm.Models;

public enum CostEstimateStatus { Declined, Locked, Linked }
public enum EstimateGapStatus { Open, Acknowledged }
public enum MatterUrgency { Standard, Urgent, Immediate }
public enum DocumentReadiness { Complete, Partial, Limited }

public class CostEstimateEnquiry
{
    public int Id { get; set; }
    public string PublicToken { get; set; } = Guid.NewGuid().ToString("N");
    public int? ClientId { get; set; }
    public Client? Client { get; set; }
    public string ContactName { get; set; } = "";
    public string Email { get; set; } = "";
    public string MatterType { get; set; } = "";
    public decimal MatterValue { get; set; }
    public MatterUrgency Urgency { get; set; }
    public bool RequiresCourtProceedings { get; set; }
    public DocumentReadiness DocumentReadiness { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ConsultationRequestedAtUtc { get; set; }
    public ICollection<MatterCostEstimate> Estimates { get; set; } = new List<MatterCostEstimate>();
    public ICollection<CostEstimateMessage> Messages { get; set; } = new List<CostEstimateMessage>();
}

public class CostEstimateMessage
{
    public int Id { get; set; }
    public int CostEstimateEnquiryId { get; set; }
    public CostEstimateEnquiry Enquiry { get; set; } = null!;
    public string SenderName { get; set; } = "";
    public string Body { get; set; } = "";
    public bool IsFromAdmin { get; set; }
    public int? AdminUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class MatterCostEstimate
{
    public int Id { get; set; }
    public int CostEstimateEnquiryId { get; set; }
    public CostEstimateEnquiry Enquiry { get; set; } = null!;
    public int Version { get; set; } = 1;
    public CostEstimateStatus Status { get; set; }
    public int ComparableMatterCount { get; set; }
    public decimal ProfessionalFeesLow { get; set; }
    public decimal ProfessionalFeesHigh { get; set; }
    public decimal DisbursementsLow { get; set; }
    public decimal DisbursementsHigh { get; set; }
    public decimal VatLow { get; set; }
    public decimal VatHigh { get; set; }
    public decimal TotalLow { get; set; }
    public decimal TotalHigh { get; set; }
    public decimal PermittedVarianceTolerance { get; set; } = .15m;
    public string RatesSnapshotJson { get; set; } = "[]";
    public string AssumptionsSnapshotJson { get; set; } = "{}";
    public string ComparableMattersSnapshotJson { get; set; } = "[]";
    public string? DeclineReason { get; set; }
    public DateTime LockedAtUtc { get; set; } = DateTime.UtcNow;
    public int? LinkedCaseId { get; set; }
    public Case? LinkedCase { get; set; }
    public DateTime? LinkedAtUtc { get; set; }
}

public class CostEstimateCoverageGap
{
    public int Id { get; set; }
    public string MatterType { get; set; } = "";
    public int ComparableMatterCount { get; set; }
    public string Reason { get; set; } = "";
    public EstimateGapStatus Status { get; set; }
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? AcknowledgedAtUtc { get; set; }
    public int? AcknowledgedByUserId { get; set; }
}

public class InvoiceEstimateAuthorisation
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;
    public int MatterCostEstimateId { get; set; }
    public MatterCostEstimate MatterCostEstimate { get; set; } = null!;
    public decimal InvoiceTotal { get; set; }
    public decimal VariancePercent { get; set; }
    public bool IsApproved { get; set; }
    public int? ApprovedByUserId { get; set; }
    public ApplicationUser? ApprovedByUser { get; set; }
    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAtUtc { get; set; }
    public string? ApprovalReason { get; set; }
}
