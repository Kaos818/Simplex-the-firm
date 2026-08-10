using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Infrastructure.Authorization;
using SimplexLawFirm.Models;

namespace SimplexLawFirm.Controllers;

[RequireSessionRole("Client")]
public sealed class ClientCalendarController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var email = HttpContext.Session.GetString("UserEmail");
        var clientId = await db.Clients.Where(x => x.Email == email).Select(x => (int?)x.Id).SingleOrDefaultAsync(ct);
        if (clientId is null) return RedirectToAction("EditProfile", "Client");
        var appointments = await db.CalendarEvents.Include(x => x.AssignedToUser).Include(x => x.Case)
            .Where(x => x.ClientId == clientId && x.Status != EventStatus.Cancelled)
            .OrderBy(x => x.StartDateTime).ToListAsync(ct);
        return View(appointments);
    }
}
