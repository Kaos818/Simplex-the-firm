using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.DTOs.FeeDiscount;
using SimplexLawFirm.Infrastructure.Authorization;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services;

namespace SimplexLawFirm.Controllers;

[Route("FeeDiscount")]
public sealed class FeeDiscountPortalController(ApplicationDbContext db, IFeeDiscountService discounts) : Controller
{
    [HttpGet(""), RequireSessionRole("Client", "Director", "Admin")]
    public async Task<IActionResult> Index()
    {
        var role = HttpContext.Session.GetString("UserRole"); var userId = HttpContext.Session.GetInt32("UserId")!.Value;
        var query = db.FeeDiscountRequests.Include(x => x.Invoice).Include(x => x.Client).AsQueryable();
        if (role == "Client") query = query.Where(x => x.ClientId == CurrentClientId(userId));
        return View("~/Views/FeeDiscount/Index.cshtml", await query.OrderByDescending(x => x.CreatedAtUtc).ToListAsync());
    }
    [HttpGet("Request/{invoiceId:int}"), RequireSessionRole("Client")]
    public async Task<IActionResult> Request(int invoiceId)
    {
        var invoice = await db.Invoices.Include(x => x.Payments).FirstOrDefaultAsync(x => x.Id == invoiceId && x.ClientId == CurrentClientId(HttpContext.Session.GetInt32("UserId")!.Value) && !x.IsPaid);
        return invoice is null ? Forbid() : View("~/Views/FeeDiscount/Create.cshtml", invoice);
    }
    [HttpPost("Request"), ValidateAntiForgeryToken, RequireSessionRole("Client")]
    public async Task<IActionResult> Request(int invoiceId, decimal proposedAmount, decimal? requestedDiscountPercent, string reason, IFormFile? supportingDocument)
    {
        try
        {
            string? path = null;
            if (supportingDocument is not null && supportingDocument.Length > 0)
            {
                if (supportingDocument.Length > 5 * 1024 * 1024) throw new InvalidOperationException("Supporting documents must be 5 MB or smaller.");
                var extension = Path.GetExtension(supportingDocument.FileName).ToLowerInvariant();
                if (extension is not (".pdf" or ".jpg" or ".jpeg" or ".png")) throw new InvalidOperationException("Supporting documents must be PDF, JPG, or PNG files.");
                var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "fee-discounts");
                Directory.CreateDirectory(folder);
                var name = $"{Guid.NewGuid():N}{extension}";
                await using var stream = System.IO.File.Create(Path.Combine(folder, name));
                await supportingDocument.CopyToAsync(stream);
                path = $"/uploads/fee-discounts/{name}";
            }
            var item = await discounts.SubmitDiscountRequestAsync(CurrentClientId(HttpContext.Session.GetInt32("UserId")!.Value), new SubmitFeeDiscountRequestDto { InvoiceId = invoiceId, ProposedAmount = proposedAmount, RequestedDiscountPercent = requestedDiscountPercent, Reason = reason, SupportingDocumentPath = path }); TempData["Success"] = item.Status == nameof(FeeDiscountStatus.AutoApproved) ? "Your discount was approved automatically." : "Your request was sent to the Director for review."; return RedirectToAction(nameof(Details), new { id = item.Id });
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; return RedirectToAction(nameof(Request), new { invoiceId }); }
    }
    [HttpGet("Details/{id:int}"), RequireSessionRole("Client", "Director", "Admin")]
    public async Task<IActionResult> Details(int id)
    {
        var item = await db.FeeDiscountRequests.Include(x => x.Invoice).Include(x => x.Client).SingleOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound(); if (HttpContext.Session.GetString("UserRole") == "Client" && item.ClientId != CurrentClientId(HttpContext.Session.GetInt32("UserId")!.Value)) return Forbid();
        return View("~/Views/FeeDiscount/Details.cshtml", item);
    }
    [HttpPost("Decide"), ValidateAntiForgeryToken, RequireSessionRole("Director", "Admin")]
    public async Task<IActionResult> Decide(int id, DirectorDecisionAction status, decimal? counterOfferAmount, string reason)
    {
        try { await discounts.ProcessDirectorDecisionAsync(HttpContext.Session.GetInt32("UserId")!.Value, new DirectorFeeDiscountDecisionDto { FeeDiscountRequestId = id, Action = status, CounterOfferAmount = counterOfferAmount, DecisionReason = reason }); TempData["Success"] = "Decision recorded and client notified."; } catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; } return RedirectToAction(nameof(Details), new { id });
    }
    [HttpPost("CounterOffer"), ValidateAntiForgeryToken, RequireSessionRole("Client")]
    public async Task<IActionResult> CounterOffer(int id, bool accept)
    {
        try { await discounts.RespondToCounterOfferAsync(CurrentClientId(HttpContext.Session.GetInt32("UserId")!.Value), new ClientCounterOfferResponseDto { FeeDiscountRequestId = id, Accept = accept }); TempData["Success"] = "Your counter-offer response was recorded."; } catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; } return RedirectToAction(nameof(Details), new { id });
    }
    private int CurrentClientId(int userId) { var email = db.Users.Where(x => x.Id == userId).Select(x => x.Email).Single(); return db.Clients.Where(x => x.Email == email).Select(x => x.Id).Single(); }
}
