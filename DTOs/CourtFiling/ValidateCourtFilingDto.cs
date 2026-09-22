using System.ComponentModel.DataAnnotations;

namespace SimplexLawFirm.DTOs.CourtFiling
{
    public class ValidateCourtFilingDto
    {
        [Required]
        public int SubmissionId { get; set; }
    }
}
