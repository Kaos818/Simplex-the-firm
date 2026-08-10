namespace SimplexLawFirm.Models;

public enum ReimbursementExpenseType
{
    Travel,
    CourtFiling,
    SheriffService,
    Courier,
    Accommodation,
    Meals,
    Parking,
    Printing,
    Other
}

public enum ExpenseClassification { ClientRecoverable, FirmOverhead }
public enum ReimbursementStatus { DraftProofRequired, RefusedValidation, PendingDirector, AutoApproved, Approved, Refused }
public enum ReimbursementActivityType { Appointment, CourtEvent, TimeEntry }

public class ExpensePolicy
{
    public int Id { get; set; }
    public ReimbursementExpenseType ExpenseType { get; set; }
    public decimal PerItemLimit { get; set; }
    public decimal DelegatedApprovalLimit { get; set; }
    public ExpenseClassification DefaultClassification { get; set; }
    public bool IsActive { get; set; } = true;
}

public class MatterExpenseTerm
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public Case Case { get; set; } = null!;
    public ReimbursementExpenseType ExpenseType { get; set; }
    public ExpenseClassification Classification { get; set; }
    public string Reason { get; set; } = "";
}

public class ReimbursementClaim
{
    public int Id { get; set; }
    public string ClaimNumber { get; set; } = "";
    public int CaseId { get; set; }
    public Case Case { get; set; } = null!;
    public int AttorneyId { get; set; }
    public ApplicationUser Attorney { get; set; } = null!;
    public ReimbursementExpenseType ExpenseType { get; set; }
    public DateTime ExpenseDate { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = "";
    public ReimbursementStatus Status { get; set; }
    public ReimbursementActivityType? MatchedActivityType { get; set; }
    public int? MatchedActivityId { get; set; }
    public string? ValidationFailureReason { get; set; }
    public string? ProofOriginalFileName { get; set; }
    public string? ProofRelativePath { get; set; }
    public string? ProofContentType { get; set; }
    public long? ProofSizeBytes { get; set; }
    public string? ProofSha256Hash { get; set; }
    public decimal PolicyLimitSnapshot { get; set; }
    public decimal DelegatedLimitSnapshot { get; set; }
    public bool ExceedsPolicyLimit { get; set; }
    public ExpenseClassification Classification { get; set; }
    public string ClassificationReason { get; set; } = "";
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DecidedAtUtc { get; set; }
    public int? DecidedByUserId { get; set; }
    public ApplicationUser? DecidedByUser { get; set; }
    public string? DecisionReason { get; set; }
    public AttorneyReimbursementPayable? Payable { get; set; }
    public MatterDisbursement? Disbursement { get; set; }
    public ICollection<ReimbursementAuditEntry> AuditEntries { get; set; } = new List<ReimbursementAuditEntry>();
}

public class AttorneyReimbursementPayable
{
    public int Id { get; set; }
    public int ReimbursementClaimId { get; set; }
    public ReimbursementClaim Claim { get; set; } = null!;
    public int AttorneyId { get; set; }
    public ApplicationUser Attorney { get; set; } = null!;
    public decimal Amount { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsPaid { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public string? PaymentReference { get; set; }
}

public class MatterDisbursement
{
    public int Id { get; set; }
    public int ReimbursementClaimId { get; set; }
    public ReimbursementClaim Claim { get; set; } = null!;
    public int CaseId { get; set; }
    public Case Case { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Description { get; set; } = "";
    public DateTime IncurredDate { get; set; }
    public int? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
    public DateTime? InvoicedAtUtc { get; set; }
}

public class ReimbursementAuditEntry
{
    public int Id { get; set; }
    public int ReimbursementClaimId { get; set; }
    public ReimbursementClaim Claim { get; set; } = null!;
    public string Action { get; set; } = "";
    public string Details { get; set; } = "";
    public int? ActorUserId { get; set; }
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
}
