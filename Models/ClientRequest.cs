// Models/ClientRequest.cs (Enhanced)
using System.ComponentModel.DataAnnotations;

namespace SimplexLawFirm.Models
{
    public class ClientRequest
    {
        public int Id { get; set; }

        [Required]
        public int ClientId { get; set; }
        public Client? Client { get; set; }

        [Required]
        public int TemplateId { get; set; }
        public RetainerTemplate Template { get; set; }

        public string? Status { get; set; } // Pending, InReview, Converted, Declined

        [Display(Name = "Additional Information")]
        [DataType(DataType.MultilineText)]
        public string? ClientNotes { get; set; }

        [Display(Name = "Preferred Contact Method")]
        public string? PreferredContact { get; set; }

        [Display(Name = "Urgency Level")]
        public string? Urgency { get; set; } // Low, Medium, High, Urgent

        public DateTime CreatedDate { get; set; }

        public DateTime? ReviewedDate { get; set; }

        public int? ConvertedToRetainerId { get; set; }

        [Display(Name = "Admin Notes")]
        public string? AdminNotes { get; set; }
    }
}