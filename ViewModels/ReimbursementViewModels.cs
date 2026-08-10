using System.ComponentModel.DataAnnotations;
using SimplexLawFirm.Models;

namespace SimplexLawFirm.ViewModels;

public class BeginReimbursementViewModel
{
    [Required] public int CaseId { get; set; }
    [Required] public ReimbursementExpenseType ExpenseType { get; set; }
    [Required, DataType(DataType.Date)] public DateTime ExpenseDate { get; set; } = DateTime.Today;
    [Range(.01, 1_000_000)] public decimal Amount { get; set; }
    [Required, StringLength(1000, MinimumLength = 5)] public string Description { get; set; } = "";
}

public class UploadReimbursementProofViewModel
{
    [Required] public int ClaimId { get; set; }
    [Required] public IFormFile? Proof { get; set; }
}

public class DecideReimbursementViewModel
{
    [Required] public int ClaimId { get; set; }
    public bool Approve { get; set; }
    [Required, StringLength(1000, MinimumLength = 5)] public string Reason { get; set; } = "";
}
