using System.ComponentModel.DataAnnotations;

namespace SimplexLawFirm.DTOs.CourtFiling
{
    public class ReturnCourtFilingDto
    {
        [Required]
        public int SubmissionId { get; set; }

        [Required, MaxLength(4000)]
        public string Reason { get; set; } = string.Empty;
    }
}
