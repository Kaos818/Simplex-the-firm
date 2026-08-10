using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimplexLawFirm.Models
{
    public class RetainerActionLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RetainerId { get; set; }

        [ForeignKey("RetainerId")]
        public Retainer? Retainer { get; set; }

        [Required]
        public string? Action { get; set; }

        public string? Details { get; set; }

        public int? UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    

    public class RetainerRenewal
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RetainerId { get; set; }

        [ForeignKey("RetainerId")]
        public Retainer Retainer { get; set; }

        public DateTime? PreviousEndDate { get; set; }

        public DateTime NewEndDate { get; set; }

        public DateTime RenewedDate { get; set; }

        public int RenewedByUserId { get; set; }

        [ForeignKey("RenewedByUserId")]
        public ApplicationUser RenewedByUser { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountAdjustment { get; set; }

        public string Notes { get; set; }
    }
}