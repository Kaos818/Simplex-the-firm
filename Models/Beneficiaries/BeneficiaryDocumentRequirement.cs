namespace SimplexLawFirm.Models.Beneficiaries;

public class BeneficiaryDocumentRequirement
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsRequired { get; set; }
    public bool RequiresCertifiedCopy { get; set; }
    public bool RequiresExpiryCheck { get; set; }
    public int MaximumAgeDays { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class BeneficiaryRequirementAssignment
{
    public int BeneficiaryId { get; set; }
    public Beneficiary Beneficiary { get; set; } = null!;
    public int RequirementId { get; set; }
    public BeneficiaryDocumentRequirement Requirement { get; set; } = null!;
    public bool IsRequired { get; set; } = true;
}
