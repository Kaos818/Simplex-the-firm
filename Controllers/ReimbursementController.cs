using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services;
using SimplexLawFirm.Services.Storage;
using SimplexLawFirm.ViewModels;

namespace SimplexLawFirm.Controllers;

public class ReimbursementController(
    ApplicationDbContext db,
    IReimbursementService service,
    IReimbursementProofStorage storage) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var attorneyId = AttorneyId();
        if (!attorneyId.HasValue) return Forbid();
        var claims = await db.ReimbursementClaims.Include(x => x.Case).Include(x => x.Payable).Include(x => x.Disbursement)
            .Where(x => x.AttorneyId == attorneyId).OrderByDescending(x => x.SubmittedAtUtc).ToListAsync(ct);
        return View(claims);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? caseId, CancellationToken ct)
    {
        var attorneyId = AttorneyId();
        if (!attorneyId.HasValue) return Forbid();
        ViewBag.Cases = await db.Cases.Where(x => x.LawyerId == attorneyId && x.Status == CaseStatus.Active)
            .OrderBy(x => x.CaseNumber).ToListAsync(ct);
        return View(new BeginReimbursementViewModel { CaseId = caseId ?? 0 });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BeginReimbursementViewModel input, CancellationToken ct)
    {
        var attorneyId = AttorneyId();
        if (!attorneyId.HasValue) return Forbid();
        if (!ModelState.IsValid)
        {
            ViewBag.Cases = await db.Cases.Where(x => x.LawyerId == attorneyId && x.Status == CaseStatus.Active).OrderBy(x => x.CaseNumber).ToListAsync(ct);
            return View(input);
        }
        try
        {
            var claim = await service.BeginAsync(attorneyId.Value, input, ct);
            if (claim.Status == ReimbursementStatus.RefusedValidation)
            {
                TempData["Error"] = claim.ValidationFailureReason;
                return RedirectToAction(nameof(Details), new { id = claim.Id });
            }
            return RedirectToAction(nameof(Proof), new { id = claim.Id });
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.Cases = await db.Cases.Where(x => x.LawyerId == attorneyId && x.Status == CaseStatus.Active).OrderBy(x => x.CaseNumber).ToListAsync(ct);
            return View(input);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Proof(int id, CancellationToken ct)
    {
        var attorneyId = AttorneyId();
        if (!attorneyId.HasValue) return Forbid();
        var claim = await db.ReimbursementClaims.Include(x => x.Case)
            .SingleOrDefaultAsync(x => x.Id == id && x.AttorneyId == attorneyId && x.Status == ReimbursementStatus.DraftProofRequired, ct);
        return claim == null ? NotFound() : View(new UploadReimbursementProofViewModel { ClaimId = id });
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Proof(UploadReimbursementProofViewModel input, CancellationToken ct)
    {
        var attorneyId = AttorneyId();
        if (!attorneyId.HasValue) return Forbid();
        if (!ModelState.IsValid || input.Proof == null) return View(input);
        try
        {
            var claim = await service.SubmitProofAsync(attorneyId.Value, input.ClaimId, input.Proof, ct);
            TempData[claim.Status == ReimbursementStatus.RefusedValidation ? "Error" : "Success"] =
                claim.Status == ReimbursementStatus.RefusedValidation
                    ? claim.ValidationFailureReason
                    : claim.Status == ReimbursementStatus.AutoApproved
                        ? "Claim approved automatically and recorded as payable."
                        : "Claim submitted to a Director for review.";
            return RedirectToAction(nameof(Details), new { id = claim.Id });
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception ex) when (ex is InvalidOperationException or InvalidDataException)
        {
            ModelState.AddModelError("", ex.Message);
            return View(input);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var role = HttpContext.Session.GetString("UserRole");
        if (!userId.HasValue || role is not ("Lawyer" or "Admin")) return Forbid();
        var claim = await db.ReimbursementClaims.Include(x => x.Case).Include(x => x.Attorney)
            .Include(x => x.DecidedByUser).Include(x => x.Payable).Include(x => x.Disbursement).ThenInclude(x => x!.Invoice)
            .Include(x => x.AuditEntries)
            .SingleOrDefaultAsync(x => x.Id == id, ct);
        if (claim == null) return NotFound();
        if (role == "Lawyer" && claim.AttorneyId != userId) return Forbid();
        return View(claim);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadProof(int id, CancellationToken ct)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var role = HttpContext.Session.GetString("UserRole");
        var claim = await db.ReimbursementClaims.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (claim == null || string.IsNullOrWhiteSpace(claim.ProofRelativePath)) return NotFound();
        if (!userId.HasValue || (role != "Admin" && !(role == "Lawyer" && claim.AttorneyId == userId))) return Forbid();
        return File(await storage.OpenReadAsync(claim.ProofRelativePath, ct), claim.ProofContentType!, claim.ProofOriginalFileName);
    }

    [HttpGet]
    public async Task<IActionResult> DirectorQueue(CancellationToken ct)
    {
        if (!IsDirector()) return Forbid();
        return View(await db.ReimbursementClaims.Include(x => x.Case).Include(x => x.Attorney)
            .Where(x => x.Status == ReimbursementStatus.PendingDirector)
            .OrderByDescending(x => x.SubmittedAtUtc).ToListAsync(ct));
    }

    [HttpGet]
    public async Task<IActionResult> Policies(CancellationToken ct)
    {
        if (!IsDirector()) return Forbid();
        ViewBag.MatterTerms = await db.MatterExpenseTerms.Include(x => x.Case).OrderBy(x => x.Case.CaseNumber).ToListAsync(ct);
        ViewBag.Cases = await db.Cases.Where(x => x.Status != CaseStatus.Archived).OrderBy(x => x.CaseNumber).ToListAsync(ct);
        return View(await db.ExpensePolicies.OrderBy(x => x.ExpenseType).ToListAsync(ct));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePolicy(int id, decimal perItemLimit, decimal delegatedApprovalLimit,
        ExpenseClassification defaultClassification, CancellationToken ct)
    {
        if (!IsDirector()) return Forbid();
        var policy = await db.ExpensePolicies.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (policy == null) return NotFound();
        if (perItemLimit <= 0 || delegatedApprovalLimit <= 0)
        {
            TempData["Error"] = "Policy and delegated limits must be greater than zero.";
            return RedirectToAction(nameof(Policies));
        }
        policy.PerItemLimit = perItemLimit;
        policy.DelegatedApprovalLimit = delegatedApprovalLimit;
        policy.DefaultClassification = defaultClassification;
        await db.SaveChangesAsync(ct);
        TempData["Success"] = $"{policy.ExpenseType} policy updated.";
        return RedirectToAction(nameof(Policies));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetMatterTerm(int caseId, ReimbursementExpenseType expenseType,
        ExpenseClassification classification, string reason, CancellationToken ct)
    {
        if (!IsDirector()) return Forbid();
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "Record the matter term that supports this classification.";
            return RedirectToAction(nameof(Policies));
        }
        if (!await db.Cases.AnyAsync(x => x.Id == caseId, ct)) return NotFound();
        var term = await db.MatterExpenseTerms.SingleOrDefaultAsync(x => x.CaseId == caseId && x.ExpenseType == expenseType, ct);
        if (term == null)
        {
            term = new MatterExpenseTerm { CaseId = caseId, ExpenseType = expenseType };
            db.MatterExpenseTerms.Add(term);
        }
        term.Classification = classification;
        term.Reason = reason.Trim();
        await db.SaveChangesAsync(ct);
        TempData["Success"] = "Matter-specific expense treatment saved.";
        return RedirectToAction(nameof(Policies));
    }

    [HttpGet]
    public async Task<IActionResult> Review(int id, CancellationToken ct)
    {
        if (!IsDirector()) return Forbid();
        var claim = await db.ReimbursementClaims.Include(x => x.Case).Include(x => x.Attorney)
            .SingleOrDefaultAsync(x => x.Id == id && x.Status == ReimbursementStatus.PendingDirector, ct);
        return claim == null ? NotFound() : View(claim);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Decide(DecideReimbursementViewModel input, CancellationToken ct)
    {
        var directorId = HttpContext.Session.GetInt32("UserId");
        if (!IsDirector() || !directorId.HasValue) return Forbid();
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "A meaningful reason is required.";
            return RedirectToAction(nameof(Review), new { id = input.ClaimId });
        }
        try
        {
            await service.DecideAsync(input.ClaimId, directorId.Value, input.Approve, input.Reason, ct);
            TempData["Success"] = input.Approve ? "Claim approved and recorded as payable." : "Claim refused with the recorded reason.";
            return RedirectToAction(nameof(DirectorQueue));
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Review), new { id = input.ClaimId });
        }
    }

    private int? AttorneyId() =>
        HttpContext.Session.GetString("UserRole") == "Lawyer" ? HttpContext.Session.GetInt32("UserId") : null;
    private bool IsDirector() => HttpContext.Session.GetString("UserRole") == "Admin";
}
