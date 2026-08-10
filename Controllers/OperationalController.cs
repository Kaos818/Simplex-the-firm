using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Infrastructure.Authorization;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services;

namespace SimplexLawFirm.Controllers;

[RequireSessionRole("Lawyer", "Admin", "Director")]
public sealed class OperationalController(ApplicationDbContext db, IOperationalUseCaseService service) : Controller
{
    [RequireSessionRole("Lawyer")]
    public async Task<IActionResult> Research(int caseId, string? issue, CancellationToken ct)
    {
        ViewBag.CaseId=caseId; ViewBag.Issue=issue;
        if (string.IsNullOrWhiteSpace(issue)) return View(Array.Empty<SimplexLawFirm.Models.LegalAuthority>());
        try
        {
            var results = await service.ResearchAsync(HttpContext.Session.GetInt32("UserId")!.Value, caseId, issue, ct);
            ViewBag.LimitedToInternal = results.Count > 0 && results.All(x => x.IsInternalFallback);
            return View(results);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); return View(Array.Empty<SimplexLawFirm.Models.LegalAuthority>()); }
    }
    [HttpPost,ValidateAntiForgeryToken,RequireSessionRole("Lawyer")]
    public async Task<IActionResult> Rely(int caseId,int authorityId,string reason,bool confirmAdverseTreatment,CancellationToken ct)
    { try{await service.RelyAsync(HttpContext.Session.GetInt32("UserId")!.Value,caseId,authorityId,reason,confirmAdverseTreatment,ct);TempData["Success"]="Authority attached to the matter and audited.";}catch(UnauthorizedAccessException){return Forbid();}catch(Exception ex){TempData["Error"]=ex.Message;}return RedirectToAction(nameof(Research),new{caseId}); }
    [RequireSessionRole("Lawyer")]
    public async Task<IActionResult> CheckIn(CancellationToken ct){var attorneyId=HttpContext.Session.GetInt32("UserId")!.Value;ViewBag.Active=await db.AttorneyWhereabouts.FirstOrDefaultAsync(x=>x.AttorneyId==attorneyId&&x.CheckedOutAtUtc==null,ct);ViewBag.Venues=await db.CalendarEvents.Where(x=>x.AssignedToUserId==attorneyId&&x.StartDateTime>=DateTime.UtcNow.AddDays(-1)&&x.StartDateTime<=DateTime.UtcNow.AddDays(7)&&x.Location!="").Select(x=>x.Location).Distinct().OrderBy(x=>x).ToListAsync(ct);return View();}
    [HttpPost,ValidateAntiForgeryToken,RequireSessionRole("Lawyer")]
    public async Task<IActionResult> CheckIn(string? venue,string? recordedVenue,DateTime expectedReturnUtc,int? eventId,CancellationToken ct){try{await service.CheckInAsync(HttpContext.Session.GetInt32("UserId")!.Value,eventId,string.IsNullOrWhiteSpace(venue)?recordedVenue??"":venue,expectedReturnUtc,ct);TempData["Success"]="Off-site check-in recorded.";}catch(Exception ex){TempData["Error"]=ex.Message;}return RedirectToAction(nameof(CheckIn));}
    [HttpPost,ValidateAntiForgeryToken,RequireSessionRole("Lawyer")]
    public async Task<IActionResult> CheckOut(int id,CancellationToken ct){await service.CheckOutAsync(HttpContext.Session.GetInt32("UserId")!.Value,id,ct);return RedirectToAction(nameof(CheckIn));}
    [RequireSessionRole("Admin", "Director")]
    public async Task<IActionResult> Whereabouts(CancellationToken ct){await service.EscalateOverdueAsync(ct);var items=await db.AttorneyWhereabouts.Include(x=>x.Attorney).Include(x=>x.CalendarEvent).ThenInclude(x=>x!.Case).OrderByDescending(x=>x.CheckedInAtUtc).ToListAsync(ct);var urgent=items.Where(x=>x.CheckedOutAtUtc==null&&x.Status==WhereaboutStatus.DirectorEscalated).SelectMany(x=>db.CalendarEvents.Where(e=>e.CaseId!=null&&e.Case!.LawyerId==x.AttorneyId&&(e.Type==EventType.CourtAppearance||e.Type==EventType.Hearing)&&e.StartDateTime>=DateTime.UtcNow&&e.StartDateTime<=DateTime.UtcNow.AddHours(24))).ToList();ViewBag.UrgentEvents=urgent;ViewBag.ReplacementCandidates=urgent.ToDictionary(x=>x.Id,x=>service.UrgentReplacementCandidatesAsync(x.Id,ct).GetAwaiter().GetResult());return View(items);}
    [HttpPost,ValidateAntiForgeryToken,RequireSessionRole("Admin", "Director")]
    public async Task<IActionResult> RecordSafetyContact(int id,string outcome,bool accountedFor,CancellationToken ct){try{await service.RecordSafetyContactAsync(HttpContext.Session.GetInt32("UserId")!.Value,id,outcome,accountedFor,ct);TempData["Success"]="Safety contact attempt recorded.";}catch(Exception ex){TempData["Error"]=ex.Message;}return RedirectToAction(nameof(Whereabouts));}
    [HttpPost,ValidateAntiForgeryToken,RequireSessionRole("Admin", "Director")]
    public async Task<IActionResult> ReallocateUrgentCommitment(int eventId,int replacementAttorneyId,string reason,CancellationToken ct){try{await service.ReallocateUrgentCommitmentAsync(HttpContext.Session.GetInt32("UserId")!.Value,eventId,replacementAttorneyId,reason,ct);TempData["Success"]="Urgent court commitment reallocated and audited.";}catch(Exception ex){TempData["Error"]=ex.Message;}return RedirectToAction(nameof(Whereabouts));}
    [RequireSessionRole("Admin")]
    public async Task<IActionResult> TrustRequests(CancellationToken ct)=>View(await db.BeneficiaryTrustDisbursementRequests.Include(x=>x.Beneficiary).OrderByDescending(x=>x.SubmittedAtUtc).ToListAsync(ct));
    [HttpPost,ValidateAntiForgeryToken,RequireSessionRole("Admin")]
    public async Task<IActionResult> DecideTrustRequest(int id,bool approve,string reason,CancellationToken ct){try{await service.DecideDisbursementAsync(HttpContext.Session.GetInt32("UserId")!.Value,id,approve,reason,ct);TempData["Success"]="Trust request decision recorded and the beneficiary was notified.";}catch(Exception ex){TempData["Error"]=ex.Message;}return RedirectToAction(nameof(TrustRequests));}
}
