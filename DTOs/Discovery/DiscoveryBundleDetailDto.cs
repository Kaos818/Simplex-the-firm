namespace SimplexLawFirm.DTOs.Discovery
{
    public class DiscoveryBundleDetailDto
    {
        public int Id { get; set; }
        public int CaseId { get; set; }
        public string CaseNumber { get; set; } = string.Empty;
        public string CaseTitle { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string OpposingAttorneyName { get; set; } = string.Empty;
        public string OpposingAttorneyEmail { get; set; } = string.Empty;
        public DateTime? ExchangeExpiresAtUtc { get; set; }
        public DateTime? ExchangedAtUtc { get; set; }
        public DateTime? AccessedAtUtc { get; set; }
        public DateTime? OpposingBundleReceivedAtUtc { get; set; }
        public string? ComplianceNotes { get; set; }
        public int PendingPrivilegeCount { get; set; }
        public int DisclosedCount { get; set; }
        public int WithheldCount { get; set; }
        public int OpposingDocumentCount { get; set; }
        public List<DiscoveryBundleItemDto> Items { get; set; } = new();
    }

    public class DiscoveryBundleItemDto
    {
        public int ItemId { get; set; }
        public int DocumentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public bool ProposedPrivileged { get; set; }
        public string? PrivilegeReason { get; set; }
        public string PrivilegeDecision { get; set; } = string.Empty;
        public string? DirectorReason { get; set; }
        public DateTime? DecidedAtUtc { get; set; }
    }
}
