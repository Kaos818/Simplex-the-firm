using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Infrastructure.Authorization;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services;

namespace SimplexLawFirm.Controllers;

[RequireSessionRole("Admin", "Lawyer")]
public sealed class PrecedentController(ApplicationDbContext db, IPrecedentLibraryService library) : Controller
{
    [RequireSessionRole("Admin")]
    public async Task<IActionResult> Index(CancellationToken ct) => View(await library.DashboardAsync(ct));

    [RequireSessionRole("Lawyer")]
    public async Task<IActionResult> Contribute(CancellationToken ct)
    {
        ViewBag.Subjects = await db.LegalSubjects.Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync(ct);
        return View(new KnowledgeArticle());
    }

    [HttpPost, ValidateAntiForgeryToken, RequireSessionRole("Lawyer")]
    public async Task<IActionResult> Contribute(KnowledgeArticle input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Title) || input.Content?.Trim().Length < 100) { TempData["Error"] = "Provide a title and at least 100 characters of useful legal analysis."; return RedirectToAction(nameof(Contribute)); }
        input.Id = 0; input.AuthorUserId = HttpContext.Session.GetInt32("UserId")!.Value; input.Status = KnowledgeArticleStatus.Published;
        input.IsPrivileged = false; input.IsConfidential = false; input.CreatedAtUtc = input.UpdatedAtUtc = DateTime.UtcNow;
        db.KnowledgeArticles.Add(input); await db.SaveChangesAsync(ct); await library.QueueArticleAsync(input.Id, ct);
        TempData["Success"] = "Contribution submitted to the firm library and queued for eligibility checks."; return RedirectToAction(nameof(Contribute));
    }

    [RequireSessionRole("Admin")]
    public async Task<IActionResult> Article(int? id, CancellationToken ct)
    {
        ViewBag.Subjects = await db.LegalSubjects.Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync(ct);
        if (!id.HasValue) return View(new KnowledgeArticle());
        var article = await db.KnowledgeArticles.SingleOrDefaultAsync(x => x.Id == id, ct);
        return article == null ? NotFound() : View(article);
    }

    [HttpPost, ValidateAntiForgeryToken, RequireSessionRole("Admin")]
    public async Task<IActionResult> SaveArticle(KnowledgeArticle input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Title) || string.IsNullOrWhiteSpace(input.Content))
        {
            TempData["Error"] = "A title and article content are required.";
            return RedirectToAction(nameof(Article), new { id = input.Id == 0 ? (int?)null : input.Id });
        }
        KnowledgeArticle article;
        if (input.Id == 0)
        {
            article = input;
            article.AuthorUserId = HttpContext.Session.GetInt32("UserId")!.Value;
            article.CreatedAtUtc = article.UpdatedAtUtc = DateTime.UtcNow;
            db.KnowledgeArticles.Add(article);
        }
        else
        {
            article = await db.KnowledgeArticles.SingleAsync(x => x.Id == input.Id, ct);
            article.Title = input.Title.Trim(); article.Content = input.Content.Trim(); article.Status = input.Status;
            article.IsPrivileged = input.IsPrivileged; article.IsConfidential = input.IsConfidential;
            article.SuggestedSubjectId = input.SuggestedSubjectId; article.UpdatedAtUtc = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);
        await library.QueueArticleAsync(article.Id, ct);
        TempData["Success"] = article.Status == KnowledgeArticleStatus.Published
            ? "Article saved and queued for eligibility checking and indexing."
            : "Article saved. Draft and archived content will not appear as current precedent.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequireSessionRole("Admin")]
    public async Task<IActionResult> Review(int id, PrecedentFlagStatus decision, string? note, CancellationToken ct)
    {
        try { await library.ReviewFlagAsync(id, decision, HttpContext.Session.GetInt32("UserId")!.Value, note, ct); TempData["Success"] = "Curation decision recorded."; }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequireSessionRole("Admin")]
    public async Task<IActionResult> Commission(int subjectId, string brief, DateTime? dueDate, CancellationToken ct)
    {
        try { await library.CommissionAsync(subjectId, HttpContext.Session.GetInt32("UserId")!.Value, brief, dueDate?.ToUniversalTime(), ct); TempData["Success"] = "Coverage material commissioned."; }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequireSessionRole("Admin")]
    public async Task<IActionResult> CompleteCommission(int id, CancellationToken ct)
    {
        try
        {
            await library.CompleteCommissionAsync(id, HttpContext.Session.GetInt32("UserId")!.Value, ct);
            TempData["Success"] = "Commission marked complete.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequireSessionRole("Admin")]
    public async Task<IActionResult> RetryBacklog(CancellationToken ct)
    {
        var processed = await library.ProcessBacklogAsync(100, ct);
        TempData["Success"] = $"{processed} queued item(s) processed or safely retained for retry.";
        return RedirectToAction(nameof(Index));
    }
}
