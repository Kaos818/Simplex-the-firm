using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Infrastructure.Authorization;
using SimplexLawFirm.Models;

namespace SimplexLawFirm.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        [RequireSessionRole("Admin")]
        public async Task<IActionResult> Admin()
        {
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.TotalClients = await _context.Clients.CountAsync();
            ViewBag.TotalCases = await _context.Cases.CountAsync();
            ViewBag.ActiveRetainers = await _context.Retainers.CountAsync(r => r.Status == RetainerStatus.Active);
            ViewBag.PendingRequests = await _context.ClientRequests.CountAsync(r => r.Status == "Pending");
            ViewBag.PendingApprovals = await _context.Retainers.CountAsync(r => r.Status == RetainerStatus.PendingApproval);

            return View();
        }

        [RequireSessionRole("Lawyer")]
        public async Task<IActionResult> Lawyer()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            ViewBag.MyCases = await _context.Cases.CountAsync(c => c.LawyerId == userId);
            ViewBag.PendingApprovals = await _context.Retainers.CountAsync(r => r.Status == RetainerStatus.PendingApproval);
            ViewBag.UpcomingEvents = await _context.CalendarEvents.CountAsync(e => e.AssignedToUserId == userId && e.ActualStartTime > DateTime.Now);
            ViewBag.UnbilledHours = await _context.TimeEntries
                .Where(t => t.LawyerId == userId && !t.IsBilled)
                .SumAsync(t => t.Hours);

            return View();
        }

        [RequireSessionRole("Client")]
        public async Task<IActionResult> Client()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var user = await _context.Users.FindAsync(userId);
            var client = await _context.Clients.FirstOrDefaultAsync(c => c.Email == user.Email);

            if (client != null)
            {
                ViewBag.MyCases = await _context.Cases.CountAsync(c => c.ClientId == client.Id);
                ViewBag.ActiveRetainers = await _context.Retainers.CountAsync(r => r.ClientId == client.Id && r.Status == RetainerStatus.Active);
                ViewBag.OpenInvoices = await _context.Invoices.CountAsync(i => i.ClientId == client.Id && i.Status != InvoiceStatus.Paid);
                ViewBag.UpcomingEvents = await _context.CalendarEvents.CountAsync(e => e.AssignedToUserId == userId && e.ActualStartTime > DateTime.Now);
            }

            return View();
        }

        [RequireSessionRole("Accountant")]
        public async Task<IActionResult> Accountant()
        {
            ViewBag.TrustBalance = await _context.TrustAccounts.Where(x => !x.IsClosed).SumAsync(x => x.Balance);
            ViewBag.TrustAccountCount = await _context.TrustAccounts.CountAsync(x => !x.IsClosed);
            ViewBag.OverdueInvoices = await _context.Invoices.CountAsync(i => i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled && i.DueDate < DateTime.Now);
            ViewBag.PendingReimbursements = await _context.ReimbursementClaims.CountAsync(r => r.Status == ReimbursementStatus.PendingDirector);
            ViewBag.RecentPayments = await _context.Payments.Include(p => p.Client).OrderByDescending(p => p.PaymentDate).Take(5).ToListAsync();
            return View();
        }

        [RequireSessionRole("Paralegal")]
        public async Task<IActionResult> Paralegal()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            ViewBag.MyTasks = await _context.CalendarEvents.CountAsync(e => e.AssignedToUserId == userId && e.Type == EventType.Task && e.Status != EventStatus.Completed && e.Status != EventStatus.Cancelled);
            ViewBag.UpcomingDeadlines = await _context.CalendarEvents.Where(e => e.AssignedToUserId == userId && e.Type == EventType.Deadline && e.Status != EventStatus.Completed && e.Status != EventStatus.Cancelled && e.StartDateTime >= DateTime.Now)
                .OrderBy(e => e.StartDateTime).Take(5).ToListAsync();
            ViewBag.RecentDocuments = await _context.Documents.OrderByDescending(d => d.Id).Take(5).ToListAsync();
            return View();
        }
    }
}
