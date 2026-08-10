namespace SimplexLawFirm.Models.Beneficiaries;

public class BeneficiaryInvitation
{
    public long Id { get; set; }
    public int BeneficiaryId { get; set; }
    public Beneficiary Beneficiary { get; set; } = null!;
    public string TokenHash { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public int CreatedByUserId { get; set; }
}
