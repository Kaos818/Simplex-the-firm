using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Infrastructure.Authorization;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services;
namespace SimplexLawFirm.Controllers;
[RequireSessionRole("Lawyer", "Director", "Admin")]
public sealed class ClientRiskController(ApplicationDbContext db, IClientRelationshipRiskService analysis) : Controller
{
 public async Task<IActionResult> Index(){var role=HttpContext.Session.GetString("UserRole");var q=db.ClientRelationshipRisks.Include(x=>x.Client).AsQueryable(); if(role=="Lawyer"){var id=HttpContext.Session.GetInt32("UserId")!.Value;q=q.Where(x=>db.Cases.Any(c=>c.ClientId==x.ClientId&&c.LawyerId==id));} ViewBag.Director=role is "Director" or "Admin";return View(await q.OrderByDescending(x=>x.HighRiskAlert).ThenByDescending(x=>x.OverallScore).ToListAsync());}
 [HttpPost,ValidateAntiForgeryToken,RequireSessionRole("Director","Admin")]
 public async Task<IActionResult> RecordAction(int id,string action,string notes){var risk=await db.ClientRelationshipRisks.FindAsync(id);if(risk is null)return NotFound();risk.DirectorAction=action;risk.DirectorNotes=notes;risk.ReviewedByUserId=HttpContext.Session.GetInt32("UserId");risk.ReviewedAtUtc=DateTime.UtcNow;await db.SaveChangesAsync();TempData["Success"]="Corrective action recorded in the risk profile.";return RedirectToAction(nameof(Index));}
 [HttpPost,ValidateAntiForgeryToken,RequireSessionRole("Director","Admin")]
 public async Task<IActionResult> Recalculate()
 {
   await analysis.RecalculateAsync(); TempData["Success"]="Relationship-risk analysis completed for all active clients.";return RedirectToAction(nameof(Index));
 }
}
