namespace SimplexLawFirm.Models
{
    public class CaseNote
    {
        public int Id { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public string Content { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public bool IsPrivileged { get; set; }
        public bool IsConfidential { get; set; }

        public int CaseId { get; set; }
    }
}
