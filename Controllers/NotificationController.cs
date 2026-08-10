using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Infrastructure.Authorization;

namespace SimplexLawFirm.Controllers;
[RequireSessionUser]
public class NotificationController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var id = HttpContext.Session.GetInt32("UserId")!.Value;
        return View(await db.SystemNotifications.Where(x => x.UserId == id).OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct));
    }
    [RequireSessionRole("Admin")]
    public async Task<IActionResult> Outbox(CancellationToken ct)
    {
        return View(await db.EmailOutboxMessages.OrderByDescending(x => x.CreatedAtUtc).Take(500).ToListAsync(ct));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Read(long id, CancellationToken ct)
    {
        var user = HttpContext.Session.GetInt32("UserId")!.Value;
        var item = await db.SystemNotifications.SingleOrDefaultAsync(x => x.Id == id && x.UserId == user, ct); if (item is null) return NotFound();
        item.IsRead = true; item.ReadAtUtc = DateTime.UtcNow; await db.SaveChangesAsync(ct);
        return item.ActionUrl is not null && Url.IsLocalUrl(item.ActionUrl) ? LocalRedirect(item.ActionUrl) : RedirectToAction(nameof(Index));
    }
}
