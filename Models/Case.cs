using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.Reflection.Metadata;

namespace SimplexLawFirm.Models
{
    public class Case
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Case title is required")]
        public string Title { get; set; }

        public CaseStatus Status { get; set; }

        [Required(ErrorMessage = "Client is required")]
        public int ClientId { get; set; }

        [ValidateNever]
        public Client Client { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        [ValidateNever]
        public string CaseNumber { get; set; }

        public string Description { get; set; } = ""; 
        public string CaseType { get; set; } = "General";
        public decimal EvidenceStrength { get; set; } = 0.5m;
        public ForecastResult? RecordedOutcome { get; set; }
        public string? OutcomeSummary { get; set; }
        public bool OutcomeIsPrivileged { get; set; }
        public bool OutcomeIsConfidential { get; set; }
        public decimal MatterValue { get; set; }
        /// <summary>Total costs awarded to this matter through decided legal-cost recovery claims.</summary>
        public decimal CostRecoveryAwardedTotal { get; set; }
        public decimal? SettlementAmount { get; set; }
        public bool IsCourtReady { get; set; }
        public DateTime? CourtReadyAtUtc { get; set; }
        public bool StrategyReviewRequired { get; set; }

        public int? LawyerId { get; set; }

        [ValidateNever]
        public ApplicationUser Lawyer { get; set; }

        [ValidateNever]
        public ICollection<CaseNote> Notes { get; set; } = new List<CaseNote>();

        [ValidateNever]
        public ICollection<Document> Documents { get; set; } = new List<Document>();
    }
}
