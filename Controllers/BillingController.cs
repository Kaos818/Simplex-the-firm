using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services;
using SimplexLawFirm.Infrastructure.Authorization;
using SimplexLawFirm.Services.Billing;

namespace SimplexLawFirm.Controllers
{
    [RequireSessionUser, AutoValidateAntiforgeryToken]
    public class BillingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMatterCostEstimateService _costEstimates;
        private readonly IAppointmentBillingService? _appointmentBilling;

        public BillingController(ApplicationDbContext context, IMatterCostEstimateService costEstimates, IAppointmentBillingService? appointmentBilling = null)
        {
            _context = context;
            _costEstimates = costEstimates;
            _appointmentBilling = appointmentBilling;
        }

        [HttpGet, RequireSessionRole("Accountant")]
        public async Task<IActionResult> PenaltyQueue()
        {
            var invoices = await _context.Invoices.Include(x => x.Client).Include(x => x.Payments)
                .Where(x => x.Status == InvoiceStatus.Overdue && !_context.InvoicePenalties.Any(p => p.InvoiceId == x.Id))
                .OrderBy(x => x.DueDate).ToListAsync();
            return View(invoices);
        }

        [HttpPost, RequireSessionRole("Accountant")]
        public async Task<IActionResult> ApplyPenalty(int invoiceId, string reason)
        {
            var accountantId = HttpContext.Session.GetInt32("UserId")!.Value;
            try
            {
                if (_appointmentBilling is null) throw new InvalidOperationException("Billing service is unavailable.");
                await _appointmentBilling.ApplyPenaltyAsync(invoiceId, accountantId, reason, DateTime.UtcNow);
                TempData["Success"] = "The pre-agreed penalty was applied and the client was notified.";
            }
            catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
            catch (UnauthorizedAccessException) { return Forbid(); }
            return RedirectToAction(nameof(PenaltyQueue));
        }

        // INVOICES
        public async Task<IActionResult> Invoices(string status = null)
        {
            if (HttpContext.Session.GetString("UserRole") == nameof(UserRole.Client))
                return RedirectToAction(nameof(MyInvoices), new { status });
            if (HttpContext.Session.GetInt32("UserId") is null) return RedirectToAction("Login", "Home");
            var invoices = await _context.Invoices
                 .Include(i => i.Client)
                 .Include(i => i.Case)
                 .OrderByDescending(i => i.IssueDate)
                 .ToListAsync();

            if (!string.IsNullOrEmpty(status))
            {
                invoices = invoices.Where(i => i.Status.ToString() == status).ToList();
            }

            
            ViewBag.StatusOptions = Enum.GetValues(typeof(InvoiceStatus));
            return View(invoices);
        }

