using System.ComponentModel.DataAnnotations;
using SimplexLawFirm.Models;

namespace SimplexLawFirm.ViewModels;

public sealed class CreateLegalCostRecoveryViewModel
{
    public int CaseId { get; set; }
    public LegalCostRecoveryGround Ground { get; set; }
    [Required, StringLength(4000, MinimumLength = 20)] public string Justification { get; set; } = "";
    [Required, StringLength(200)] public string OpposingPartyName { get; set; } = "";
    [Required, EmailAddress, StringLength(320)] public string OpposingPartyEmail { get; set; } = "";
    [MinLength(1)] public List<int> TimeEntryIds { get; set; } = [];
}

public sealed class DecideLegalCostRecoveryViewModel
{
    public int ClaimId { get; set; }
    public LegalCostRecoveryStatus Decision { get; set; }
    public decimal? AwardedAmount { get; set; }
    [Required, StringLength(4000, MinimumLength = 10)] public string DecisionNotes { get; set; } = "";
}
