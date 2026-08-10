using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using System.Security.Cryptography;
using System.Text;
using SimplexLawFirm.Services;
using SimplexLawFirm.Infrastructure.Authorization;

namespace SimplexLawFirm.Controllers
{
    [RequireSessionUser, AutoValidateAntiforgeryToken]
    public class CaseController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IMatterCostEstimateService _costEstimates;
        private readonly IPrecedentLibraryService _precedents;
        private readonly IVulnerableClientService _vulnerableClients;

        public CaseController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, IMatterCostEstimateService costEstimates, IPrecedentLibraryService precedents, IVulnerableClientService vulnerableClients)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _costEstimates = costEstimates;
            _precedents = precedents;
            _vulnerableClients = vulnerableClients;
        }

        // GET: Case/Index
        public async Task<IActionResult> Index(string status = null, string searchTerm = null)
        {
            var role = HttpContext.Session.GetString("UserRole");
            var userId = HttpContext.Session.GetInt32("UserId");
            if (role == nameof(UserRole.Client))
                return RedirectToAction(nameof(MyCases), new { status });
            if (!userId.HasValue || role is not ("Admin" or "Lawyer" or "Paralegal")) return Forbid();
            var query = _context.Cases
                .Include(c => c.Client)
                .Include(c => c.Lawyer)
                .AsQueryable();
            if (role == "Lawyer") query = query.Where(c => c.LawyerId == userId.Value);

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<CaseStatus>(status, true, out var statusEnum))
            {
                query = query.Where(c => c.Status == statusEnum);
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(c => c.Title.Contains(searchTerm) ||
                                         c.CaseNumber.Contains(searchTerm) ||
                                         c.Client.FirstName.Contains(searchTerm) ||
                                         c.Client.LastName.Contains(searchTerm) ||
                                         c.Client.CompanyName.Contains(searchTerm));
            }

            var cases = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();

            ViewBag.CurrentStatus = status;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.StatusOptions = Enum.GetValues(typeof(CaseStatus));
            ViewBag.StatusCounts = await GetStatusCounts();

            return View(cases);
        }

        // GET: Case/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var caseItem = await _context.Cases
                .Include(c => c.Client)
                .Include(c => c.Lawyer)
                .Include(c => c.Notes)
                .Include(c => c.Documents)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (caseItem == null)
            {
                TempData["Error"] = "Case not found.";
                return RedirectToAction("Index");
            }
            if (HttpContext.Session.GetString("UserRole") == nameof(UserRole.Client))
            {
                var email = HttpContext.Session.GetString("UserEmail");
                if (!string.Equals(caseItem.Client?.Email, email, StringComparison.OrdinalIgnoreCase)) return NotFound();
            }

            // Get related retainers
            var retainers = await _context.Retainers
                .Where(r => r.CaseId == id && !r.IsDeleted)
                .Include(r => r.Template)
                .ToListAsync();

            // Get upcoming events
            var upcomingEvents = await _context.CalendarEvents
                .Where(e => e.CaseId == id && e.StartDateTime >= DateTime.Now && e.Status != EventStatus.Cancelled)
                .OrderBy(e => e.StartDateTime)
                .Take(5)
                .ToListAsync();

            // Get recent documents
            var recentDocs = await _context.Documents
                .Where(d => d.CaseId == id)
                .OrderByDescending(d => d.UploadedAt)
                .Take(5)
                .ToListAsync();

            // Get time entries for this case
            var timeEntries = await _context.TimeEntries
                .Where(t => t.CaseId == id)
                .OrderByDescending(t => t.Date)
                .Take(10)
                .ToListAsync();

            ViewBag.Retainers = retainers;
            ViewBag.UpcomingEvents = upcomingEvents;
            ViewBag.RecentDocuments = recentDocs;
            ViewBag.TimeEntries = timeEntries;
            ViewBag.TotalBillableHours = timeEntries.Where(t => t.IsBillable).Sum(t => t.Hours);

            return View(caseItem);
        }


        // GET: Case/Create
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();

            var model = new Case
            {
                CaseNumber = await GenerateUniqueCaseNumber(),
                CreatedAt = DateTime.Now,
                Status = CaseStatus.Active,
                Notes = new List<CaseNote>(),      // Initialize collections
                Documents = new List<Document>()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
    Case caseItem,
    List<IFormFile> uploadedDocuments,
    string initialNote)
        {
            // 🔥 REMOVE NAVIGATION VALIDATION ERRORS
            ModelState.Remove("Client");
            ModelState.Remove("Lawyer");
            ModelState.Remove("Notes");
            ModelState.Remove("Documents");

            // 🔥 FORCE SAFE VALUES
            caseItem.Description = caseItem.Description ?? "";
            caseItem.Title = caseItem.Title ?? "";

            // 🔥 VALIDATE IDs MANUALLY (IMPORTANT)
            if (caseItem.ClientId == 0)
                ModelState.AddModelError("ClientId", "Please select a client.");

            // Lawyer is optional → no validation needed

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                TempData["Error"] = string.Join("<br/>", errors);
                await PopulateDropdowns();
                return View(caseItem);
            }

            caseItem.CaseNumber = await GenerateUniqueCaseNumber();
            caseItem.CreatedAt = DateTime.Now;

            // ✅ Add Note
            if (!string.IsNullOrWhiteSpace(initialNote))
            {
                caseItem.Notes.Add(new CaseNote
                {
                    Content = initialNote,
                    CreatedAt = DateTime.Now
                });
            }

            // ✅ FILE UPLOAD FIX
            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            if (uploadedDocuments != null && uploadedDocuments.Count > 0)
            {
                foreach (var file in uploadedDocuments)
                {
                    if (file.Length > 0)
                    {
                        var uniqueName = $"{Guid.NewGuid()}_{file.FileName}";
                        var fullPath = Path.Combine(uploadPath, uniqueName);

                        using (var stream = new FileStream(fullPath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        caseItem.Documents.Add(new Document
                        {
                            FileName = file.FileName,
                            FilePath = "/uploads/" + uniqueName,
                            FileType = file.ContentType,
                            UploadedAt = DateTime.Now
                        });
                    }
                }
            }

            _context.Cases.Add(caseItem);
            await _context.SaveChangesAsync();
            await _costEstimates.TryAutoLinkAsync(caseItem.Id);

            TempData["Success"] = $"Case '{caseItem.Title}' created successfully!";
            return RedirectToAction("Details", new { id = caseItem.Id });
        }

        // Populate dropdowns
        private async Task PopulateDropdowns()
        {
            var clients = await _context.Clients
                .Where(c => c.IsActive)
                .ToListAsync();

            ViewBag.Clients = new SelectList(
                clients.Select(c => new
                {
                    c.Id,
                    Name = c.FullName
                }),
                "Id",
                "Name"
            );

            var lawyers = await _context.Users
                .Where(u => u.Role == UserRole.Lawyer && u.IsActive)
                .ToListAsync();

            ViewBag.Lawyers = new SelectList(
                lawyers.Select(l => new
                {
                    l.Id,
                    Name = l.FullName
                }),
                "Id",
                "Name"
            );

            ViewBag.StatusOptions = new SelectList(Enum.GetValues(typeof(CaseStatus)));
        }

        // Generate unique case number (your original method)
        private async Task<string> GenerateUniqueCaseNumber()
        {
            string caseNumber;
            int count = 0;
            do
            {
                var year = DateTime.Now.Year.ToString().Substring(2);
                var index = _context.Cases.Count(c => c.CreatedAt.Year == DateTime.Now.Year) + 1 + count;
                caseNumber = $"CASE-{year}-{index:D4}";
                count++;
            } while (await _context.Cases.AnyAsync(c => c.CaseNumber == caseNumber));

            return caseNumber;
        }

        // GET: Case/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var caseItem = await _context.Cases.FindAsync(id);
            if (caseItem == null)
            {
                TempData["Error"] = "Case not found.";
                return RedirectToAction("Index");
            }

            ViewBag.Clients = new SelectList(_context.Clients.Where(c => c.IsActive), "Id", "FullName", caseItem.ClientId);

            // IMPORTANT: ONLY show users with Lawyer role
            ViewBag.Lawyers = new SelectList(_context.Users
                .Where(u => u.Role == UserRole.Lawyer && u.IsActive)
                .OrderBy(u => u.FullName), "Id", "FullName", caseItem.LawyerId);

            ViewBag.StatusOptions = new SelectList(Enum.GetValues(typeof(CaseStatus)), caseItem.Status);

            return View(caseItem);
        }

        // POST: Case/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Case caseItem)
        {
            if (id != caseItem.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.Cases.FindAsync(id);
                    if (existing == null)
                    {
                        return NotFound();
                    }

                    var oldStatus = existing.Status;
                    var oldLawyerId = existing.LawyerId;
                    if (oldLawyerId.HasValue && caseItem.LawyerId != oldLawyerId)
                    {
                        TempData["Error"] = "An assigned matter cannot be transferred directly. Approve a reassignment and complete its handover.";
                        return RedirectToAction("Reassign", "Practice", new { id });
                    }
                    if (caseItem.Status == CaseStatus.Closed && oldStatus != CaseStatus.Closed &&
                        await _context.CaseForecasts.AnyAsync(x => x.CaseId == id && x.Status == ForecastStatus.Locked))
                    {
                        TempData["Error"] = "Record the actual outcome to close this forecasted matter.";
                        return RedirectToAction("Forecast", "Practice", new { id });
                    }

                    existing.Title = caseItem.Title;
                    existing.Description = caseItem.Description;
                    existing.CaseType = caseItem.CaseType;
                    existing.EvidenceStrength = Math.Clamp(caseItem.EvidenceStrength, 0, 1);
                    existing.Status = caseItem.Status;
                    existing.LawyerId = caseItem.LawyerId;
                    existing.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();

                    // Create audit log for status change
                    if (oldStatus != existing.Status)
                    {
                        await CreateAuditLog($"Case status changed from {oldStatus} to {existing.Status} for case {existing.CaseNumber}");
                    }

                    // Create audit log for lawyer change
                    if (oldLawyerId != existing.LawyerId)
                    {
                        var newLawyer = await _context.Users.FindAsync(existing.LawyerId);
                        await CreateAuditLog($"Case {existing.CaseNumber} reassigned to {newLawyer?.FullName ?? "Unknown"}");
                    }

                    TempData["Success"] = "Case updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Cases.Any(e => e.Id == id))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction("Details", new { id = caseItem.Id });
            }

            // Repopulate dropdowns on error
            ViewBag.Clients = new SelectList(_context.Clients.Where(c => c.IsActive), "Id", "FullName");
            ViewBag.Lawyers = new SelectList(_context.Users
                .Where(u => u.Role == UserRole.Lawyer && u.IsActive)
                .OrderBy(u => u.FullName), "Id", "FullName");
            ViewBag.StatusOptions = new SelectList(Enum.GetValues(typeof(CaseStatus)));

            return View(caseItem);
        }

        // POST: Case/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var caseItem = await _context.Cases.FindAsync(id);
            if (caseItem == null)
            {
                return Json(new { success = false, message = "Case not found" });
            }

            // Soft delete - just mark as archived
            caseItem.Status = CaseStatus.Archived;
            caseItem.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            await _precedents.WithdrawMatterAsync(caseItem.Id, "Source matter was archived.");

            await CreateAuditLog($"Case {caseItem.CaseNumber} archived");

            return Json(new { success = true, message = "Case archived successfully" });
        }

        // GET: Case/Active
        public async Task<IActionResult> Active()
        {
            var role = HttpContext.Session.GetString("UserRole");
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue || role is not ("Admin" or "Lawyer" or "Paralegal")) return Forbid();
            var query = _context.Cases
                .Include(c => c.Client)
                .Include(c => c.Lawyer)
                .Where(c => c.Status == CaseStatus.Active)
                .AsQueryable();
            if (role == "Lawyer") query = query.Where(c => c.LawyerId == userId.Value);
            var activeCases = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();

            return View(activeCases);
        }

        // GET: Case/AssignLawyer/5
        public async Task<IActionResult> AssignLawyer(int id)
        {
            var caseItem = await _context.Cases
                .Include(c => c.Client)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (caseItem == null)
            {
                TempData["Error"] = "Case not found.";
                return RedirectToAction("Index");
            }

            // IMPORTANT: ONLY get users with Lawyer role for assignment
            var allLawyers = await _context.Users
                .Where(u => u.Role == UserRole.Lawyer && u.IsActive)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            var eligibleLawyers = new List<ApplicationUser>();

            foreach (var lawyer in allLawyers)
            {
                // Check for conflicts (simplified - you can add more complex logic)
                var hasConflict = await CheckLawyerConflict(lawyer.Id, caseItem.ClientId);
                if (!hasConflict)
                {
                    eligibleLawyers.Add(lawyer);
                }
            }

            ViewBag.Case = caseItem;
            ViewBag.EligibleLawyers = new SelectList(eligibleLawyers, "Id", "FullName");
            ViewBag.ConflictInfo = await GetConflictInfo(caseItem.ClientId);
            ViewBag.CurrentLawyer = caseItem.LawyerId.HasValue ?
                await _context.Users.FindAsync(caseItem.LawyerId.Value) : null;

            return View(caseItem);
        }

        // POST: Case/AssignLawyer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignLawyer(int caseId, int lawyerId, string notes)
        {
            var caseItem = await _context.Cases.FindAsync(caseId);
            if (caseItem == null)
            {
                TempData["Error"] = "Case not found.";
                return RedirectToAction("Index");
            }

            // Verify the selected user is actually a lawyer
            var selectedLawyer = await _context.Users.FindAsync(lawyerId);
            if (selectedLawyer == null || selectedLawyer.Role != UserRole.Lawyer)
            {
                TempData["Error"] = "Invalid lawyer selection.";
                return RedirectToAction("AssignLawyer", new { id = caseId });
            }

            // Check for conflicts before assigning
            var hasConflict = await CheckLawyerConflict(lawyerId, caseItem.ClientId);
            if (hasConflict)
            {
                TempData["Error"] = "Cannot assign lawyer due to conflict of interest. This lawyer already has an active case with this client.";
                return RedirectToAction("AssignLawyer", new { id = caseId });
            }

            var oldLawyerId = caseItem.LawyerId;
            caseItem.LawyerId = lawyerId;
            caseItem.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            var newLawyer = await _context.Users.FindAsync(lawyerId);
            await CreateAuditLog($"Lawyer assigned to case {caseItem.CaseNumber}: {newLawyer?.FullName}. Notes: {notes}");

            TempData["Success"] = $"Lawyer {newLawyer?.FullName} assigned to case successfully!";
            return RedirectToAction("Details", new { id = caseId });
        }

        // GET: Case/RemoveLawyer/5
        [HttpPost]
        public async Task<IActionResult> RemoveLawyer(int caseId)
        {
            var caseItem = await _context.Cases.FindAsync(caseId);
            if (caseItem == null)
            {
                return Json(new { success = false, message = "Case not found" });
            }

            var oldLawyer = await _context.Users.FindAsync(caseItem.LawyerId);
            caseItem.LawyerId = null;
            caseItem.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            await CreateAuditLog($"Lawyer {oldLawyer?.FullName} removed from case {caseItem.CaseNumber}");

            return Json(new { success = true, message = "Lawyer removed from case successfully" });
        }

        // GET: Case/AddNote/5
        public async Task<IActionResult> AddNote(int id)
        {
            var caseItem = await _context.Cases.FindAsync(id);
            if (caseItem == null)
            {
                TempData["Error"] = "Case not found.";
                return RedirectToAction("Index");
            }
            var role = HttpContext.Session.GetString("UserRole");
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId is null) return RedirectToAction("Login", "Home");
            if (role != "Admin" && (role != "Lawyer" || caseItem.LawyerId != userId)) return Forbid();
            if (caseItem.Status == CaseStatus.Archived)
            {
                TempData["Error"] = "Archived matters cannot accept new notes.";
                return RedirectToAction("Details", new { id });
            }

            ViewBag.Case = caseItem;
            return View(new CaseNote { CaseId = id, CreatedAt = DateTime.Now });
        }

        // POST: Case/AddNote
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddNote(CaseNote note)
        {
            var matter = await _context.Cases.SingleOrDefaultAsync(x => x.Id == note.CaseId);
            if (matter == null) return NotFound();
            var role = HttpContext.Session.GetString("UserRole");
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId is null) return RedirectToAction("Login", "Home");
            if (role != "Admin" && (role != "Lawyer" || matter.LawyerId != userId)) return Forbid();
            if (matter.Status == CaseStatus.Archived)
            {
                TempData["Error"] = "Archived matters cannot accept new notes.";
                return RedirectToAction("Details", new { id = note.CaseId });
            }
            if (ModelState.IsValid)
            {
                note.CreatedAt = DateTime.Now;
                _context.CaseNotes.Add(note);
                await _context.SaveChangesAsync();
                await _precedents.QueueCaseNoteAsync(note.Id);

                await CreateAuditLog($"Note added to case {note.CaseId}");

                TempData["Success"] = "Note added successfully!";
                return RedirectToAction("Details", new { id = note.CaseId });
            }

            return View(note);
        }

        public async Task<IActionResult> MyCases(string status = null)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("UserRole");

            var query = _context.Cases
                .Include(c => c.Client)
                .AsQueryable();
            if (role == nameof(UserRole.Client))
            {
                var email = HttpContext.Session.GetString("UserEmail");
                query = query.Where(c => c.Client.Email == email);
            }
            else if (role == nameof(UserRole.Lawyer)) query = query.Where(c => c.LawyerId == userId);
            else return Forbid();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<CaseStatus>(status, true, out var statusEnum))
            {
                query = query.Where(c => c.Status == statusEnum);
            }

            var cases = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();

            return role == nameof(UserRole.Client) ? View("ClientMyCases", cases) : View("Lawyer/MyCases", cases);
        }

        public async Task<IActionResult> LawyerCreate()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            ViewBag.Clients = new SelectList(
                await _context.Clients.Where(c => c.IsActive).ToListAsync(),
                "Id", "FullName"
            );

            var model = new Case
            {
                CaseNumber = await GenerateUniqueCaseNumber(),
                CreatedAt = DateTime.Now,
                Status = CaseStatus.Active,
                LawyerId = userId // 🔥 AUTO ASSIGN
            };

            return View("Lawyer/Create", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LawyerCreate(Case caseItem)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            ModelState.Remove("Lawyer");
            ModelState.Remove("Documents");
            ModelState.Remove("Notes");

            if (caseItem.ClientId == 0)
                ModelState.AddModelError("ClientId", "⚠️ Please select a client.");

            if (!ModelState.IsValid)
            {
                // 🔥 DEBUG: SHOW EXACT ERRORS
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                TempData["Error"] = string.Join("<br/>", errors);

                ViewBag.Clients = new SelectList(
                    await _context.Clients.Where(c => c.IsActive).ToListAsync(),
                    "Id", "FullName"
                );

                return View("Lawyer/Create", caseItem);
            }

            caseItem.LawyerId = userId;
            caseItem.CreatedAt = DateTime.Now;
            caseItem.CaseNumber = await GenerateUniqueCaseNumber();

            _context.Cases.Add(caseItem);
            await _context.SaveChangesAsync();
            if (caseItem.Status == CaseStatus.Archived)
                await _precedents.WithdrawMatterAsync(caseItem.Id, "Source matter was archived.");
            await _costEstimates.TryAutoLinkAsync(caseItem.Id);

            TempData["Success"] = $"✅ Case '{caseItem.Title}' created successfully!";
            return RedirectToAction("MyCases");
        }

        public async Task<IActionResult> LawyerEdit(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            var caseItem = await _context.Cases
                .FirstOrDefaultAsync(c => c.Id == id && c.LawyerId == userId);

            if (caseItem == null)
            {
                TempData["Error"] = "Unauthorized access!";
                return RedirectToAction("MyCases");
            }

            return View("Lawyer/Edit", caseItem);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LawyerEdit(int id, Case model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            var caseItem = await _context.Cases
                .FirstOrDefaultAsync(c => c.Id == id && c.LawyerId == userId);

            if (caseItem == null)
                return Unauthorized();
            if (model.Status == CaseStatus.Closed && caseItem.Status != CaseStatus.Closed &&
                await _context.CaseForecasts.AnyAsync(x => x.CaseId == id && x.Status == ForecastStatus.Locked))
            {
                TempData["Error"] = "A Director must record the actual outcome before this forecasted matter can close.";
                return RedirectToAction("Forecast", "Practice", new { id });
            }

            caseItem.Title = model.Title;
            caseItem.Description = model.Description;
            caseItem.CaseType = model.CaseType;
            caseItem.EvidenceStrength = Math.Clamp(model.EvidenceStrength, 0, 1);
            caseItem.Status = model.Status;
            caseItem.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            if (caseItem.Status == CaseStatus.Archived)
                await _precedents.WithdrawMatterAsync(caseItem.Id, "Source matter was archived.");

            TempData["Success"] = "Case updated!";
            return RedirectToAction("MyCases");
        }

        [HttpPost]
        public async Task<IActionResult> LawyerArchive(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            var caseItem = await _context.Cases
                .FirstOrDefaultAsync(c => c.Id == id && c.LawyerId == userId);

            if (caseItem == null)
                return Json(new { success = false });

            caseItem.Status = CaseStatus.Archived;
            await _context.SaveChangesAsync();
            await _precedents.WithdrawMatterAsync(caseItem.Id, "Source matter was archived.");

            return Json(new { success = true });
        }

        // GET: Case/CaseHistory/5
        public async Task<IActionResult> CaseHistory(int id)
        {
            var caseItem = await _context.Cases
                .Include(c => c.Client)
                .Include(c => c.Lawyer)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (caseItem == null)
            {
                TempData["Error"] = "Case not found.";
                return RedirectToAction("Index");
            }
            var staffId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("UserRole");
            if (!staffId.HasValue || role is not ("Admin" or "Lawyer")) return Forbid();
            if (role == "Lawyer" && caseItem.LawyerId != staffId.Value) return Forbid();
            if (staffId.HasValue && role is "Admin" or "Lawyer" or "Paralegal" or "Accountant")
            {
                var unacknowledged = await _vulnerableClients.UnacknowledgedAsync(id, staffId.Value);
                if (unacknowledged.Count > 0)
                    return RedirectToAction("Acknowledge", "VulnerableClient", new { caseId = id });
            }
            ViewBag.ClientSafeguards = await _vulnerableClients.ActiveFlagsAsync(caseItem.ClientId);

            // Get all case activities
            var activities = new List<CaseActivityViewModel>();

            // Get notes
            var notes = await _context.CaseNotes
                .Where(n => n.CaseId == id)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
            foreach (var note in notes)
            {
                activities.Add(new CaseActivityViewModel
                {
                    Date = note.CreatedAt,
                    Type = "Note",
                    Description = note.Content,
                    User = "System"
                });
            }

            // Get document uploads
            var documents = await _context.Documents
                .Where(d => d.CaseId == id)
                .Include(d => d.UploadedBy)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();
            foreach (var doc in documents)
            {
                activities.Add(new CaseActivityViewModel
                {
                    Date = doc.UploadedAt,
                    Type = "Document",
                    Description = $"Uploaded: {doc.FileName}",
                    User = doc.UploadedBy?.FullName ?? "Unknown"
                });
            }

            // Get calendar events
            var events = await _context.CalendarEvents
                .Where(e => e.CaseId == id)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
            foreach (var evt in events)
            {
                activities.Add(new CaseActivityViewModel
                {
                    Date = evt.CreatedAt,
                    Type = "Event",
                    Description = $"Event: {evt.Title} on {evt.StartDateTime:dd MMM yyyy}",
                    User = evt.CreatedByUser?.FullName ?? "System"
                });
            }

            // Get time entries
            var timeEntries = await _context.TimeEntries
                .Where(t => t.CaseId == id)
                .Include(t => t.Lawyer)
                .OrderByDescending(t => t.Date)
                .ToListAsync();
            foreach (var entry in timeEntries)
            {
                activities.Add(new CaseActivityViewModel
                {
                    Date = entry.Date,
                    Type = "Time Entry",
                    Description = $"{entry.Hours} hours - {entry.Description}",
                    User = entry.Lawyer?.FullName ?? "Unknown"
                });
            }

            ViewBag.Case = caseItem;
            ViewBag.Activities = activities.OrderByDescending(a => a.Date).ToList();

            return View();
        }

        // GET: Case/PerformanceReport
        public async Task<IActionResult> PerformanceReport(DateTime? startDate, DateTime? endDate)
        {
            startDate ??= DateTime.Now.AddMonths(-1);
            endDate ??= DateTime.Now;

            var cases = await _context.Cases
                .Include(c => c.Lawyer)
                .Include(c => c.Client)
                .Where(c => c.CreatedAt >= startDate && c.CreatedAt <= endDate)
                .ToListAsync();

            // Calculate lawyer performance metrics - ONLY for lawyers
            var lawyerPerformance = cases
                .Where(c => c.LawyerId.HasValue)
                .GroupBy(c => c.LawyerId)
                .Select(g => new LawyerPerformanceViewModel
                {
                    LawyerId = g.Key.Value,
                    LawyerName = g.First().Lawyer?.FullName ?? "Unknown",
                    TotalCases = g.Count(),
                    ActiveCases = g.Count(c => c.Status == CaseStatus.Active),
                    ClosedCases = g.Count(c => c.Status == CaseStatus.Closed),
                    PendingCases = g.Count(c => c.Status == CaseStatus.Pending),
                    AvgResolutionDays = CalculateAvgResolutionDays(g)
                })
                .OrderByDescending(l => l.TotalCases)
                .ToList();

            // Get billable hours by lawyer
            foreach (var perf in lawyerPerformance)
            {
                var billableHours = await _context.TimeEntries
                    .Where(t => t.LawyerId == perf.LawyerId &&
                               t.Date >= startDate &&
                               t.Date <= endDate &&
                               t.IsBillable)
                    .SumAsync(t => t.Hours);
                perf.BillableHours = billableHours;

                var lawyerProfile = await _context.LawyerProfiles
                    .FirstOrDefaultAsync(lp => lp.UserId == perf.LawyerId);
                perf.HourlyRate = lawyerProfile?.HourlyRate ?? 2000;
                perf.EstimatedRevenue = billableHours * perf.HourlyRate;
            }

            ViewBag.StartDate = startDate.Value.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate.Value.ToString("yyyy-MM-dd");
            ViewBag.TotalCases = cases.Count;
            ViewBag.ActiveCases = cases.Count(c => c.Status == CaseStatus.Active);
            ViewBag.ClosedCases = cases.Count(c => c.Status == CaseStatus.Closed);
            ViewBag.PendingCases = cases.Count(c => c.Status == CaseStatus.Pending);
            ViewBag.LawyerPerformance = lawyerPerformance;

            return View();
        }

        // GET: Case/GetLawyersList (AJAX endpoint for dynamic lawyer selection)
        [HttpGet]
        public async Task<IActionResult> GetLawyersList(int? excludeClientId = null)
        {
            var query = _context.Users
                .Where(u => u.Role == UserRole.Lawyer && u.IsActive)
                .Select(u => new { u.Id, u.FullName, u.Email });

            var lawyers = await query.OrderBy(u => u.FullName).ToListAsync();
            return Json(lawyers);
        }

        // GET: Case/GetCaseTimeline (AJAX endpoint for case timeline)
        [HttpGet]
        public async Task<IActionResult> GetCaseTimeline(int caseId)
        {
            var activities = new List<object>();

            var notes = await _context.CaseNotes
                .Where(n => n.CaseId == caseId)
                .Select(n => new { n.CreatedAt, Type = "Note", n.Content })
                .ToListAsync();
            activities.AddRange(notes);

            var documents = await _context.Documents
                .Where(d => d.CaseId == caseId)
                .Select(d => new { d.UploadedAt, Type = "Document", Content = d.FileName })
                .ToListAsync();
            activities.AddRange(documents);

            return Json(activities.OrderBy(a => a.GetType().GetProperty("CreatedAt")?.GetValue(a)));
        }

        // Helper Methods
        private async Task<Dictionary<CaseStatus, int>> GetStatusCounts()
        {
            var counts = await _context.Cases
                .GroupBy(c => c.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(k => k.Status, v => v.Count);

            return counts;
        }

        private string GenerateCaseNumber()
        {
            var year = DateTime.Now.Year;
            var prefix = year.ToString().Substring(2);
            var count = _context.Cases.Count(c => c.CreatedAt.Year == DateTime.Now.Year) + 1;
            return $"CASE-{prefix}-{count:D4}";
        }

        private async Task<bool> CheckLawyerConflict(int lawyerId, int clientId)
        {
            // Check if lawyer has any active cases with the same client
            var hasActiveCase = await _context.Cases
                .AnyAsync(c => c.LawyerId == lawyerId &&
                              c.ClientId == clientId &&
                              c.Status == CaseStatus.Active);

            // Add more conflict checks as needed (e.g., opposing parties, etc.)
            return hasActiveCase;
        }

        private async Task<string> GetConflictInfo(int clientId)
        {
            var conflicts = await _context.Cases
                .Where(c => c.ClientId == clientId && c.Status == CaseStatus.Active && c.LawyerId.HasValue)
                .Select(c => c.LawyerId)
                .Distinct()
                .ToListAsync();

            if (!conflicts.Any())
                return "No conflicts detected.";

            var lawyers = await _context.Users
                .Where(u => conflicts.Contains(u.Id))
                .Select(u => u.FullName)
                .ToListAsync();

            return $"Conflict detected with: {string.Join(", ", lawyers)}";
        }

        private double CalculateAvgResolutionDays(IGrouping<int?, Case> cases)
        {
            var resolvedCases = cases.Where(c => c.Status == CaseStatus.Closed && c.UpdatedAt.HasValue);
            if (!resolvedCases.Any()) return 0;

            return resolvedCases.Average(c => (c.UpdatedAt.Value - c.CreatedAt).TotalDays);
        }

        private async Task CreateAuditLog(string action)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userName = HttpContext.Session.GetString("UserName") ?? "System";
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            // In a real implementation, save to an AuditLog table
            System.Diagnostics.Debug.WriteLine($"AUDIT: [{DateTime.Now}] User: {userName} (ID: {userId}) - {action} - IP: {ipAddress}");
        }
    }

    // View Models
    public class CaseActivityViewModel
    {
        public DateTime Date { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public string User { get; set; }
    }

    public class LawyerPerformanceViewModel
    {
        public int LawyerId { get; set; }
        public string LawyerName { get; set; }
        public int TotalCases { get; set; }
        public int ActiveCases { get; set; }
        public int ClosedCases { get; set; }
        public int PendingCases { get; set; }
        public double AvgResolutionDays { get; set; }
        public decimal BillableHours { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal EstimatedRevenue { get; set; }
    }
}
