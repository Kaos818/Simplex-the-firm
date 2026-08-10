using System.ComponentModel.DataAnnotations;

namespace SimplexLawFirm.Models
{
    public class Document
    {
        public int Id { get; set; }

        [Required]
        public string FileName { get; set; }

        [Required]
        public string FilePath { get; set; }

        public string FileSize { get; set; }

        public string FileType { get; set; }

        public int? CaseId { get; set; }
        public Case Case { get; set; }

        public int? ClientId { get; set; }
        public Client Client { get; set; }

        public int? UploadedById { get; set; }
        public ApplicationUser UploadedBy { get; set; }

        public string Description { get; set; }
        [MaxLength(80)] public string? RequirementCode { get; set; }

        public DocumentCategory Category { get; set; }

        public int? VersionNumber { get; set; }

        public int? ParentDocumentId { get; set; }

        public bool IsPublic { get; set; }
        public bool RequiresSignature { get; set; }
        public DateTime? SignedAtUtc { get; set; }

        public DateTime UploadedAt { get; set; }

        public DateTime? LastModifiedAt { get; set; }

        public ICollection<DocumentShare> SharedWith { get; set; }
    }

    public class DocumentShare
    {
        public int Id { get; set; }

        public int DocumentId { get; set; }
        public Document Document { get; set; }

        public int SharedWithUserId { get; set; }
        public ApplicationUser SharedWithUser { get; set; }

        public int SharedByUserId { get; set; } 
        public ApplicationUser SharedByUser { get; set; }

        public DateTime SharedAt { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public string AccessToken { get; set; }
    }

    public enum DocumentCategory
    {
        Pleadings,
        Evidence,
        Contracts,
        Correspondence,
        CourtOrders,
        ClientDocuments,
        Internal,
        Other
    }
}
