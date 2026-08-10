// Models/RetainerTemplate.cs
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using SimplexLawFirm.Models;
using System.ComponentModel.DataAnnotations;


namespace SimplexLawFirm.Models
{
    public class RetainerTemplate
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Service Name")]
        public string Name { get; set; }

        [Display(Name = "Service Description")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [Display(Name = "What's Included")]
        [DataType(DataType.MultilineText)]
        public string Inclusions { get; set; }

        [Display(Name = "What's NOT Included")]
        [DataType(DataType.MultilineText)]
        public string Exclusions { get; set; }

        [Display(Name = "Base Price")]
        [DataType(DataType.Currency)]
        public decimal BasePrice { get; set; }

        [Display(Name = "Price Display")]
        public string PriceDisplay { get; set; }

        public RetainerType Type { get; set; }

        public int IncludedHours { get; set; }

        [Display(Name = "Overage Rate")]
        [DataType(DataType.Currency)]
        public decimal OverageRate { get; set; }

        [Display(Name = "Billing Cycle")]
        public string BillingCycle { get; set; }

        public bool IsPublic { get; set; }

        [Display(Name = "Service Category")]
        public string Category { get; set; }

        [Display(Name = "Estimated Duration")]
        public string EstimatedDuration { get; set; }

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;

        [Display(Name = "Requires Upfront Payment")]
        public bool RequiresUpfrontPayment { get; set; }

        [Display(Name = "Upfront Percentage")]
        public int? UpfrontPercentage { get; set; }

        [Display(Name = "Allow Installments")]
        public bool AllowInstallments { get; set; }

        [Display(Name = "Maximum Installments")]
        public int? MaxInstallments { get; set; }

        [Display(Name = "Terms and Conditions")]
        [DataType(DataType.MultilineText)]
        public string TermsAndConditions { get; set; }


        [ValidateNever]
        public ICollection<ClientRequest> ClientRequests { get; set; }
    }
}