namespace SimplexLawFirm.Models.Beneficiaries;

public class BiometricConsentRecord
{
    public long Id { get; set; }
    public Guid VerificationSessionId { get; set; }
    public int BeneficiaryId { get; set; }
    public string NoticeVersion { get; set; } = "";
    public string NoticeTextHash { get; set; } = "";
    public bool ConsentGranted { get; set; }
    public DateTime RecordedAtUtc { get; set; }
    public string? UserAgent { get; set; }
    public string? IpAddressHash { get; set; }
}
