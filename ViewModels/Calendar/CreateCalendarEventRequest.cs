using System.ComponentModel.DataAnnotations;
using SimplexLawFirm.Models;

namespace SimplexLawFirm.ViewModels.Calendar;

public class CreateCalendarEventRequest
{
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string? Location { get; set; }
    public string? MeetingLink { get; set; }
    public DateTime StartLocal { get; set; }
    public DateTime EndLocal { get; set; }
    public EventType Type { get; set; }
    public int? AssignedToUserId { get; set; }
    public int? CaseId { get; set; }
    public int? RetainerId { get; set; }
    public int? ClientId { get; set; }
    public decimal? AppointmentFee { get; set; }
    public int PaymentDueDays { get; set; } = 7;
    public LatePenaltyType LatePenaltyType { get; set; }
    public decimal LatePenaltyValue { get; set; }
    public int LatePenaltyGraceDays { get; set; }
}
