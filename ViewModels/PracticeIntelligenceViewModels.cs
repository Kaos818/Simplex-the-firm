using System.ComponentModel.DataAnnotations;
using SimplexLawFirm.Models;

namespace SimplexLawFirm.ViewModels;

public record ForecastFactor(string Label, decimal Weight, string Rationale);
public record ComparableMatter(string CaseNumber, string Title, ForecastResult Outcome);

public class ForecastPageViewModel
{
    public Case Case { get; set; } = null!;
    public CaseForecast? Forecast { get; set; }
    [Range(0, 100)] public decimal AttorneyAssessmentPercent { get; set; }
    public bool AttorneyAgrees { get; set; }
    [MaxLength(1000)] public string? AttorneyNotes { get; set; }
}

public class HandoverPageViewModel
{
    public CaseHandover Handover { get; set; } = null!;
    public List<HandoverItem> BlockingItems => Handover.Items.Where(x => x.IsMandatory && !x.IsResolved).ToList();
    public List<HandoverItem> UnacknowledgedMandatoryItems => Handover.Items.Where(x => x.IsMandatory && !x.AcknowledgedByReceiving).ToList();
}

public class LodgeComplaintViewModel
{
    [Required] public int CaseId { get; set; }
    [Required] public ComplaintCategory Category { get; set; }
    [Required, MinLength(20), MaxLength(4000)] public string Description { get; set; } = "";
    public bool ConfirmPossibleDuplicate { get; set; }
    public List<IFormFile> Attachments { get; set; } = [];
}

public class ReassignmentViewModel
{
    [Required] public int CaseId { get; set; }
    [Required] public int ReceivingAttorneyId { get; set; }
    [Required, MinLength(10), MaxLength(1000)] public string Reason { get; set; } = "";
}

public class RequestProspectsViewModel
{
    [Required] public int CaseId { get; set; }
    [MaxLength(1000)] public string? Message { get; set; }
}
