using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace SimplexLawFirm.Models
{
    public class Payment
    {
        public int Id { get; set; }

        [Required]
        public int InvoiceId { get; set; }
        [ValidateNever] public Invoice Invoice { get; set; }

        [Required]
        public int ClientId { get; set; }
        [ValidateNever] public Client Client { get; set; }

        [Required]
        [DataType(DataType.Currency)]
        public decimal Amount { get; set; }

        [DataType(DataType.Date)]
        public DateTime PaymentDate { get; set; }

        public PaymentMethod PaymentMethod { get; set; }

        public string TransactionReference { get; set; }

        public string? Notes { get; set; }

        public bool IsTrustAccountDeposit { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class TimeEntry
    {
        public int Id { get; set; }

        [Required]
        public int LawyerId { get; set; }
        [ValidateNever] public ApplicationUser Lawyer { get; set; }

        [Required]
        public int CaseId { get; set; }
        [ValidateNever] public Case Case { get; set; }

        public int? RetainerId { get; set; }
        [ValidateNever] public Retainer Retainer { get; set; }

        public int? InvoiceId { get; set; }
        [ValidateNever] public Invoice Invoice { get; set; }

        [Required]
        public string? Description { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime Date { get; set; }

        [Required]
        public decimal Hours { get; set; }

        [Required]
        [DataType(DataType.Currency)]
        public decimal HourlyRate { get; set; }

        [DataType(DataType.Currency)]
        public decimal TotalAmount { get; set; }

        public bool IsBillable { get; set; } = true;

        public bool IsBilled { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
