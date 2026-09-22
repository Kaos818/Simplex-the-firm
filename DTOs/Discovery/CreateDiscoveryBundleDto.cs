using System.ComponentModel.DataAnnotations;

namespace SimplexLawFirm.DTOs.Discovery
{
    /// <summary>Attorney payload to compile a discovery bundle for a case.</summary>
    public class CreateDiscoveryBundleDto
    {
        [Required]
        public int CaseId { get; set; }

        [Required, MaxLength(200)]
        public string OpposingAttorneyName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(320)]
        public string OpposingAttorneyEmail { get; set; } = string.Empty;

        /// <summary>Existing Documents.Id values to include in the bundle.</summary>
        [MinLength(1)]
        public List<int> DocumentIds { get; set; } = new();

        /// <summary>Subset of DocumentIds the attorney proposes to withhold under privilege.</summary>
        public List<int> PrivilegedDocumentIds { get; set; } = new();

        /// <summary>DocumentId -> stated privilege reason (required for each privileged id).</summary>
        public Dictionary<int, string> PrivilegeReasons { get; set; } = new();
    }
}
