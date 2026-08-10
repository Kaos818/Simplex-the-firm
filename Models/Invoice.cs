using System.ComponentModel.DataAnnotations;

namespace SimplexLawFirm.Models
{
    public class Invoice
    {
        public int Id { get; set; }

        [Required]
        public int ClientId { get; set; }
        public Client Client { get; set; }
        public int? RetainerId { get; set; }
        public Retainer Retainer { get; set; }

        public int? CaseId { get; set; }
        public Case Case { get; set; }

        [Required]
        [DataType(DataType.Currency)]
        public decimal Amount { get; set; }

        public DateTime CreatedDate { get; set; }
        public bool IsPaid { get; set; }

        [DataType(DataType.Currency)]
        public decimal TaxAmount { get; set; }

        [DataType(DataType.Currency)]
        public decimal TotalAmount { get; set; }

        [DataType(DataType.Date)]
        public DateTime IssueDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? PaidDate { get; set; }

        public InvoiceStatus Status { get; set; }

        public string Notes { get; set; } = string.Empty;

        public string? PdfPath { get; set; }

        public DateTime CreatedAt { get; set; }

        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();
        public string? Description { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public int? MatterCostEstimateId { get; set; }
        public MatterCostEstimate? MatterCostEstimate { get; set; }
        public bool RequiresEstimateAuthorisation { get; set; }
        public ICollection<InvoiceEstimateAuthorisation> EstimateAuthorisations { get; set; } = new List<InvoiceEstimateAuthorisation>();
        public ICollection<MatterDisbursement> Disbursements { get; set; } = new List<MatterDisbursement>();
    }
}
