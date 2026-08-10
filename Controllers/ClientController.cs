using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using BCrypt.Net;
using SimplexLawFirm.Services;
using SimplexLawFirm.Infrastructure.Authorization;

namespace SimplexLawFirm.Controllers
{
    [RequireSessionUser, AutoValidateAntiforgeryToken]
    public class ClientController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IVulnerableClientService _safeguards;

        public ClientController(ApplicationDbContext context, IVulnerableClientService safeguards)
        {
            _context = context;
            _safeguards = safeguards;
        }

        // GET: Client/Index
        [RequireSessionRole("Admin", "Lawyer", "Paralegal")]
        public async Task<IActionResult> Index(string searchTerm = null, bool? isBusiness = null)
        {
            var query = _context.Clients.AsQueryable();
            if (HttpContext.Session.GetString("UserRole") == "Lawyer")
            {
                var lawyerId = HttpContext.Session.GetInt32("UserId");
                query = query.Where(c => c.Cases.Any(x => x.LawyerId == lawyerId));
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(c => c.FirstName.Contains(searchTerm) ||
                                         c.LastName.Contains(searchTerm) ||
                                         c.Email.Contains(searchTerm) ||
                                         c.CompanyName.Contains(searchTerm) ||
                                         c.Phone.Contains(searchTerm));
            }

            if (isBusiness.HasValue)
            {
                query = query.Where(c => c.IsBusiness == isBusiness.Value);
            }

            var clients = await query.OrderBy(c => c.LastName).ThenBy(c => c.FirstName).ToListAsync();

            ViewBag.SearchTerm = searchTerm;
            ViewBag.IsBusiness = isBusiness;
            ViewBag.TotalClients = clients.Count;
            ViewBag.IndividualCount = clients.Count(c => !c.IsBusiness);
            ViewBag.BusinessCount = clients.Count(c => c.IsBusiness);

            return View(clients);
        }

        // GET: Client/Details/5
        [RequireSessionRole("Admin", "Lawyer", "Paralegal")]
        public async Task<IActionResult> Details(int id)
        {
            var client = await _context.Clients
                .Include(c => c.Cases)
                    .ThenInclude(cs => cs.Lawyer)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (client == null)
            {
                TempData["Error"] = "Client not found.";
                return RedirectToAction("Index");
            }
            var role = HttpContext.Session.GetString("UserRole");
            var userId = HttpContext.Session.GetInt32("UserId");
            if (role == "Lawyer" && (!userId.HasValue || !client.Cases.Any(x => x.LawyerId == userId.Value))) return Forbid();

            // Get trust account info
            var trustAccount = await _context.TrustAccounts
                .FirstOrDefaultAsync(t => t.ClientId == id);

            // Get active retainers
            var activeRetainers = await _context.Retainers
                .Include(r => r.Template)
                .Where(r => r.ClientId == id && r.Status == RetainerStatus.Active && !r.IsDeleted)
                .ToListAsync();

            // Get recent invoices
            var recentInvoices = await _context.Invoices
                .Where(i => i.ClientId == id)
                .OrderByDescending(i => i.IssueDate)
                .Take(5)
                .ToListAsync();

            ViewBag.TrustAccount = trustAccount;
            ViewBag.ActiveRetainers = activeRetainers;
            ViewBag.RecentInvoices = recentInvoices;
            ViewBag.TotalCases = client.Cases?.Count ?? 0;
            ViewBag.ClientSafeguards = await _safeguards.ActiveFlagsAsync(id);

            return View(client);
        }

        // GET: Client/Create
        [RequireSessionRole("Admin", "Lawyer", "Paralegal")]
        public IActionResult Create()
        {
            ViewBag.ClientTypes = new SelectList(new[]
            {
        new { Value = "false", Text = "Individual" },
        new { Value = "true", Text = "Business" }
    }, "Value", "Text");

            return View(new CreateClientViewModel
            {
                IsBusiness = false // default to Individual
            });
        }