        public async Task<IActionResult> CreateInvoice(int? clientId, int? caseId)
        {
            ViewBag.Clients = new SelectList(_context.Clients, "Id", "FirstName", "LastName");
            ViewBag.Cases = new SelectList(_context.Cases, "Id", "Title");

            var invoice = new Invoice
            {
                IssueDate = DateTime.Now,
                DueDate = DateTime.Now.AddDays(30),
                Status = InvoiceStatus.Draft,
                CreatedAt = DateTime.Now
            };

            if (clientId.HasValue) invoice.ClientId = clientId.Value;
            if (caseId.HasValue) invoice.CaseId = caseId.Value;

            return View(invoice);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateInvoice(Invoice invoice)
        {
            if (ModelState.IsValid)
            {
                invoice.InvoiceNumber = GenerateInvoiceNumber();
                var pendingDisbursements = invoice.CaseId.HasValue
                    ? await _context.MatterDisbursements.Where(x => x.CaseId == invoice.CaseId && x.InvoiceId == null).ToListAsync()
                    : [];
                invoice.Amount += pendingDisbursements.Sum(x => x.Amount);
                invoice.TotalAmount = invoice.Amount + invoice.TaxAmount;
                invoice.CreatedAt = DateTime.Now;

                _context.Invoices.Add(invoice);
                foreach (var disbursement in pendingDisbursements)
                {
                    disbursement.Invoice = invoice;
                    disbursement.InvoicedAtUtc = DateTime.UtcNow;
                }
                await _costEstimates.EvaluateInvoiceAsync(invoice);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Invoice #{invoice.InvoiceNumber} created successfully!";
                return RedirectToAction("InvoiceDetails", new { id = invoice.Id });
            }

            ViewBag.Clients = new SelectList(_context.Clients, "Id", "FirstName", "LastName");
            ViewBag.Cases = new SelectList(_context.Cases, "Id", "Title");
            return View(invoice);
        }

        #region Client Invoice Views

        // GET: Billing/MyInvoices - Client views their invoices
        [HttpGet]
        public async Task<IActionResult> MyInvoices(string status = null)
        {
            // Get logged in user
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                TempData["Error"] = "Please log in to view your invoices.";
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FindAsync(userId.Value);
            var client = await _context.Clients.FirstOrDefaultAsync(c => c.Email == user.Email);

            if (client == null)
            {
                TempData["Error"] = "Client profile not found.";
                return RedirectToAction("Edit", "Client");
            }

            // Get invoices for this client
            var query = _context.Invoices
                .Include(i => i.Client)
                .Include(i => i.Case)
                .Include(i => i.Retainer)
                .Include(i => i.Payments)
                .Include(i => i.Disbursements)
                .Where(i => i.ClientId == client.Id);

            // Apply status filter
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<InvoiceStatus>(status, out var statusEnum))
            {
                query = query.Where(i => i.Status == statusEnum);
            }

            var invoices = await query
                .OrderByDescending(i => i.IssueDate)
                .ToListAsync();

            // Calculate statistics
            ViewBag.TotalInvoices = invoices.Count;
            ViewBag.TotalPaid = invoices.Where(i => i.Status == InvoiceStatus.Paid).Sum(i => i.TotalAmount);
            ViewBag.TotalOutstanding = invoices.Where(i => i.Status != InvoiceStatus.Paid).Sum(i => i.TotalAmount - (i.Payments?.Sum(p => p.Amount) ?? 0));
            ViewBag.OverdueCount = invoices.Count(i => i.DueDate < DateTime.Now && i.Status != InvoiceStatus.Paid);
            ViewBag.CurrentStatus = status;
            ViewBag.StatusOptions = Enum.GetValues(typeof(InvoiceStatus));

            return View(invoices);
        }

        // GET: Billing/ClientInvoiceDetails/{id} - Client views specific invoice
        [HttpGet]
        public async Task<IActionResult> ClientInvoiceDetails(int id)
        {
            // Get logged in user
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FindAsync(userId.Value);
            var client = await _context.Clients.FirstOrDefaultAsync(c => c.Email == user.Email);

            if (client == null)
            {
                return RedirectToAction("Edit", "Client");
            }

            var invoice = await _context.Invoices
                .Include(i => i.Client)
                .Include(i => i.Case)
                .Include(i => i.Retainer)
                .Include(i => i.Payments)
                .Include(i => i.Disbursements)
                .FirstOrDefaultAsync(i => i.Id == id && i.ClientId == client.Id);

            if (invoice == null)
            {
                TempData["Error"] = "Invoice not found.";
                return RedirectToAction("MyInvoices");
            }

            var paidAmount = invoice.Payments?.Sum(p => p.Amount) ?? 0;
            ViewBag.PaidAmount = paidAmount;
            ViewBag.BalanceDue = invoice.TotalAmount - paidAmount;

            return View("ClientInvoiceDetails", invoice);
        }

        // POST: Billing/PayInvoice - Client initiates payment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayInvoice(int id, string paymentMethod)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FindAsync(userId.Value);
            var client = await _context.Clients.FirstOrDefaultAsync(c => c.Email == user.Email);

