// Models/Retainer.cs (Enhanced)
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimplexLawFirm.Models
{
    public class Retainer
    {
        public int Id { get; set; }

        [Required]
        public int ClientId { get; set; }
        public Client? Client { get; set; }

        public DateTime? UpdatedAt { get; set; }
        public int? CaseId { get; set; }
        public Case? Case { get; set; }

        public int? TemplateId { get; set; }
        public RetainerTemplate? Template { get; set; }

        [Display(Name = "Retainer Title")]
        [Required]
        public string Title { get; set; }

        [Display(Name = "Scope of Work")]
        [DataType(DataType.MultilineText)]
        [Required]
        public string ScopeOfWork { get; set; }

        [Display(Name = "Special Terms")]
        [DataType(DataType.MultilineText)]
        public string SpecialTerms { get; set; }

        public RetainerType? Type { get; set; }
        public RetainerStatus Status { get; set; }

        [Display(Name = "Total Amount")]
        [DataType(DataType.Currency)]
        public decimal Amount { get; set; }

        [Display(Name = "Included Hours")]
        public int IncludedHours { get; set; }

        [Display(Name = "Overage Rate")]
        [DataType(DataType.Currency)]
        public decimal OverageRate { get; set; }

        [Display(Name = "Billing Cycle")]
        public string? BillingCycle { get; set; }

        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Lawyer Notes")]
        [DataType(DataType.MultilineText)]
        public string? LawyerNotes { get; set; }

        [Display(Name = "Admin Notes")]
        [DataType(DataType.MultilineText)]
        public string? AdminNotes { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? SubmittedForApprovalDate { get; set; }
        public int? SubmittedByUserId { get; set; }

        public DateTime? ApprovedDate { get; set; }
        public int? ApprovedByUserId { get; set; }

        public DateTime? SentToClientDate { get; set; }

        public DateTime? SignedDate { get; set; }
        public string? ClientSignatureName { get; set; }
        public string? ClientIPAddress { get; set; }

        public DateTime? PaymentConfirmedDate { get; set; }
        public string? PaymentReference { get; set; }
        public decimal? AmountPaid { get; set; }

        [Display(Name = "Available Retainer Balance")]
        [DataType(DataType.Currency)]
        public decimal AvailableBalance { get; set; }

        [Display(Name = "PDF Document Path")]
        public string? PdfPath { get; set; }

        public string? SignatureToken { get; set; }
        public DateTime? SignatureTokenExpiry { get; set; }

        public bool IsDeleted { get; set; }
        public string? RejectionReason { get; set; }

        // Navigation properties
        public ApplicationUser? ApprovedByUser { get; set; }
        public ApplicationUser? SubmittedByUser { get; set; }

        // Payment schedule
        public ICollection<RetainerPaymentSchedule> PaymentSchedules { get; set; }
        public ICollection<RetainerPayment> Payments { get; set; }

        // Add to your existing Retainer model
        public RetainerSource Source { get; set; } = RetainerSource.AdminCreated;
        public int? AssignedLawyerId { get; set; }
        [ForeignKey("AssignedLawyerId")]
        public ApplicationUser? AssignedLawyer { get; set; }
        public bool RequiresUpfrontPayment { get; set; } = true;
        public int PaymentDueDays { get; set; } = 7;
        public DateTime? ActivatedDate { get; set; }
        public DateTime? RevisionRequestedDate { get; set; }
        public int? RevisionRequestedByUserId { get; set; }
        public string? ClientChangeRequest { get; set; }
        public DateTime? ChangeRequestedDate { get; set; }
        public DateTime? RejectedDate { get; set; }
        public int? RejectedByUserId { get; set; }
        public DateTime? CancelledDate { get; set; }
        public string? CancellationReason { get; set; }
        public int? CancelledByUserId { get; set; }
       
        public ICollection<RetainerActionLog> ActionLogs { get; set; }
       
        public ICollection<RetainerRenewal> Renewals { get; set; }
    }

    public class RetainerPaymentSchedule
    {
        public int Id { get; set; }
        public int RetainerId { get; set; }
        public Retainer? Retainer { get; set; }

        [Display(Name = "Installment Number")]
        public int InstallmentNumber { get; set; }

        [Display(Name = "Due Date")]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        [Display(Name = "Amount Due")]
        [DataType(DataType.Currency)]
        public decimal AmountDue { get; set; }

        [Display(Name = "Status")]
        public PaymentScheduleStatus Status { get; set; }

        public DateTime? PaidDate { get; set; }
        public string? PaymentReference { get; set; }
       
    }

    public class ClientTrustViewModel
    {
        public TrustAccount? TrustAccount { get; set; }
        public List<TrustTransaction> RecentTransactions { get; set; }
        public Client? Client { get; set; }
    }

    public class TrustDepositViewModel
    {
        public int ClientId { get; set; }
        public string? ClientName { get; set; }
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Please enter a valid amount")]
        public decimal Amount { get; set; }
        public string? Description { get; set; }
    }

    public class RetainerDepositViewModel
    {
        public int RetainerId { get; set; }
        public string RetainerTitle { get; set; } = "";
        public decimal CurrentBalance { get; set; }
        [Required, Range(0.01, 100000000, ErrorMessage = "Please enter a valid amount")]
        public decimal Amount { get; set; }
        [Required]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CreditCard;
        [Required, StringLength(100)]
        public string TransactionReference { get; set; } = "";
    }

    public class AdminTrustDetailsViewModel
    {
        public TrustAccount? TrustAccount { get; set; }
        public List<TrustTransaction> Transactions { get; set; } = new List<TrustTransaction>();
        public decimal TotalDeposited { get; set; }
        public decimal TotalWithdrawn { get; set; }
        public decimal CurrentBalance { get; set; }
    }
    public class RetainerPayment
    {
        public int Id { get; set; }
        public int RetainerId { get; set; }
        public Retainer? Retainer { get; set; }

        public int? PaymentScheduleId { get; set; }
        public RetainerPaymentSchedule? PaymentSchedule { get; set; }

        [DataType(DataType.Currency)]
        public decimal Amount { get; set; }

        public DateTime PaymentDate { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string? TransactionReference { get; set; }
        public string? Notes { get; set; }
        public bool IsDepositedToTrust { get; set; }
    }
}