        // POST: Client/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireSessionRole("Admin", "Lawyer", "Paralegal")]
        public async Task<IActionResult> Create(CreateClientViewModel model)
        {
            // 🔥 CONDITIONAL VALIDATION
            if (model.IsBusiness)
            {
                if (string.IsNullOrEmpty(model.CompanyName))
                    ModelState.AddModelError("CompanyName", "Company name is required");
            }
            else
            {
                if (string.IsNullOrEmpty(model.FirstName))
                    ModelState.AddModelError("FirstName", "First name is required");

                if (string.IsNullOrEmpty(model.LastName))
                    ModelState.AddModelError("LastName", "Last name is required");
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please fix the errors before submitting.";
                return View(model);
            }

            // 🔍 DUPLICATE CHECK
            if (await _context.Clients.AnyAsync(c => c.Email == model.Email))
            {
                TempData["Error"] = "Client with this email already exists.";
                return View(model);
            }

            // ✅ CREATE CLIENT
            var client = new Client
            {
                IsBusiness = model.IsBusiness,
                FirstName = model.IsBusiness ? null : model.FirstName,
                LastName = model.IsBusiness ? null : model.LastName,
                SAIDNumber = model.IsBusiness ? null : model.SAIDNumber,
                CompanyName = model.IsBusiness ? model.CompanyName : null,
                RegistrationNumber = model.IsBusiness ? model.RegistrationNumber : null,
                Email = model.Email,
                Phone = model.Phone,
                CreatedAt = DateTime.Now,
                IsActive = true,
                Cases = new List<Case>() // prevents null refs
            };

            _context.Clients.Add(client);
            await _context.SaveChangesAsync();

            // ✅ CREATE USER ACCOUNT
            var password = string.IsNullOrEmpty(model.Password) ? "Client123!" : model.Password;

            var user = new ApplicationUser
            {
                FullName = model.IsBusiness
                    ? model.CompanyName
                    : $"{model.FirstName} {model.LastName}",
                Email = model.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = UserRole.Client,
                IsActive = true,
                EmailConfirmed = true,
                CreatedAt = DateTime.Now,
                AssignedCases = new List<Case>() // prevents null refs
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Client registered successfully!";
            return RedirectToAction("Details", new { id = client.Id });
        }
        // GET: Client/Edit/5
        [RequireSessionRole("Admin", "Lawyer", "Paralegal")]
        public async Task<IActionResult> Edit(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null)
            {
                TempData["Error"] = "Client not found.";
                return RedirectToAction("Index");
            }

            ViewBag.ClientTypes = new SelectList(new[]
            {
                new { Value = "false", Text = "Individual" },
                new { Value = "true", Text = "Business" }
            }, "Value", "Text", client.IsBusiness.ToString());

            return View(client);
        }

        // POST: Client/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireSessionRole("Admin", "Lawyer", "Paralegal")]
        public async Task<IActionResult> Edit(int id, Client client)
        {
            if (id != client.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.Clients.FindAsync(id);
                    if (existing == null)
                    {
                        return NotFound();
                    }

                    // Update client information
                    existing.FirstName = client.FirstName;
                    existing.LastName = client.LastName;
                    existing.Email = client.Email;
                    existing.Phone = client.Phone;
                    existing.SAIDNumber = client.SAIDNumber;
                    existing.CompanyName = client.CompanyName;
                    existing.RegistrationNumber = client.RegistrationNumber;
                    existing.IsBusiness = client.IsBusiness;
                    existing.IsActive = client.IsActive;
                    existing.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();

                    // Update associated user account
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == existing.Email);
                    if (user != null)
                    {
                        user.FullName = existing.IsBusiness ? existing.CompanyName : $"{existing.FirstName} {existing.LastName}";
                        user.IsActive = existing.IsActive;
                        await _context.SaveChangesAsync();
                    }

                    TempData["Success"] = "Client updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Clients.Any(e => e.Id == id))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction("Details", new { id = client.Id });
            }

            ViewBag.ClientTypes = new SelectList(new[]
            {
                new { Value = "false", Text = "Individual" },
                new { Value = "true", Text = "Business" }
            }, "Value", "Text");

            return View(client);
        }

        // GET: Client/Edit (for client self-editing)
        [RequireSessionRole("Client")]
        public async Task<IActionResult> EditProfile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var client = await _context.Clients.FirstOrDefaultAsync(c => c.Email == user.Email);

            if (client == null)
            {
                client = new Client { Email = user.Email };
                return View(client);
            }

            return View(client);
        }

        // POST: Client/EditProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireSessionRole("Client")]
        public async Task<IActionResult> EditProfile(Client client)
        {
            if (ModelState.IsValid)
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    return RedirectToAction("Index", "Home");
                }

                var user = await _context.Users.FindAsync(userId.Value);
                if (user == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                var existing = await _context.Clients.FirstOrDefaultAsync(c => c.Email == user.Email);

                if (existing == null)
                {
                    client.Email = user.Email;
                    client.CreatedAt = DateTime.Now;
                    _context.Clients.Add(client);
                }
                else
                {
                    existing.FirstName = client.FirstName;
                    existing.LastName = client.LastName;
                    existing.Phone = client.Phone;
                    existing.SAIDNumber = client.SAIDNumber;
                    existing.CompanyName = client.CompanyName;
                    existing.RegistrationNumber = client.RegistrationNumber;
                    existing.IsBusiness = client.IsBusiness;
                    existing.UpdatedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Profile updated successfully!";
                return RedirectToAction("Index", "Home");
            }

            return View(client);
        }

        // POST: Client/Delete/5
        [HttpPost]
        [RequireSessionRole("Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null)
            {
                return Json(new { success = false, message = "Client not found" });
            }

            // Soft delete - just deactivate
            client.IsActive = false;
            client.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            // Also deactivate user account
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == client.Email);
            if (user != null)
            {
                user.IsActive = false;
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true, message = "Client deactivated successfully" });
        }

        // GET: Client/GetClientCases
        [HttpGet]
        [RequireSessionRole("Admin", "Lawyer", "Paralegal")]
        public async Task<IActionResult> GetClientCases(int clientId)
        {
            var cases = await _context.Cases
                .Where(c => c.ClientId == clientId)
                .Select(c => new { c.Id, c.Title, c.CaseNumber, c.Status })
                .ToListAsync();

            return Json(cases);
        }
    }
}