            if (client == null)
            {
                return RedirectToAction("Edit", "Client");
            }

            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.Id == id && i.ClientId == client.Id);

            if (invoice == null)
            {
                TempData["Error"] = "Invoice not found.";
                return RedirectToAction("MyInvoices");
            }

            // Redirect to payment gateway or payment page
            TempData["Success"] = $"Proceed with {paymentMethod} payment for invoice #{invoice.InvoiceNumber}";
            return RedirectToAction("MakePayment", "Retainer", new { invoiceId = invoice.Id });
        }

        #endregion

        #region Admin Invoice Management

        // GET: Billing/AdminInvoices - Admin views all invoices with filtering
        [HttpGet]
        public async Task<IActionResult> AdminInvoices(
            string status = null,
            string clientId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string searchTerm = null)
        {
            // Check if user is admin
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                TempData["Error"] = "Access denied. Admin only.";
                return RedirectToAction("Index", "Home");
            }

            var query = _context.Invoices
                .Include(i => i.Client)
                .Include(i => i.Case)
                .Include(i => i.Retainer)
                .Include(i => i.Payments)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<InvoiceStatus>(status, out var statusEnum))
            {
                query = query.Where(i => i.Status == statusEnum);
            }

            if (!string.IsNullOrEmpty(clientId) && int.TryParse(clientId, out var clientIdInt))
            {
                query = query.Where(i => i.ClientId == clientIdInt);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(i => i.IssueDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                var endDate = toDate.Value.AddDays(1);
                query = query.Where(i => i.IssueDate <= endDate);
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(i => i.InvoiceNumber.Contains(searchTerm) ||
                                          i.Description.Contains(searchTerm));
            }

            var invoices = await query
                .OrderByDescending(i => i.IssueDate)
                .ToListAsync();

            // Calculate statistics
            ViewBag.TotalInvoices = invoices.Count;
            ViewBag.TotalRevenue = invoices.Where(i => i.Status == InvoiceStatus.Paid).Sum(i => i.TotalAmount);
            ViewBag.TotalOutstanding = invoices.Where(i => i.Status != InvoiceStatus.Paid).Sum(i => i.TotalAmount - (i.Payments?.Sum(p => p.Amount) ?? 0));
            ViewBag.OverdueInvoices = invoices.Count(i => i.DueDate < DateTime.Now && i.Status != InvoiceStatus.Paid);

            // ⭐ FIX: Use FirstName and LastName separately instead of FullName
            var clients = await _context.Clients
                .Where(c => c.IsActive)
                .ToListAsync();

            // Create SelectList with formatted name
            var clientSelectList = clients.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = $"{c.FirstName} {c.LastName}".Trim()
            }).OrderBy(c => c.Text);

            ViewBag.Clients = new SelectList(clientSelectList, "Value", "Text");
            ViewBag.StatusOptions = Enum.GetValues(typeof(InvoiceStatus));
            ViewBag.CurrentStatus = status;
            ViewBag.SelectedClientId = clientId;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.SearchTerm = searchTerm;

            return View(invoices);
        }

        // GET: Billing/AdminInvoiceDetails/{id} - Admin views invoice details
        [HttpGet]
        public async Task<IActionResult> AdminInvoiceDetails(int id)
        {
            // Check if user is admin
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                TempData["Error"] = "Access denied. Admin only.";
                return RedirectToAction("Index", "Home");
            }

            var invoice = await _context.Invoices
                .Include(i => i.Client)
                .Include(i => i.Case)
                .Include(i => i.Retainer)
                .Include(i => i.Payments)
                .Include(i => i.MatterCostEstimate)
                .Include(i => i.EstimateAuthorisations).ThenInclude(x => x.ApprovedByUser)
                .Include(i => i.Disbursements)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
            {
                TempData["Error"] = "Invoice not found.";
                return RedirectToAction("AdminInvoices");
            }

            var paidAmount = invoice.Payments?.Sum(p => p.Amount) ?? 0;
            ViewBag.PaidAmount = paidAmount;
            ViewBag.BalanceDue = invoice.TotalAmount - paidAmount;

            return View(invoice);
        }

        // GET: Billing/InvoiceAnalytics - Admin dashboard for invoice analytics
        [HttpGet]
        public async Task<IActionResult> InvoiceAnalytics()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                TempData["Error"] = "Access denied. Admin only.";
                return RedirectToAction("Index", "Home");
            }

            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var startOfYear = new DateTime(DateTime.Now.Year, 1, 1);

            // Monthly revenue
            var monthlyRevenue = await _context.Invoices
                .Where(i => i.Status == InvoiceStatus.Paid && i.PaidDate.HasValue && i.PaidDate.Value.Year == DateTime.Now.Year)
                .GroupBy(i => i.PaidDate.Value.Month)
                .Select(g => new { Month = g.Key, Total = g.Sum(i => i.TotalAmount) })
                .ToDictionaryAsync(k => k.Month, v => v.Total);

            // Status distribution
            var statusDistribution = await _context.Invoices
                .GroupBy(i => i.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(k => k.Status.ToString(), v => v.Count);

            // Top clients by revenue
            var topClients = await _context.Invoices
                .Where(i => i.Status == InvoiceStatus.Paid)
                .GroupBy(i => i.ClientId)
                .Select(g => new
                {
                    ClientId = g.Key,
                    Total = g.Sum(i => i.TotalAmount),
                    ClientName = g.First().Client.FullName
                })
                .OrderByDescending(x => x.Total)
                .Take(5)
                .ToListAsync();

            ViewBag.MonthlyRevenue = monthlyRevenue;
            ViewBag.StatusDistribution = statusDistribution;
            ViewBag.TopClients = topClients;
            ViewBag.TotalRevenueThisMonth = await _context.Invoices
                .Where(i => i.Status == InvoiceStatus.Paid && i.PaidDate >= startOfMonth)
                .SumAsync(i => i.TotalAmount);
            ViewBag.TotalRevenueThisYear = await _context.Invoices
                .Where(i => i.Status == InvoiceStatus.Paid && i.PaidDate >= startOfYear)
                .SumAsync(i => i.TotalAmount);

            return View();
        }

        #endregion

        public async Task<IActionResult> InvoiceDetails(int id)
        {
            if (HttpContext.Session.GetString("UserRole") == nameof(UserRole.Client))
                return RedirectToAction(nameof(ClientInvoiceDetails), new { id });
            var invoice = await _context.Invoices
                .Include(i => i.Client)
                .Include(i => i.Case)
                .Include(i => i.Payments)
                .Include(i => i.MatterCostEstimate)
                .Include(i => i.EstimateAuthorisations).ThenInclude(x => x.ApprovedByUser)
                .Include(i => i.Disbursements)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null) return NotFound();

            var paidAmount = invoice.Payments?.Sum(p => p.Amount) ?? 0;
            ViewBag.PaidAmount = paidAmount;
            ViewBag.BalanceDue = invoice.TotalAmount - paidAmount;

            return View(invoice);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendInvoice(int id)
        {
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice == null) return NotFound();
            if (invoice.RequiresEstimateAuthorisation)
            {
                TempData["Error"] = "This invoice cannot be sent until a Director authorises the variance from the locked estimate.";
                return Conflict(new { success = false, message = "Director authorisation required." });
            }

            invoice.Status = InvoiceStatus.Sent;
            await _context.SaveChangesAsync();

            // Send email notification
            TempData["Success"] = $"Invoice #{invoice.InvoiceNumber} sent to client.";
            return RedirectToAction("InvoiceDetails", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveEstimateVariance(int id, string reason)
        {
            var directorId = HttpContext.Session.GetInt32("UserId");
            if (HttpContext.Session.GetString("UserRole") != "Admin" || !directorId.HasValue) return Forbid();
            try
            {
                await _costEstimates.ApproveVarianceAsync(id, directorId.Value, reason, HttpContext.RequestAborted);
                TempData["Success"] = "Invoice variance authorised by the Director.";
            }
            catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
            return RedirectToAction(nameof(AdminInvoiceDetails), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsPaid(int id, decimal amount, string reference)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Client)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null) return NotFound();
            if (invoice.RequiresEstimateAuthorisation)
            {
                TempData["Error"] = "Payment cannot be recorded until a Director authorises the invoice variance.";
                return Conflict("Director authorisation required.");
            }

            var payment = new Payment
            {
                InvoiceId = id,
                ClientId = invoice.ClientId,
                Amount = amount,
                PaymentDate = DateTime.Now,
                TransactionReference = reference,
                CreatedAt = DateTime.Now,
                PaymentMethod = PaymentMethod.EFT,  
                Notes = ""  // Empty string instead of NULL
            };

            _context.Payments.Add(payment);

            var totalPaid = (invoice.Payments?.Sum(p => p.Amount) ?? 0) + amount;
            if (totalPaid >= invoice.TotalAmount)
            {
                invoice.Status = InvoiceStatus.Paid;
                invoice.PaidDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Payment of R {amount:N2} recorded for invoice #{invoice.InvoiceNumber}";
            return RedirectToAction("InvoiceDetails", new { id });
        }

        // TIME TRACKING
        public async Task<IActionResult> TimeEntries(int? caseId)
        {
            var entries = await _context.TimeEntries
    .Include(t => t.Lawyer)
    .Include(t => t.Case)
    .OrderByDescending(t => t.Date)
    .ToListAsync();

            if (caseId.HasValue)
            {
                entries = entries.Where(t => t.CaseId == caseId.Value).ToList();
            }
            ViewBag.Cases = new SelectList(_context.Cases, "Id", "Title");
            return View(entries);
        }

        public IActionResult MyTimeEntries() => RedirectToAction(nameof(TimeEntries));

        public IActionResult LawyerTimeEntries() => RedirectToAction(nameof(TimeEntries));

        public IActionResult AddTimeEntry()
        {
            ViewBag.Lawyers = new SelectList(_context.Users.Where(u => u.Role == UserRole.Lawyer), "Id", "FullName");
            ViewBag.Cases = new SelectList(_context.Cases, "Id", "Title");
            ViewBag.Retainers = new SelectList(_context.Retainers.Where(r => r.Status == RetainerStatus.Active), "Id", "Title");

            return View(new TimeEntry { Date = DateTime.Now, IsBillable = true });
        }

        [HttpPost]
        public async Task<IActionResult> AddTimeEntry(TimeEntry entry)
        {
            if (HttpContext.Session.GetString("UserRole") == "Lawyer")
                entry.LawyerId = HttpContext.Session.GetInt32("UserId")!.Value;
            if (ModelState.IsValid)
            {
                entry.TotalAmount = entry.Hours * entry.HourlyRate;
                entry.CreatedAt = DateTime.Now;

                _context.TimeEntries.Add(entry);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Time entry added successfully!";
                return RedirectToAction("TimeEntries");
            }

            ViewBag.Lawyers = new SelectList(_context.Users.Where(u => u.Role == UserRole.Lawyer), "Id", "FullName");
            ViewBag.Cases = new SelectList(_context.Cases, "Id", "Title");
            ViewBag.Retainers = new SelectList(_context.Retainers.Where(r => r.Status == RetainerStatus.Active), "Id", "Title");
            return View(entry);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTimeEntry(int id)
        {
            var entry = await _context.TimeEntries.FindAsync(id);
            if (entry == null)
            {
                return Json(new { success = false, message = "Time entry not found." });
            }

            _context.TimeEntries.Remove(entry);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Time entry deleted successfully." });
        }

        // TRUST ACCOUNT
        public async Task<IActionResult> TrustAccounts()
        {
            var trustAccounts = await _context.TrustAccounts
                .Include(t => t.Client)
                .ToListAsync();

            return View(trustAccounts);
        }

        public async Task<IActionResult> TrustAccountDetails(int id)
        {
            var account = await _context.TrustAccounts
                .Include(t => t.Client)
                .Include(t => t.Transactions)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (account == null) return NotFound();

            return View(account);
        }

        [HttpPost]
        public async Task<IActionResult> DepositToTrust(int clientId, decimal amount, string description)
        {
            var trustAccount = await _context.TrustAccounts
                .FirstOrDefaultAsync(t => t.ClientId == clientId);

            if (trustAccount == null)
            {
                trustAccount = new TrustAccount
                {
                    ClientId = clientId,
                    Balance = 0,
                    TotalDeposited = 0,
                    TotalWithdrawn = 0,
                    LastUpdated = DateTime.Now
                };
                _context.TrustAccounts.Add(trustAccount);
                await _context.SaveChangesAsync();
            }

            var transaction = new TrustTransaction
            {
                TrustAccountId = trustAccount.Id,
                Type = TransactionType.Deposit,
                Amount = amount,
                Description = description,
                TransactionDate = DateTime.Now
            };

            trustAccount.Balance += amount;
            trustAccount.TotalDeposited += amount;
            trustAccount.LastUpdated = DateTime.Now;

            _context.TrustTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"R {amount:N2} deposited to trust account.";
            return RedirectToAction("TrustAccounts");
        }

        private string GenerateInvoiceNumber()
        {
            var year = DateTime.Now.Year;
            var month = DateTime.Now.Month;
            var count = _context.Invoices.Count(i => i.IssueDate.Year == year) + 1;
            return $"INV-{year}{month:D2}-{count:D4}";
        }
    }
}
