using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using System.Security.Cryptography;
using System.Text;
using SimplexLawFirm.Infrastructure.Authorization;
using SimplexLawFirm.Services.Notifications;

namespace SimplexLawFirm.Controllers
{
    [RequireSessionUser, AutoValidateAntiforgeryToken]
    public class RetainerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly INotificationService _notifications;

        public RetainerController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, INotificationService notifications)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _notifications = notifications;
        }

        #region Template Management (Working Fine - No Changes)

        // GET: Retainer/Templates
        public async Task<IActionResult> Templates()
        {
            var templates = await _context.RetainerTemplates
                .OrderBy(t => t.DisplayOrder)
                .ToListAsync();
            return View(templates);
        }

        // GET: Retainer/CreateTemplate
        public IActionResult CreateTemplate()
        {
            ViewBag.RetainerTypes = new SelectList(Enum.GetValues(typeof(RetainerType)));
            return View();
        }

        // POST: Retainer/CreateTemplate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTemplate(RetainerTemplate template)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.RetainerTypes = new SelectList(
                    Enum.GetValues(typeof(RetainerType))
                        .Cast<RetainerType>()
                        .Select(e => new { Id = e, Name = e.ToString() }),
                    "Id",
                    "Name"
                );
                return View(template);
            }

            _context.RetainerTemplates.Add(template);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Template '{template.Name}' created successfully!";
            return RedirectToAction("Templates");
        }

        // GET: Retainer/EditTemplate/5
        public async Task<IActionResult> EditTemplate(int id)
        {
            var template = await _context.RetainerTemplates.FindAsync(id);
            if (template == null)
            {
                TempData["Error"] = "Template not found.";
                return RedirectToAction("Templates");
            }
            ViewBag.RetainerTypes = new SelectList(Enum.GetValues(typeof(RetainerType)), template.Type);
            return View(template);
        }

        // POST: Retainer/EditTemplate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTemplate(int id, RetainerTemplate template)
        {
            if (id != template.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(template);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Template '{template.Name}' updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.RetainerTemplates.Any(e => e.Id == id))
                    {
                        TempData["Error"] = "Template not found.";
                        return RedirectToAction("Templates");
                    }
                    throw;
                }
                return RedirectToAction("Templates");
            }
            ViewBag.RetainerTypes = new SelectList(Enum.GetValues(typeof(RetainerType)), template.Type);
            return View(template);
        }

        // POST: Retainer/DeleteTemplate/5
        [HttpPost]
        public async Task<IActionResult> DeleteTemplate(int id)
        {
            var template = await _context.RetainerTemplates.FindAsync(id);
            if (template == null)
            {
                return Json(new { success = false, message = "Template not found" });
            }

            template.IsActive = false;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Template archived successfully" });
        }

        // POST: Retainer/PublishTemplate/5
        [HttpPost]
        public async Task<IActionResult> PublishTemplate(int id)
        {
            var template = await _context.RetainerTemplates.FindAsync(id);
            if (template == null)
            {
                return Json(new { success = false, message = "Template not found" });
            }

            template.IsPublic = !template.IsPublic;
            await _context.SaveChangesAsync();

            return Json(new { success = true, isPublic = template.IsPublic, message = template.IsPublic ? "Template published to client portal" : "Template hidden from client portal" });
        }

        #endregion

        #region Public Service Catalog (Client Facing)

        // GET: Retainer/ServiceCatalog
        public async Task<IActionResult> ServiceCatalog()
        {
            var templates = await _context.RetainerTemplates
                .Where(t => t.IsPublic && t.IsActive)
                .OrderBy(t => t.DisplayOrder)
                .ToListAsync();

            var categories = templates.GroupBy(t => t.Category ?? "General")
                .ToDictionary(g => g.Key, g => g.ToList());

            ViewBag.Categories = categories;
            return View(templates);
        }

        // GET: Retainer/ServiceDetails/5
        public async Task<IActionResult> ServiceDetails(int id)
        {
            var template = await _context.RetainerTemplates.FindAsync(id);
            if (template == null)
            {
                TempData["Error"] = "Service package not found.";
                return RedirectToAction("ServiceCatalog");
            }
            return View(template);
        }


        // GET: api/retainer/client/{clientId}/cases
        [HttpGet("api/retainer/client/{clientId}/cases")]
        public async Task<IActionResult> GetClientCases(int clientId)
        {
            try
            {
                var cases = await _context.Cases
                    .Where(c => c.ClientId == clientId && c.Status != CaseStatus.Closed)
                    .Select(c => new
                    {
                        id = c.Id,
                        title = c.Title ?? $"Case #{c.Id}"  // Fallback if Title is null
                    })
                    .ToListAsync();

                // If no cases found, return empty array instead of error
                return Ok(cases);
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error loading cases for client {clientId}: {ex.Message}");
                return Ok(new List<object>()); // Return empty array instead of error
            }
        }

        // GET: api/retainer/template/{id}
        [HttpGet("api/retainer/template/{id}")]
        public async Task<IActionResult> GetTemplateApi(int id)
        {
            try
            {
                var template = await _context.RetainerTemplates.FindAsync(id);
                if (template == null)
                {
                    return NotFound(new { message = "Template not found" });
                }

                return Ok(new
                {
                    id = template.Id,
                    name = template.Name ?? "",
                    description = template.Description ?? "",
                    type = template.Type.ToString(),
                    basePrice = template.BasePrice,
                    includedHours = template.IncludedHours,
                    overageRate = template.OverageRate,
                    billingCycle = template.BillingCycle ?? "One-time"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading template {id}: {ex.Message}");
                return StatusCode(500, new { message = "Error loading template" });
            }
        }


        // GET: Retainer/SelectService/5 - Client selects a template to create a retainer request
        public async Task<IActionResult> SelectService(int id)
        {
            var template = await _context.RetainerTemplates.FindAsync(id);
            if (template == null)
            {
                TempData["Error"] = "Service package not found.";
                return RedirectToAction("ServiceCatalog");
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                TempData["Error"] = "Please log in to request services.";
                return RedirectToAction("Index", "Home");
            }

            var user = await _context.Users.FindAsync(userId.Value);
            var client = await _context.Clients.FirstOrDefaultAsync(c => c.Email == user.Email);

            if (client == null)
            {
                TempData["Error"] = "Please complete your client profile before requesting services.";
                return RedirectToAction("Edit", "Client");
            }

            var model = new ClientServiceSelectionViewModel
            {
                TemplateId = id,
                Template = template,
                ClientId = client.Id,
                Client = client
            };

            return View(model);
        }

        // POST: Retainer/SelectService - Client submits service request
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SelectService(ClientServiceSelectionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var selectedTemplate = await _context.RetainerTemplates.FindAsync(model.TemplateId);
                model.Template = selectedTemplate;
                return View(model);
            }

            var template = await _context.RetainerTemplates.FindAsync(model.TemplateId);
            if (template == null)
            {
                TempData["Error"] = "Service package not found.";
                return RedirectToAction("ServiceCatalog");
            }

            // Create retainer in PENDING LAWYER APPROVAL state
            var retainer = new Retainer
            {
                ClientId = model.ClientId,
                TemplateId = model.TemplateId,
                Title = template.Name,
                ScopeOfWork = template.Description,
                SpecialTerms = template.Exclusions,
                Type = template.Type,
                Amount = template.BasePrice,
                IncludedHours = template.IncludedHours,
                OverageRate = template.OverageRate,
                BillingCycle = template.BillingCycle,
                StartDate = DateTime.Now,
                Status = RetainerStatus.PendingApproval, // Immediately pending lawyer approval
                CreatedDate = DateTime.Now,
                AdminNotes = $"Created from client selection. Client notes: {model.ClientNotes}",
                Source = RetainerSource.ClientPortal
            };

            _context.Retainers.Add(retainer);
            await _context.SaveChangesAsync();

            // Log the action
            await LogRetainerAction(retainer.Id, "Created", "Client requested retainer from service catalog", GetCurrentUserId());

            // Notify assigned lawyer (you can implement email notification)
            await NotifyLawyerPendingApproval(retainer.Id);

            TempData["Success"] = "Your service request has been submitted for lawyer approval. You will be notified once reviewed.";
            return RedirectToAction("MyRetainers");
        }

        [HttpGet]
        public async Task<IActionResult> RequestService(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var email = userId.HasValue ? await _context.Users.Where(x => x.Id == userId.Value).Select(x => x.Email).SingleOrDefaultAsync() : null;
            var client = email is null ? null : await _context.Clients.SingleOrDefaultAsync(x => x.Email == email);
            var template = await _context.RetainerTemplates.SingleOrDefaultAsync(x => x.Id == id && x.IsActive);
            if (client is null) return Forbid();
            if (template is null) return NotFound();
            ViewBag.Client = client;
            ViewBag.Template = template;
            return View(new ClientRequest { ClientId = client.Id, TemplateId = template.Id, Urgency = "Medium", PreferredContact = "Email" });
        }

        [HttpPost]
        public async Task<IActionResult> RequestService(ClientRequest model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var email = userId.HasValue ? await _context.Users.Where(x => x.Id == userId.Value).Select(x => x.Email).SingleOrDefaultAsync() : null;
            var client = email is null ? null : await _context.Clients.SingleOrDefaultAsync(x => x.Email == email);
            var template = await _context.RetainerTemplates.SingleOrDefaultAsync(x => x.Id == model.TemplateId && x.IsActive);
            if (client is null) return Forbid();
            if (template is null) return NotFound();
            model.ClientId = client.Id;
            model.Client = null;
            model.Template = null!;
            model.Status = "Pending";
            model.CreatedDate = DateTime.UtcNow;
            if (!ModelState.IsValid)
            {
                ViewBag.Client = client;
                ViewBag.Template = template;
                return View(model);
            }
            _context.ClientRequests.Add(model);
            _context.AuditEntries.Add(new AuditEntry { ActorUserId = userId, EntityType = "ClientRequest", EntityId = "pending", Action = "Service requested", SafeMetadataJson = System.Text.Json.JsonSerializer.Serialize(new { clientId = client.Id, templateId = template.Id, model.Urgency }) });
            await _context.SaveChangesAsync();
            TempData["Success"] = "Your service request was submitted for review.";
            return RedirectToAction(nameof(MyRetainers));
        }

        #endregion

        #region Admin/Staff Retainer Management

        // GET: Retainer/Index
        public async Task<IActionResult> Index(string status = null)
        {
            var query = _context.Retainers
                .Include(r => r.Client)
                .Include(r => r.Case)
                .Include(r => r.Template)
                .Where(r => !r.IsDeleted);

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<RetainerStatus>(status, out var statusEnum))
            {
                query = query.Where(r => r.Status == statusEnum);
            }

            var retainers = await query.OrderByDescending(r => r.CreatedDate).ToListAsync();

            ViewBag.CurrentStatus = status;
            ViewBag.StatusOptions = Enum.GetValues(typeof(RetainerStatus)).Cast<RetainerStatus>();
            ViewBag.StatusCounts = await GetStatusCounts();

            return View(retainers);
        }

        // GET: Retainer/Create - Admin/Paralegal creates a retainer draft
        [HttpGet]
        public async Task<IActionResult> Create(int? clientId, int? caseId)
        {
            var clients = await _context.Clients.Where(c => c.IsActive).ToListAsync();
            ViewBag.Clients = new SelectList(clients, "Id", "FullName");
            ViewBag.Cases = new SelectList(_context.Cases.Where(c => c.Status != CaseStatus.Closed), "Id", "Title");
            ViewBag.Templates = new SelectList(_context.RetainerTemplates.Where(t => t.IsActive), "Id", "Name");
            ViewBag.RetainerTypes = new SelectList(Enum.GetValues(typeof(RetainerType)));
            ViewBag.Lawyers = new SelectList(_context.Users.Where(u => u.Role == UserRole.Lawyer), "Id", "FullName");

            var model = new Retainer
            {
                StartDate = DateTime.Now,
                Type = RetainerType.Fixed,
                Status = RetainerStatus.Draft, // Draft until submitted to lawyer
                CreatedDate = DateTime.Now,
                Source = RetainerSource.AdminCreated
            };

            if (clientId.HasValue)
            {
                model.ClientId = clientId.Value;
            }

            if (caseId.HasValue)
            {
                model.CaseId = caseId.Value;
            }

            return View(model);
        }

        // POST: Retainer/Create - Save as DRAFT
        [HttpPost]
        public async Task<IActionResult> Create(Retainer retainer, string action)
        {
            // ⚠️ CRITICAL: Remove validation for navigation properties
            ModelState.Remove("Client");
            ModelState.Remove("Case");
            ModelState.Remove("Template");
            ModelState.Remove("ApprovedByUser");
            ModelState.Remove("SubmittedByUser");
            ModelState.Remove("AssignedLawyer");
            ModelState.Remove("ActionLogs");
            ModelState.Remove("Renewals");
            ModelState.Remove("PaymentSchedules");
            ModelState.Remove("Payments");
            ModelState.Remove("ApprovedByUserId");
            ModelState.Remove("SubmittedByUserId");
            ModelState.Remove("CancelledByUserId");
            ModelState.Remove("RejectedByUserId");
            ModelState.Remove("RevisionRequestedByUserId");

            // 🔒 SERVER-SIDE VALIDATION
            if (retainer.ClientId <= 0)
                ModelState.AddModelError("ClientId", "Please select a client.");

            if (string.IsNullOrWhiteSpace(retainer.Title))
                ModelState.AddModelError("Title", "Retainer title is required.");

            if (string.IsNullOrWhiteSpace(retainer.ScopeOfWork))
                ModelState.AddModelError("ScopeOfWork", "Scope of work is required.");

            if (retainer.Amount <= 0)
                ModelState.AddModelError("Amount", "Amount must be greater than 0.");

            // Auto-set StartDate if default
            if (retainer.StartDate == default(DateTime))
            {
                retainer.StartDate = DateTime.Now;
            }

            // Validate EndDate
            if (retainer.EndDate.HasValue && retainer.EndDate < retainer.StartDate)
            {
                ModelState.AddModelError("EndDate", "End date cannot be before start date.");
            }

            if (!ModelState.IsValid)
            {
                await LoadCreateViewBags(retainer);
                TempData["Error"] = "Please correct the errors below.";
                return View(retainer);
            }

            try
            {
                // 🛡️ SET ALL STRING FIELDS TO AVOID NULLS (THIS IS THE FIX)
                retainer.Title = retainer.Title?.Trim() ?? "";
                retainer.ScopeOfWork = retainer.ScopeOfWork?.Trim() ?? "";
                retainer.SpecialTerms = retainer.SpecialTerms ?? "";
                retainer.BillingCycle = string.IsNullOrWhiteSpace(retainer.BillingCycle) ? "One-time" : retainer.BillingCycle;
                retainer.LawyerNotes = retainer.LawyerNotes ?? "";
                retainer.AdminNotes = retainer.AdminNotes ?? "";
                retainer.ClientSignatureName = retainer.ClientSignatureName ?? "";
                retainer.ClientIPAddress = retainer.ClientIPAddress ?? "";
                retainer.PaymentReference = retainer.PaymentReference ?? "";
                retainer.RejectionReason = retainer.RejectionReason ?? "";
                retainer.ClientChangeRequest = retainer.ClientChangeRequest ?? "";
                retainer.CancellationReason = retainer.CancellationReason ?? "";  // ⭐ THIS WAS THE MISSING ONE
                retainer.PdfPath = retainer.PdfPath ?? "";

                // 🛡️ Set defaults for value types
                if (retainer.PaymentDueDays <= 0) retainer.PaymentDueDays = 7;
                if (retainer.IncludedHours < 0) retainer.IncludedHours = 0;
                if (retainer.OverageRate < 0) retainer.OverageRate = 0;

                // 🧱 SYSTEM FIELDS
                retainer.CreatedDate = DateTime.Now;
                retainer.Status = RetainerStatus.Draft;
                retainer.Source = RetainerSource.AdminCreated;

                // Generate a signature token for future use
                retainer.SignatureToken = GenerateSignatureToken();
                retainer.SignatureTokenExpiry = DateTime.Now.AddDays(90);

                // Handle nullable foreign keys
                if (retainer.AssignedLawyerId == 0) retainer.AssignedLawyerId = null;
                if (retainer.CaseId == 0) retainer.CaseId = null;
                if (retainer.TemplateId == 0) retainer.TemplateId = null;

                _context.Retainers.Add(retainer);
                await _context.SaveChangesAsync();

                // 📝 LOGGING
                await LogRetainerAction(
                    retainer.Id,
                    "Created",
                    $"Retainer draft created by {GetCurrentUserName()} for client ID {retainer.ClientId}",
                    GetCurrentUserId()
                );

                TempData["Success"] = $"Retainer draft '{retainer.Title}' created successfully!";

                if (action == "createAndContinue")
                {
                    return RedirectToAction("Create", new { clientId = retainer.ClientId, caseId = retainer.CaseId });
                }

                return RedirectToAction("Details", new { id = retainer.Id });
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException?.InnerException?.Message ?? ex.InnerException?.Message ?? ex.Message;
                Console.WriteLine($"Error creating retainer: {innerMessage}");
                TempData["Error"] = $"Error: {innerMessage}";
                await LoadCreateViewBags(retainer);
                return View(retainer);
            }
        }

        private async Task LoadCreateViewBags(Retainer retainer = null)
        {
            // Get clients (only once)
            var clients = await _context.Clients.Where(c => c.IsActive).ToListAsync();
            ViewBag.Clients = new SelectList(clients, "Id", "FullName", retainer?.ClientId);

            // Get cases
            var cases = await _context.Cases.Where(c => c.Status != CaseStatus.Closed).ToListAsync();
            ViewBag.Cases = new SelectList(cases, "Id", "Title", retainer?.CaseId);

            // Get templates
            var templates = await _context.RetainerTemplates.Where(t => t.IsActive).ToListAsync();
            ViewBag.Templates = new SelectList(templates, "Id", "Name", retainer?.TemplateId);

            // ✅ FIXED: RetainerTypes - proper SelectList
            var retainerTypes = Enum.GetValues(typeof(RetainerType))
                .Cast<RetainerType>()
                .Select(e => new SelectListItem
                {
                    Value = ((int)e).ToString(),
                    Text = e.ToString(),
                    Selected = retainer != null && retainer.Type == e
                }).ToList();

            ViewBag.RetainerTypes = new SelectList(retainerTypes, "Value", "Text");

            // Get lawyers
            var lawyers = await _context.Users.Where(u => u.Role == UserRole.Lawyer).ToListAsync();
            ViewBag.Lawyers = new SelectList(lawyers, "Id", "FullName", retainer?.AssignedLawyerId);
        }



        // POST: Retainer/CreateAndContinue - Create draft and stay on form
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAndContinue(Retainer retainer)
        {
            if (ModelState.IsValid)
            {
                retainer.CreatedDate = DateTime.Now;
                retainer.Status = RetainerStatus.Draft;
                retainer.Source = RetainerSource.AdminCreated;

                _context.Retainers.Add(retainer);
                await _context.SaveChangesAsync();

                await LogRetainerAction(retainer.Id, "Created", $"Retainer draft created by {GetCurrentUserName()}", GetCurrentUserId());

                TempData["Success"] = $"Retainer '{retainer.Title}' created successfully! You can create another one below.";
                
                return RedirectToAction("Create", new { 
                    clientId = retainer.ClientId, 
                    caseId = retainer.CaseId 
                });
            }

            var clients = await _context.Clients.Where(c => c.IsActive).ToListAsync();
            ViewBag.Clients = new SelectList(clients, "Id", "FullName", retainer.ClientId);
            ViewBag.Cases = new SelectList(_context.Cases.Where(c => c.Status != CaseStatus.Closed), "Id", "Title", retainer.CaseId);
            ViewBag.Templates = new SelectList(_context.RetainerTemplates.Where(t => t.IsActive), "Id", "Name", retainer.TemplateId);
            ViewBag.RetainerTypes = new SelectList(Enum.GetValues(typeof(RetainerType)), retainer.Type);
            ViewBag.Lawyers = new SelectList(_context.Users.Where(u => u.Role == UserRole.Lawyer), "Id", "FullName");

            TempData["Error"] = "Please correct the errors below.";
            return View("Create", retainer);
        }

        // POST: Retainer/SubmitForApproval - Admin/Paralegal submits draft to lawyer
        [HttpPost]
        public async Task<IActionResult> SubmitForApproval(int id, int? assignedLawyerId)
        {
            var retainer = await _context.Retainers.FindAsync(id);
            if (retainer == null)
            {
                return Json(new { success = false, message = "Retainer not found." });
            }

            if (retainer.Status != RetainerStatus.Draft && retainer.Status != RetainerStatus.Rejected)
            {
                return Json(new { success = false, message = "Only draft or rejected retainers can be submitted for approval." });
            }

            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            // Assign lawyer if provided
            if (assignedLawyerId.HasValue && assignedLawyerId.Value > 0)
            {
                retainer.AssignedLawyerId = assignedLawyerId.Value;
            }

            // If no lawyer assigned, show error
            if (!retainer.AssignedLawyerId.HasValue || retainer.AssignedLawyerId.Value == 0)
            {
                return Json(new { success = false, message = "Please assign a lawyer before submitting for approval." });
            }

            retainer.Status = RetainerStatus.PendingApproval;
            retainer.SubmittedForApprovalDate = DateTime.Now;
            retainer.SubmittedByUserId = userId;

            await _context.SaveChangesAsync();
            await LogRetainerAction(retainer.Id, "SubmittedForApproval", $"Submitted for lawyer approval by {GetCurrentUserName()}", userId);
            await NotifyLawyerPendingApproval(retainer.Id);

            return Json(new { success = true, message = "Retainer submitted for lawyer approval successfully!" });
        }

        #endregion

        #region Lawyer Review & Approval Workflow

        // GET: Retainer/PendingApprovals - Lawyer views pending retainers

        // GET: Retainer/PendingApprovals - Separate views for Lawyer vs Admin
        [HttpGet]
        public async Task<IActionResult> PendingApprovals()
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            if (!userId.HasValue)
            {
                TempData["Error"] = "Please log in to access this page.";
                return RedirectToAction("Login", "Account");
            }

            IQueryable<Retainer> query = _context.Retainers
                .Include(r => r.Client)
                .Include(r => r.Case)
                .Include(r => r.Template)
                .Include(r => r.AssignedLawyer)
                .Where(r => r.Status == RetainerStatus.PendingApproval && !r.IsDeleted);

            // Separate logic for Lawyers vs Admins
            if (userRole == UserRole.Lawyer)
            {
                // Lawyers see retainers assigned to them OR unassigned retainers (so they can claim them)
                query = query.Where(r => r.AssignedLawyerId == userId || r.AssignedLawyerId == null);

                var pendingRetainers = await query
                    .OrderByDescending(r => r.SubmittedForApprovalDate)
                    .ToListAsync();

                // Return Lawyer-specific view
                return View("PendingApprovalsLawyer", pendingRetainers);
            }
            else if (userRole == UserRole.Admin)
            {
                // Admins see ALL pending retainers
                var pendingRetainers = await query
                    .OrderByDescending(r => r.SubmittedForApprovalDate)
                    .ToListAsync();

                // Return Admin-specific view
                return View("PendingApprovalsAdmin", pendingRetainers);
            }

            TempData["Error"] = "You do not have permission to view pending approvals.";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> AssignLawyer(int id, int lawyerId)
        {
            var retainer = await _context.Retainers.FindAsync(id);
            if (retainer == null)
            {
                return Json(new { success = false, message = "Retainer not found." });
            }

            retainer.AssignedLawyerId = lawyerId;
            await _context.SaveChangesAsync();

            await LogRetainerAction(id, "LawyerAssigned", $"Lawyer assigned by admin", GetCurrentUserId());

            return Json(new { success = true, message = "Lawyer assigned successfully." });
        }

        // GET: Retainer/Review/5 - Lawyer reviews a retainer
        [HttpGet]
        public async Task<IActionResult> Review(int id)
        {
            var retainer = await _context.Retainers
                .Include(r => r.Client)
                .Include(r => r.Case)
                .Include(r => r.Template)
                .Include(r => r.PaymentSchedules)
                .Include(r => r.ActionLogs)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (retainer == null)
            {
                TempData["Error"] = "Retainer not found.";
                return RedirectToAction("PendingApprovals");
            }

            // Check permissions - only assigned lawyer or admin can review
            var userRole = GetCurrentUserRole();
            var userId = GetCurrentUserId();

            if (userRole != UserRole.Admin && retainer.AssignedLawyerId != userId)
            {
                TempData["Error"] = "You are not authorized to review this retainer.";
                return RedirectToAction("PendingApprovals");
            }

            ViewBag.Cases = new SelectList(_context.Cases.Where(c => c.ClientId == retainer.ClientId && c.Status != CaseStatus.Closed), "Id", "Title", retainer.CaseId);
            
            var viewModel = new LawyerReviewViewModel
            {
                Retainer = retainer,
                ActionLogs = retainer.ActionLogs?.OrderByDescending(l => l.CreatedAt).ToList() ?? new List<RetainerActionLog>(),
                SuggestedChanges = string.Empty
            };

            return View(viewModel);
        }

        // POST: Retainer/Approve - Lawyer approves the retainer
        [HttpPost]
      
        public async Task<IActionResult> Approve(int id, string lawyerNotes, bool requiresUpfrontPayment = true, int paymentDueDays = 7)
        {
            var retainer = await _context.Retainers
                .Include(r => r.Client)
                .Include(r => r.Template)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (retainer == null)
            {
                TempData["Error"] = "Retainer not found.";
                return RedirectToAction("PendingApprovals");
            }

            // Check permissions
            var userRole = GetCurrentUserRole();
            var userId = GetCurrentUserId();

            if (userRole != UserRole.Admin && retainer.AssignedLawyerId != userId)
            {
                TempData["Error"] = "You are not authorized to approve this retainer.";
                return RedirectToAction("PendingApprovals");
            }

            var lawyerId = userId;
            var lawyer = await _context.Users.FindAsync(lawyerId);

            retainer.Status = RetainerStatus.Approved;
            retainer.ApprovedDate = DateTime.Now;
            retainer.ApprovedByUserId = lawyerId;
            retainer.LawyerNotes = lawyerNotes;
            retainer.RequiresUpfrontPayment = requiresUpfrontPayment;
            retainer.PaymentDueDays = paymentDueDays;

            // Generate signature token (valid for 30 days)
            retainer.SignatureToken = GenerateSignatureToken();
            retainer.SignatureTokenExpiry = DateTime.Now.AddDays(30);

            // Generate PDF document
            retainer.PdfPath = await GenerateRetainerPdf(retainer);

            await _context.SaveChangesAsync();
            await LogRetainerAction(retainer.Id, "Approved", $"Approved by Lawyer: {lawyer?.FullName}. Notes: {lawyerNotes}", lawyerId);

            // Generate billing request
            await GenerateBillingForRetainer(retainer.Id);

            // Notify client
            await NotifyClient(retainer.Id, "approved");

            TempData["Success"] = "Retainer approved successfully! The client has been notified and can now review, sign, and make payment.";
            return RedirectToAction("Details", new { id = retainer.Id });
        }

        

        // POST: Retainer/Reject - Lawyer rejects the retainer
        [HttpPost]
       
        public async Task<IActionResult> Reject(int id, string rejectionReason)
        {
            var retainer = await _context.Retainers.FindAsync(id);
            if (retainer == null)
            {
                TempData["Error"] = "Retainer not found.";
                return RedirectToAction("PendingApprovals");
            }

            // Check permissions
            var userRole = GetCurrentUserRole();
            var userId = GetCurrentUserId();

            if (userRole != UserRole.Admin && retainer.AssignedLawyerId != userId)
            {
                TempData["Error"] = "You are not authorized to reject this retainer.";
                return RedirectToAction("PendingApprovals");
            }

            retainer.Status = RetainerStatus.Rejected;
            retainer.RejectionReason = rejectionReason;
            retainer.LawyerNotes = rejectionReason;
            retainer.RejectedDate = DateTime.Now;
            retainer.RejectedByUserId = userId;

            await _context.SaveChangesAsync();
            await LogRetainerAction(retainer.Id, "Rejected", $"Rejected by lawyer: {rejectionReason}", userId);

            // Notify admin who submitted
            await NotifyAdminRejected(retainer.Id);

            TempData["Warning"] = "Retainer rejected. Please inform the admin/paralegal to make necessary changes.";
            return RedirectToAction("PendingApprovals");
        }

        #endregion

        #region Billing Generation

        // POST: Retainer/GenerateBilling - Generate invoice for approved retainer
        [HttpPost]
        public async Task<IActionResult> GenerateBilling(int id)
        {
            var retainer = await _context.Retainers
                .Include(r => r.Client)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (retainer == null)
            {
                return Json(new { success = false, message = "Retainer not found." });
            }

            if (retainer.Status != RetainerStatus.Approved && retainer.Status != RetainerStatus.AwaitingPayment)
            {
                return Json(new { success = false, message = "Billing can only be generated for approved retainers." });
            }

            var invoice = await GenerateInvoiceForRetainer(retainer);

            return Json(new { success = true, message = $"Invoice #{invoice.InvoiceNumber} generated successfully.", invoiceId = invoice.Id });
        }

        private async Task<Invoice> GenerateInvoiceForRetainer(Retainer retainer)
        {
            // Check if invoice already exists
            var existingInvoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.RetainerId == retainer.Id);

            if (existingInvoice != null)
            {
                return existingInvoice;
            }

            var dueDate = DateTime.Now.AddDays(retainer.PaymentDueDays > 0 ? retainer.PaymentDueDays : 7);

            var invoice = new Invoice
            {
                ClientId = retainer.ClientId,
                RetainerId = retainer.Id,
                CaseId = retainer.CaseId,
                Amount = retainer.Amount,
                TaxAmount = 0,
                TotalAmount = retainer.Amount,
                IssueDate = DateTime.Now,
                DueDate = dueDate,
                Status = InvoiceStatus.Sent,
                Description = $"Retainer fee for {retainer.Title}",
                InvoiceNumber = GenerateInvoiceNumber(),
                CreatedAt = DateTime.Now,
                Notes = $"Payment required to activate retainer. Due within {retainer.PaymentDueDays} days.",

                // ⭐ CRITICAL FIXES - Add these missing fields
                PdfPath = "",  // Empty string instead of NULL
                PaidDate = null,  // Nullable, so null is fine
                
            };

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            return invoice;
        }

        private async Task GenerateBillingForRetainer(int retainerId)
        {
            var retainer = await _context.Retainers
                .Include(r => r.Client)
                .FirstOrDefaultAsync(r => r.Id == retainerId);

            if (retainer != null && retainer.RequiresUpfrontPayment)
            {
                await GenerateInvoiceForRetainer(retainer);
                await LogRetainerAction(retainer.Id, "BillingGenerated", "Billing invoice generated for retainer approval", GetCurrentUserId());
            }
        }

        #region Lawyer Review & Approval Workflow

        // GET: Retainer/LawyerPendingApprovals - Lawyer views THEIR pending retainers
        [HttpGet]
        public async Task<IActionResult> LawyerPendingApprovals()
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            if (!userId.HasValue)
            {
                TempData["Error"] = "Please log in to access this page.";
                return RedirectToAction("Login", "Account");
            }

            if (userRole != UserRole.Lawyer)
            {
                TempData["Error"] = "Access denied. Lawyer portal only.";
                return RedirectToAction("Index", "Home");
            }

            // Lawyers see retainers assigned to them OR unassigned retainers
            var pendingRetainers = await _context.Retainers
                .Include(r => r.Client)
                .Include(r => r.Case)
                .Include(r => r.Template)
                .Include(r => r.AssignedLawyer)
                .Where(r => r.Status == RetainerStatus.PendingApproval
                            && !r.IsDeleted
                            && (r.AssignedLawyerId == userId || r.AssignedLawyerId == null))
                .OrderByDescending(r => r.SubmittedForApprovalDate)
                .ToListAsync();

            return View("PendingApprovalsLawyer", pendingRetainers);
        }

        // GET: Retainer/AdminPendingApprovals - Admin views ALL pending retainers
        [HttpGet]
        public async Task<IActionResult> AdminPendingApprovals()
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            if (!userId.HasValue)
            {
                TempData["Error"] = "Please log in to access this page.";
                return RedirectToAction("Login", "Account");
            }

            if (userRole != UserRole.Admin)
            {
                TempData["Error"] = "Access denied. Admin portal only.";
                return RedirectToAction("Index", "Home");
            }

            // Admins see ALL pending retainers
            var pendingRetainers = await _context.Retainers
                .Include(r => r.Client)
                .Include(r => r.Case)
                .Include(r => r.Template)
                .Include(r => r.AssignedLawyer)
                .Where(r => r.Status == RetainerStatus.PendingApproval && !r.IsDeleted)
                .OrderByDescending(r => r.SubmittedForApprovalDate)
                .ToListAsync();

            // Get lawyers for the dropdown in admin view
            ViewBag.Lawyers = await _context.Users
                .Where(u => u.Role == UserRole.Lawyer && u.IsActive)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            return View("PendingApprovalsAdmin", pendingRetainers);
        }

        // GET: Retainer/Api/AdminPendingCounts - Get pending counts for admin sidebar badges
        [HttpGet]
        public async Task<IActionResult> GetAdminPendingCounts()
        {
            if (GetCurrentUserRole() != UserRole.Admin)
            {
                return Json(new { pendingApprovals = 0, pendingRequests = 0 });
            }

            var pendingApprovals = await _context.Retainers
                .Where(r => r.Status == RetainerStatus.PendingApproval && !r.IsDeleted)
                .CountAsync();
            var pendingRequests = await _context.ClientRequests
                .Where(x => x.Status == "Pending")
                .CountAsync();

            return Json(new { pendingApprovals, pendingRequests });
        }

        // GET: Retainer/Api/PendingCount - Get pending count for lawyer badge
        [HttpGet]
        public async Task<IActionResult> GetLawyerPendingCount()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Json(new { pendingCount = 0 });
            }

            var pendingCount = await _context.Retainers
                .Where(r => r.Status == RetainerStatus.PendingApproval
                            && !r.IsDeleted
                            && (r.AssignedLawyerId == userId || r.AssignedLawyerId == null))
                .CountAsync();

            return Json(new { pendingCount = pendingCount });
        }

        // POST: Retainer/AssignLawyer - Admin assigns lawyer to retainer
       

        // GET: Retainer/ClaimRetainer - Lawyer claims an unassigned retainer
        [HttpPost]
        public async Task<IActionResult> ClaimRetainer(int id)
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            if (!userId.HasValue || userRole != UserRole.Lawyer)
            {
                return Json(new { success = false, message = "Unauthorized." });
            }

            var retainer = await _context.Retainers.FindAsync(id);
            if (retainer == null)
            {
                return Json(new { success = false, message = "Retainer not found." });
            }

            if (retainer.AssignedLawyerId != null)
            {
                return Json(new { success = false, message = "This retainer is already assigned to another lawyer." });
            }

            retainer.AssignedLawyerId = userId;
            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(userId);
            await LogRetainerAction(id, "Claimed", $"Retainer claimed by lawyer {user?.FullName}", userId);

            return Json(new { success = true, message = "Retainer claimed successfully. You can now review it." });
        }

        #endregion

        #endregion

        #region Client Interaction (Signing & Payment)

        // GET: Retainer/Sign/{token} - Client views and signs retainer
        public async Task<IActionResult> Sign(string token)
        {
            var retainer = await _context.Retainers
                .Include(r => r.Client)
                .Include(r => r.Template)
                .Include(r => r.PaymentSchedules)
                .FirstOrDefaultAsync(r => r.SignatureToken == token &&
                    (r.Status == RetainerStatus.Approved || r.Status == RetainerStatus.AwaitingPayment) &&
                    r.SignatureTokenExpiry > DateTime.Now);

            if (retainer == null)
            {
                TempData["Error"] = "Invalid or expired signature link. Please contact the law firm for assistance.";
                return RedirectToAction("Index", "Home");
            }

            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.RetainerId == retainer.Id && i.Status != InvoiceStatus.Paid);

            var viewModel = new RetainerSignViewModel
            {
                Retainer = retainer,
                RequiresPayment = retainer.RequiresUpfrontPayment,
                PaymentAmount = retainer.Amount,
                PaymentSchedules = retainer.PaymentSchedules?.ToList() ?? new List<RetainerPaymentSchedule>(),
                Invoice = invoice,
                HasOutstandingInvoice = invoice != null && invoice.Status != InvoiceStatus.Paid
            };

            return View(viewModel);
        }

        // POST: Retainer/Sign - Client signs the retainer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sign(int id, string signatureName, bool acceptTerms, string selectedPaymentMethod = null)
        {
            var retainer = await _context.Retainers
                .Include(r => r.Client)
                .Include(r => r.PaymentSchedules)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (retainer == null)
            {
                TempData["Error"] = "Retainer not found.";
                return RedirectToAction("Index", "Home");
            }

            if (!acceptTerms)
            {
                TempData["Error"] = "You must accept the terms and conditions to proceed.";
                return RedirectToAction("Sign", new { token = retainer.SignatureToken });
            }

            if (string.IsNullOrWhiteSpace(signatureName))
            {
                TempData["Error"] = "Please enter your full name as your electronic signature.";
                return RedirectToAction("Sign", new { token = retainer.SignatureToken });
            }

            // Record the signature
            retainer.ClientSignatureName = signatureName;
            retainer.ClientIPAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            retainer.SignedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            await LogRetainerAction(retainer.Id, "Signed", $"Signed by client: {signatureName} from IP {retainer.ClientIPAddress}", null);

            // Check if payment is required
            if (retainer.RequiresUpfrontPayment)
            {
                retainer.Status = RetainerStatus.AwaitingPayment;
                await _context.SaveChangesAsync();

                TempData["Success"] = "Retainer signed successfully! Please complete your payment to activate the service.";
                return RedirectToAction("MakePayment", new { id = retainer.Id, token = retainer.SignatureToken });
            }
            else
            {
                // No payment required, activate immediately
                retainer.Status = RetainerStatus.Active;
                retainer.ActivatedDate = DateTime.Now;
                await _context.SaveChangesAsync();

                // Activate associated case if exists
                if (retainer.CaseId.HasValue)
                {
                    var caseItem = await _context.Cases.FindAsync(retainer.CaseId.Value);
                    if (caseItem != null && caseItem.Status == CaseStatus.Draft)
                    {
                        caseItem.Status = CaseStatus.Active;
                        await _context.SaveChangesAsync();
                    }
                }

                await LogRetainerAction(retainer.Id, "Activated", "Retainer activated (no payment required)", null);
                TempData["Success"] = "Retainer signed and activated successfully! Your legal service is now active.";
            }

            return RedirectToAction("MyRetainers");
        }

        // POST: Retainer/CancelRequest - Client cancels a pending retainer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelRequest(int id, string cancellationReason)
        {
            var retainer = await _context.Retainers
                .Include(r => r.Client)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (retainer == null)
            {
                TempData["Error"] = "Retainer not found.";
                return RedirectToAction("MyRetainers");
            }

            // Only allow cancellation for pending or draft retainers
            if (retainer.Status != RetainerStatus.PendingApproval && retainer.Status != RetainerStatus.Draft)
            {
                TempData["Error"] = "This retainer cannot be cancelled at this stage.";
                return RedirectToAction("MyRetainers");
            }

            retainer.Status = RetainerStatus.Cancelled;
            retainer.CancelledDate = DateTime.Now;
            retainer.CancellationReason = cancellationReason;

            await _context.SaveChangesAsync();
            await LogRetainerAction(retainer.Id, "Cancelled", $"Cancelled by client: {cancellationReason}", null);

            TempData["Info"] = "Your retainer request has been cancelled.";
            return RedirectToAction("MyRetainers");
        }

        // POST: Retainer/ClientRequestChanges - Client requests changes to retainer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClientRequestChanges(int id, string changeRequest)
        {
            var retainer = await _context.Retainers
                .Include(r => r.Client)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (retainer == null)
            {
                TempData["Error"] = "Retainer not found.";
                return RedirectToAction("MyRetainers");
            }

            if (retainer.Status != RetainerStatus.PendingApproval)
            {
                TempData["Error"] = "Changes can only be requested while the retainer is pending approval.";
                return RedirectToAction("MyRetainers");
            }

            retainer.Status = RetainerStatus.Draft;
            retainer.ClientChangeRequest = changeRequest;
            retainer.ChangeRequestedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            await LogRetainerAction(retainer.Id, "ChangesRequestedByClient", $"Client requested changes: {changeRequest}", null);

            // Notify admin/lawyer
            await NotifyAdminChangesRequested(retainer.Id);

            TempData["Success"] = "Your change request has been submitted. The law firm will review and update the retainer.";
            return RedirectToAction("MyRetainers");
        }
        // POST: Retainer/RequestChanges - Lawyer requests changes to the retainer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestChanges(int id, string requestedChanges)
        {
            var retainer = await _context.Retainers.FindAsync(id);
            if (retainer == null)
            {
                TempData["Error"] = "Retainer not found.";
                return RedirectToAction("PendingApprovals");
            }

            // Check permissions
            var userRole = GetCurrentUserRole();
            var userId = GetCurrentUserId();

            if (userRole != UserRole.Admin && retainer.AssignedLawyerId != userId)
            {
                TempData["Error"] = "You are not authorized to request changes for this retainer.";
                return RedirectToAction("PendingApprovals");
            }

            retainer.Status = RetainerStatus.Draft; // Back to draft for editing
            retainer.LawyerNotes = requestedChanges;
            retainer.RevisionRequestedDate = DateTime.Now;
            retainer.RevisionRequestedByUserId = userId;

            await _context.SaveChangesAsync();
            await LogRetainerAction(retainer.Id, "ChangesRequested", $"Lawyer requested changes: {requestedChanges}", userId);

            // Notify admin who submitted
            await NotifyAdminChangesRequested(retainer.Id);

            TempData["Warning"] = "Changes requested. The retainer has been sent back for editing.";
            return RedirectToAction("PendingApprovals");
        }

        // GET: Retainer/MakePayment - Client makes payment
        public async Task<IActionResult> MakePayment(int id, string token)
        {
            var retainer = await _context.Retainers
                .Include(r => r.Client)
                .Include(r => r.Template)
                .Include(r => r.PaymentSchedules)
                .FirstOrDefaultAsync(r => r.Id == id && r.SignatureToken == token);

            if (retainer == null)
            {
                TempData["Error"] = "Invalid payment link.";
                return RedirectToAction("Index", "Home");
            }

            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.RetainerId == retainer.Id && i.Status != InvoiceStatus.Paid);

            var payments = await _context.Payments
                .Where(p => p.InvoiceId == invoice.Id)
                .ToListAsync();

            var paidAmount = payments.Sum(p => p.Amount);
            var remainingBalance = (invoice?.TotalAmount ?? retainer.Amount) - paidAmount;

            var viewModel = new RetainerPaymentViewModel
            {
                RetainerId = retainer.Id,
                RetainerTitle = retainer.Title,
                TotalAmount = invoice?.TotalAmount ?? retainer.Amount,
                AmountPaid = paidAmount,
                RemainingBalance = remainingBalance,
                SignatureToken = token,
                Invoice = invoice,
                PaymentSchedules = retainer.PaymentSchedules?.Where(p => p.Status == PaymentScheduleStatus.Pending).ToList() ?? new List<RetainerPaymentSchedule>()
            };

            return View(viewModel);
        }

        // POST: Retainer/ProcessPayment - Process client payment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(int retainerId, decimal amount, string paymentMethod, string transactionReference, int? scheduleId = null)
        {
            var retainer = await _context.Retainers
                .Include(r => r.Client)
                .FirstOrDefaultAsync(r => r.Id == retainerId);

            if (retainer == null)
            {
                TempData["Error"] = "Retainer not found.";
                return RedirectToAction("Index", "Home");
            }

            if (amount <= 0)
            {
                TempData["Error"] = "Please enter a valid payment amount.";
                return RedirectToAction("MakePayment", new { id = retainer.Id, token = retainer.SignatureToken });
            }

            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.RetainerId == retainer.Id);

            if (invoice == null)
            {
                TempData["Error"] = "No invoice found for this retainer.";
                return RedirectToAction("MakePayment", new { id = retainer.Id, token = retainer.SignatureToken });
            }

            // Record the payment
            var payment = new Payment
            {
                InvoiceId = invoice.Id,
                ClientId = retainer.ClientId,
                Amount = amount,
                PaymentDate = DateTime.Now,
                PaymentMethod = Enum.Parse<PaymentMethod>(paymentMethod),
                TransactionReference = transactionReference ?? GeneratePaymentReference(),
                Notes = $"Payment for retainer #{retainer.Id}",
                CreatedAt = DateTime.Now
            };

            _context.Payments.Add(payment);
            _context.RetainerPayments.Add(new RetainerPayment
            {
                RetainerId = retainer.Id, PaymentScheduleId = scheduleId, Amount = amount,
                PaymentDate = DateTime.UtcNow, PaymentMethod = payment.PaymentMethod,
                TransactionReference = payment.TransactionReference, Notes = "Retainer funding", IsDepositedToTrust = true
            });
            retainer.AvailableBalance += amount;

            // Update invoice paid amount
            var totalPaid = (invoice.Payments?.Sum(p => p.Amount) ?? 0) + amount;
            
            if (totalPaid >= invoice.TotalAmount)
            {
                invoice.Status = InvoiceStatus.Paid;
                invoice.PaidDate = DateTime.Now;
                
                // Activate retainer
                retainer.Status = RetainerStatus.Active;
                retainer.ActivatedDate = DateTime.Now;
                retainer.AmountPaid = totalPaid;
                retainer.PaymentConfirmedDate = DateTime.Now;

                // Activate associated case
                if (retainer.CaseId.HasValue)
                {
                    var caseItem = await _context.Cases.FindAsync(retainer.CaseId.Value);
                    if (caseItem != null && caseItem.Status == CaseStatus.Draft)
                    {
                        caseItem.Status = CaseStatus.Active;
                        _context.Update(caseItem);
                    }
                }

                await LogRetainerAction(retainer.Id, "Activated", $"Retainer activated after full payment of R {amount:N2}", null);
                TempData["Success"] = $"Payment of R {amount:N2} received successfully! Your retainer is now active.";
            }
            else
            {
                invoice.Status = InvoiceStatus.PartiallyPaid;
                retainer.AmountPaid = totalPaid;
                await LogRetainerAction(retainer.Id, "PartialPayment", $"Partial payment of R {amount:N2} received. Remaining: R {(invoice.TotalAmount - totalPaid):N2}", null);
                TempData["Success"] = $"Payment of R {amount:N2} received successfully! Remaining balance: R {(invoice.TotalAmount - totalPaid):N2}";
            }

            await _context.SaveChangesAsync();

            // Create trust account deposit record
            await CreateTrustDeposit(retainer.ClientId, amount, payment.TransactionReference);

            return RedirectToAction("MyRetainers");
        }

        #endregion

        #region Client Dashboard

        // GET: Retainer/MyRetainers - Client views their retainers
        public async Task<IActionResult> MyRetainers()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                TempData["Error"] = "Please log in to view your retainers.";
                return RedirectToAction("Index", "Home");
            }

            var user = await _context.Users.FindAsync(userId.Value);
            var client = await _context.Clients.FirstOrDefaultAsync(c => c.Email == user.Email);

            if (client == null)
            {
                TempData["Error"] = "Please complete your client profile first.";
                return RedirectToAction("Edit", "Client");
            }

            var retainers = await _context.Retainers
                .Include(r => r.Case)
                .Include(r => r.Template)
                .Include(r => r.Payments)
                .Where(r => r.ClientId == client.Id && !r.IsDeleted)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();

            // Calculate status counts for dashboard
            ViewBag.ActiveCount = retainers.Count(r => r.Status == RetainerStatus.Active);
            ViewBag.PendingCount = retainers.Count(r => r.Status == RetainerStatus.PendingApproval);
            ViewBag.AwaitingPaymentCount = retainers.Count(r => r.Status == RetainerStatus.AwaitingPayment);
            ViewBag.CompletedCount = retainers.Count(r => r.Status == RetainerStatus.Completed);

            return View(retainers);
        }

        public async Task<IActionResult> PendingRequests()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role is not ("Director" or "Admin" or "Lawyer")) return Forbid();
            var requests = await _context.ClientRequests.Include(x => x.Client).Include(x => x.Template)
                .Where(x => x.Status == "Pending").OrderByDescending(x => x.CreatedDate).ToListAsync();
            return View(requests);
        }

        public IActionResult MyRequests() => RedirectToAction(nameof(MyRetainers));

        public IActionResult PaymentHistory() => RedirectToAction("MyInvoices", "Billing");

        // GET: Retainer/ClientRetainerDetails/5
        public async Task<IActionResult> ClientRetainerDetails(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await _context.Users.FindAsync(userId.Value);
            var client = await _context.Clients.FirstOrDefaultAsync(c => c.Email == user.Email);

            if (client == null)
            {
                return RedirectToAction("Edit", "Client");
            }

            var retainer = await _context.Retainers
                .Include(r => r.Client)
                .Include(r => r.Case)
                .Include(r => r.Template)
                .Include(r => r.PaymentSchedules)
                .Include(r => r.ActionLogs)
                .FirstOrDefaultAsync(r => r.Id == id && r.ClientId == client.Id);

            if (retainer == null)
            {
                TempData["Error"] = "Retainer not found.";
                return RedirectToAction("MyRetainers");
            }

            // ⭐ FIX: Check if invoice exists before using it
            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.RetainerId == retainer.Id);

            // ⭐ FIX: Only get payments if invoice exists
            List<Payment> payments = new List<Payment>();
            if (invoice != null)
            {
                payments = await _context.Payments
                    .Where(p => p.InvoiceId == invoice.Id)
                    .ToListAsync();
            }

            var totalPaid = payments.Sum(p => p.Amount);
            var remainingBalance = (invoice?.TotalAmount ?? retainer.Amount) - totalPaid;

            var viewModel = new ClientRetainerDetailsViewModel
            {
                Retainer = retainer,
                Invoice = invoice,
                Payments = payments,
                PaymentSchedules = retainer.PaymentSchedules?.ToList() ?? new List<RetainerPaymentSchedule>(),
                TotalPaid = totalPaid,
                RemainingBalance = remainingBalance,
                ActionLogs = retainer.ActionLogs?.OrderByDescending(l => l.CreatedAt).ToList() ?? new List<RetainerActionLog>()
            };

            return View(viewModel);
        }

        // GET: Retainer/DownloadRetainerPdf/5
        public async Task<IActionResult> DownloadRetainerPdf(int id)
        {
            var retainer = await _context.Retainers.FindAsync(id);
            if (retainer == null || string.IsNullOrEmpty(retainer.PdfPath))
            {
                TempData["Error"] = "PDF document not found.";
                return RedirectToAction("MyRetainers");
            }

            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, retainer.PdfPath.TrimStart('/'));
            if (!System.IO.File.Exists(filePath))
            {
                TempData["Error"] = "PDF file not found on server.";
                return RedirectToAction("MyRetainers");
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            var fileName = $"Retainer_{retainer.Id}_{retainer.Title.Replace(" ", "_")}.pdf";

            return File(fileBytes, "application/pdf", fileName);
        }

        #endregion

        #region Ongoing Management (Admin/Lawyer)

        // GET: Retainer/Details/5 - Full retainer details with audit trail
        public async Task<IActionResult> Details(int id)
        {
            var retainer = await _context.Retainers
                .Include(r => r.Client)
                .Include(r => r.Case)
                .Include(r => r.Template)
                .Include(r => r.ApprovedByUser)
                .Include(r => r.SubmittedByUser)
                .Include(r => r.Payments)
                .Include(r => r.PaymentSchedules)
                .Include(r => r.ActionLogs)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (retainer == null)
            {
                TempData["Error"] = "Retainer not found.";
                return RedirectToAction("Index");
            }

            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.RetainerId == retainer.Id);

            // ⭐ FIX: Check if invoice exists before using it
            List<Payment> payments = new List<Payment>();
            if (invoice != null)
            {
                payments = await _context.Payments
                    .Where(p => p.InvoiceId == invoice.Id)
                    .ToListAsync();
            }

            var viewModel = new RetainerDetailsViewModel
            {
                Retainer = retainer,
                Invoice = invoice,  // This can be null for draft retainers
                Payments = payments,
                PaymentSchedules = retainer.PaymentSchedules?.ToList() ?? new List<RetainerPaymentSchedule>(),
                TotalPaid = payments.Sum(p => p.Amount),
                CanEdit = retainer.Status == RetainerStatus.Draft || retainer.Status == RetainerStatus.Rejected,
                CanSubmit = retainer.Status == RetainerStatus.Draft || retainer.Status == RetainerStatus.Rejected,
                CanApprove = retainer.Status == RetainerStatus.PendingApproval,
                ActionLogs = retainer.ActionLogs?.OrderByDescending(l => l.CreatedAt).ToList() ?? new List<RetainerActionLog>()
            };

            return View(viewModel);
        }

        // GET: Retainer/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var retainer = await _context.Retainers.FindAsync(id);
            if (retainer == null)
            {
                TempData["Error"] = "Retainer not found.";
                return RedirectToAction("Index");
            }

            if (retainer.Status != RetainerStatus.Draft && retainer.Status != RetainerStatus.Rejected)
            {
                TempData["Error"] = "Only draft or rejected retainers can be edited.";
                return RedirectToAction("Details", new { id = retainer.Id });
            }

            var clients = await _context.Clients.Where(c => c.IsActive).ToListAsync();
            ViewBag.Clients = new SelectList(clients, "Id", "FullName", retainer.ClientId);
            ViewBag.Cases = new SelectList(_context.Cases.Where(c => c.ClientId == retainer.ClientId && c.Status != CaseStatus.Closed), "Id", "Title", retainer.CaseId);
            ViewBag.Templates = new SelectList(_context.RetainerTemplates.Where(t => t.IsActive), "Id", "Name", retainer.TemplateId);
            ViewBag.RetainerTypes = new SelectList(Enum.GetValues(typeof(RetainerType)), retainer.Type);
            ViewBag.Lawyers = new SelectList(_context.Users.Where(u => u.Role == UserRole.Lawyer), "Id", "FullName", retainer.AssignedLawyerId);

            return View(retainer);
        }

        // POST: Retainer/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Retainer retainer)
        {
            if (id != retainer.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.Retainers.FindAsync(id);
                    if (existing == null)
                    {
                        return NotFound();
                    }

                    existing.Title = retainer.Title;
                    existing.ScopeOfWork = retainer.ScopeOfWork;
                    existing.SpecialTerms = retainer.SpecialTerms;
                    existing.Type = retainer.Type;
                    existing.Amount = retainer.Amount;
                    existing.IncludedHours = retainer.IncludedHours;
                    existing.OverageRate = retainer.OverageRate;
                    existing.BillingCycle = retainer.BillingCycle;
                    existing.StartDate = retainer.StartDate;
                    existing.EndDate = retainer.EndDate;
                    existing.AdminNotes = retainer.AdminNotes;
                    existing.AssignedLawyerId = retainer.AssignedLawyerId;
                    existing.RequiresUpfrontPayment = retainer.RequiresUpfrontPayment;
                    existing.PaymentDueDays = retainer.PaymentDueDays;

                    if (existing.Status == RetainerStatus.Rejected)
                    {
                        existing.Status = RetainerStatus.Draft;
                        existing.RejectionReason = null;
                    }

                    await _context.SaveChangesAsync();
                    await LogRetainerAction(existing.Id, "Edited", $"Retainer edited by {GetCurrentUserName()}", GetCurrentUserId());
                    
                    TempData["Success"] = "Retainer updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Retainers.Any(e => e.Id == id))
                    {
                        return NotFound();
                    }
                    throw;
                }

                return RedirectToAction("Details", new { id = retainer.Id });
            }

            return View(retainer);
        }

        // POST: Retainer/Renew - Renew an active or expired retainer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Renew(int id, DateTime newEndDate, decimal? newAmount = null)
        {
            var retainer = await _context.Retainers
                .Include(r => r.Client)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (retainer == null)
            {
                return Json(new { success = false, message = "Retainer not found." });
            }

            if (retainer.Status != RetainerStatus.Active && retainer.Status != RetainerStatus.Completed)
            {
                return Json(new { success = false, message = "Only active or completed retainers can be renewed." });
            }

            var currentUserId = GetCurrentUserId();

            // Create a renewal record
            var renewal = new RetainerRenewal
            {
                RetainerId = retainer.Id,
                PreviousEndDate = retainer.EndDate,
                NewEndDate = newEndDate,
                RenewedDate = DateTime.Now,
                RenewedByUserId = currentUserId.HasValue ? currentUserId.Value : 0,
                AmountAdjustment = newAmount.HasValue ? newAmount.Value - retainer.Amount : 0,
                Notes = $"Renewed until {newEndDate:MM/dd/yyyy}"
            };

            _context.RetainerRenewals.Add(renewal);

            // Update retainer
            retainer.EndDate = newEndDate;
            if (newAmount.HasValue)
            {
                retainer.Amount = newAmount.Value;

                // Generate new invoice for renewal amount if needed
                if (newAmount.Value > 0)
                {
                    var renewalInvoice = new Invoice
                    {
                        ClientId = retainer.ClientId,
                        RetainerId = retainer.Id,
                        Amount = newAmount.Value,
                        TotalAmount = newAmount.Value,
                        IssueDate = DateTime.Now,
                        DueDate = DateTime.Now.AddDays(retainer.PaymentDueDays > 0 ? retainer.PaymentDueDays : 7),
                        Status = InvoiceStatus.Sent,
                        Description = $"Renewal fee for {retainer.Title} - Extended to {newEndDate:MM/dd/yyyy}",
                        InvoiceNumber = GenerateInvoiceNumber(),
                        CreatedAt = DateTime.Now
                    };
                    _context.Invoices.Add(renewalInvoice);
                }
            }

            retainer.Status = RetainerStatus.Active;
            retainer.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            await LogRetainerAction(retainer.Id, "Renewed", $"Retainer renewed until {newEndDate:MM/dd/yyyy} by {GetCurrentUserName()}", currentUserId);

            return Json(new { success = true, message = $"Retainer renewed successfully until {newEndDate:MM/dd/yyyy}." });
        }

        // POST: Retainer/Cancel - Admin/Lawyer cancels a retainer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string cancellationReason, bool processRefund = false)
        {
            var retainer = await _context.Retainers
                .Include(r => r.Client)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (retainer == null)
            {
                TempData["Error"] = "Retainer not found.";
                return RedirectToAction("Index");
            }

            if (retainer.Status == RetainerStatus.Active || retainer.Status == RetainerStatus.PendingApproval)
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                retainer.Status = RetainerStatus.Cancelled;
                retainer.CancelledDate = DateTime.Now;
                retainer.CancellationReason = cancellationReason;
                retainer.CancelledByUserId = GetCurrentUserId();

                await _context.SaveChangesAsync();
                await LogRetainerAction(retainer.Id, "Cancelled", $"Retainer cancelled by {GetCurrentUserName()}. Reason: {cancellationReason}", GetCurrentUserId());

                // Process refund if requested
                if (processRefund && retainer.AmountPaid > 0)
                {
                    await ProcessRefund(retainer.Id, retainer.AmountPaid.Value);
                }

                await transaction.CommitAsync();

                TempData["Success"] = "Retainer has been cancelled.";
            }
            else
            {
                TempData["Error"] = "This retainer cannot be cancelled at its current status.";
            }

            return RedirectToAction("Details", new { id });
        }

        // POST: Retainer/Delete/5 - Soft delete
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var retainer = await _context.Retainers.FindAsync(id);
            if (retainer == null)
            {
                return Json(new { success = false, message = "Retainer not found" });
            }

            if (retainer.Status != RetainerStatus.Draft && retainer.Status != RetainerStatus.Rejected && retainer.Status != RetainerStatus.Cancelled)
            {
                return Json(new { success = false, message = "Only draft, rejected, or cancelled retainers can be deleted" });
            }

            retainer.IsDeleted = true;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Retainer deleted successfully" });
        }

        // GET: Retainer/Dashboard - Admin dashboard
        public async Task<IActionResult> Dashboard()
        {
            var dashboard = new RetainerDashboardViewModel
            {
                PendingApprovalCount = await _context.Retainers.CountAsync(r => r.Status == RetainerStatus.PendingApproval && !r.IsDeleted),
                ActiveRetainersCount = await _context.Retainers.CountAsync(r => r.Status == RetainerStatus.Active && !r.IsDeleted),
                AwaitingPaymentCount = await _context.Retainers.CountAsync(r => r.Status == RetainerStatus.AwaitingPayment && !r.IsDeleted),
                DraftCount = await _context.Retainers.CountAsync(r => r.Status == RetainerStatus.Draft && !r.IsDeleted),
                RecentRetainers = await _context.Retainers
                    .Include(r => r.Client)
                    .Include(r => r.Template)
                    .Where(r => !r.IsDeleted)
                    .OrderByDescending(r => r.CreatedDate)
                    .Take(10)
                    .ToListAsync(),
                MonthlyRevenue = await GetMonthlyRevenue(),
                OverduePayments = await GetOverduePayments()
            };

            return View(dashboard);
        }

        // GET: Retainer/AuditTrail/5 - View complete audit trail
        public async Task<IActionResult> AuditTrail(int id)
        {
            var retainer = await _context.Retainers
                .Include(r => r.Client)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (retainer == null)
            {
                TempData["Error"] = "Retainer not found.";
                return RedirectToAction("Index");
            }

            var logs = await _context.RetainerActionLogs
                .Include(l => l.User)
                .Where(l => l.RetainerId == id)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            ViewBag.Retainer = retainer;
            return View(logs);
        }

        #endregion

        #region Trust Account Management

        // GET: Retainer/MyTrustAccount - Client views their trust account
        public async Task<IActionResult> MyTrustAccount()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await _context.Users.FindAsync(userId.Value);
            var client = await _context.Clients.FirstOrDefaultAsync(c => c.Email == user.Email);

            if (client == null)
            {
                return RedirectToAction("Edit", "Client");
            }

            var trustAccount = await _context.TrustAccounts
                .Include(t => t.Transactions)
                .FirstOrDefaultAsync(t => t.ClientId == client.Id);

            if (trustAccount == null)
            {
                trustAccount = new TrustAccount
                {
                    ClientId = client.Id,
                    Balance = 0,
                    TotalDeposited = 0,
                    TotalWithdrawn = 0,
                    LastUpdated = DateTime.Now
                };
                _context.TrustAccounts.Add(trustAccount);
                await _context.SaveChangesAsync();
            }

            var recentTransactions = trustAccount.Transactions?
                .OrderByDescending(t => t.TransactionDate)
                .Take(10)
                .ToList() ?? new List<TrustTransaction>();

            var viewModel = new ClientTrustViewModel
            {
                TrustAccount = trustAccount,
                RecentTransactions = recentTransactions,
                Client = client
            };

            return View(viewModel);
        }

        // GET: Retainer/DepositToTrust
        public async Task<IActionResult> DepositToTrust()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await _context.Users.FindAsync(userId.Value);
            var client = await _context.Clients.FirstOrDefaultAsync(c => c.Email == user.Email);

            if (client == null)
            {
                return RedirectToAction("Edit", "Client");
            }

            var model = new TrustDepositViewModel
            {
                ClientId = client.Id,
                ClientName = client.FullName
            };

            return View(model);
        }

        // POST: Retainer/DepositToTrust
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DepositToTrust(TrustDepositViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userEmail = userId.HasValue ? await _context.Users.Where(x => x.Id == userId.Value).Select(x => x.Email).SingleOrDefaultAsync() : null;
            var client = userEmail is null ? null : await _context.Clients.SingleOrDefaultAsync(x => x.Email == userEmail);
            if (client is null) return Forbid();
            model.ClientId = client.Id;
            model.ClientName = client.FullName;
            if (!ModelState.IsValid || model.Amount <= 0)
            {
                TempData["Error"] = "Please enter a valid deposit amount.";
                return View(model);
            }

            var trustAccount = await _context.TrustAccounts
                .FirstOrDefaultAsync(t => t.ClientId == model.ClientId);

            if (trustAccount == null)
            {
                trustAccount = new TrustAccount
                {
                    ClientId = model.ClientId,
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
                Amount = model.Amount,
                Description = model.Description ?? "Client deposit to trust account",
                Reference = GeneratePaymentReference(),
                TransactionDate = DateTime.Now
            };

            trustAccount.Balance += model.Amount;
            trustAccount.TotalDeposited += model.Amount;
            trustAccount.LastUpdated = DateTime.Now;

            _context.TrustTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"R {model.Amount:N2} has been deposited to your trust account successfully!";
            return RedirectToAction("MyTrustAccount");
        }

        [HttpGet]
        public async Task<IActionResult> DepositToRetainer(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var email = userId.HasValue ? await _context.Users.Where(x => x.Id == userId.Value).Select(x => x.Email).SingleOrDefaultAsync() : null;
            var retainer = await _context.Retainers.Include(x => x.Client).SingleOrDefaultAsync(x => x.Id == id && x.Client!.Email == email && x.Status == RetainerStatus.Active && !x.IsDeleted);
            if (retainer is null) return NotFound();
            return View(new RetainerDepositViewModel { RetainerId = retainer.Id, RetainerTitle = retainer.Title, CurrentBalance = retainer.AvailableBalance });
        }

        [HttpPost]
        public async Task<IActionResult> DepositToRetainer(RetainerDepositViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var email = userId.HasValue ? await _context.Users.Where(x => x.Id == userId.Value).Select(x => x.Email).SingleOrDefaultAsync() : null;
            var retainer = await _context.Retainers.Include(x => x.Client).SingleOrDefaultAsync(x => x.Id == model.RetainerId && x.Client!.Email == email && x.Status == RetainerStatus.Active && !x.IsDeleted);
            if (retainer is null) return NotFound();
            model.RetainerTitle = retainer.Title;
            model.CurrentBalance = retainer.AvailableBalance;
            if (!ModelState.IsValid || model.Amount <= 0) return View(model);
            if (await _context.RetainerPayments.AnyAsync(x => x.TransactionReference == model.TransactionReference))
            {
                ModelState.AddModelError(nameof(model.TransactionReference), "This payment reference has already been used.");
                return View(model);
            }
            await using var transaction = await _context.Database.BeginTransactionAsync();
            var trust = await _context.TrustAccounts.SingleOrDefaultAsync(x => x.ClientId == retainer.ClientId);
            if (trust is null)
            {
                trust = new TrustAccount { ClientId = retainer.ClientId, LastUpdated = DateTime.UtcNow, Transactions = [] };
                _context.TrustAccounts.Add(trust);
                await _context.SaveChangesAsync();
            }
            retainer.AvailableBalance += model.Amount;
            retainer.AmountPaid = (retainer.AmountPaid ?? 0) + model.Amount;
            trust.Balance += model.Amount;
            trust.TotalDeposited += model.Amount;
            trust.LastUpdated = DateTime.UtcNow;
            _context.RetainerPayments.Add(new RetainerPayment { RetainerId = retainer.Id, Amount = model.Amount, PaymentDate = DateTime.UtcNow, PaymentMethod = model.PaymentMethod, TransactionReference = model.TransactionReference.Trim(), Notes = "Client retainer top-up", IsDepositedToTrust = true });
            _context.TrustTransactions.Add(new TrustTransaction { TrustAccountId = trust.Id, Type = TransactionType.Deposit, Amount = model.Amount, Description = $"Top-up for retainer #{retainer.Id}: {retainer.Title}", Reference = model.TransactionReference.Trim(), TransactionDate = DateTime.UtcNow, AuthorizedBy = retainer.Client!.FullName });
            _context.AuditEntries.Add(new AuditEntry { ActorUserId = userId, EntityType = "Retainer", EntityId = retainer.Id.ToString(), Action = "Client retainer deposit", SafeMetadataJson = System.Text.Json.JsonSerializer.Serialize(new { amount = model.Amount, reference = model.TransactionReference.Trim(), method = model.PaymentMethod.ToString() }) });
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            await _notifications.QueueAsync(userId!.Value, "RetainerDeposit", "Retainer funded", $"R{model.Amount:N2} was deposited. Available balance: R{retainer.AvailableBalance:N2}.", $"/Retainer/ClientRetainerDetails/{retainer.Id}", $"retainer-deposit:{retainer.Id}:{model.TransactionReference.Trim()}");
            TempData["Success"] = $"R {model.Amount:N2} was deposited to your active retainer.";
            return RedirectToAction(nameof(ClientRetainerDetails), new { id = retainer.Id });
        }

        // GET: Retainer/AdminTrustAccounts - Admin view all trust accounts
        public async Task<IActionResult> AdminTrustAccounts()
        {
            var trustAccounts = await _context.TrustAccounts
                .Include(t => t.Client)
                .OrderByDescending(t => t.Balance)
                .ToListAsync();

            return View(trustAccounts);
        }

        // GET: Retainer/AdminTrustDetails/5
        public async Task<IActionResult> AdminTrustDetails(int id)
        {
            var trustAccount = await _context.TrustAccounts
                .Include(t => t.Client)
                .Include(t => t.Transactions)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (trustAccount == null)
            {
                TempData["Error"] = "Trust account not found.";
                return RedirectToAction("AdminTrustAccounts");
            }

            var transactions = trustAccount.Transactions?
                .OrderByDescending(t => t.TransactionDate)
                .ToList() ?? new List<TrustTransaction>();

            var viewModel = new AdminTrustDetailsViewModel
            {
                TrustAccount = trustAccount,
                Transactions = transactions,
                TotalDeposited = trustAccount.TotalDeposited,
                TotalWithdrawn = trustAccount.TotalWithdrawn,
                CurrentBalance = trustAccount.Balance
            };

            return View(viewModel);
        }

        // GET: Retainer/TrustTransactionHistory - Client views their transaction history
        [HttpGet]
        public async Task<IActionResult> TrustTransactionHistory()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await _context.Users.FindAsync(userId.Value);
            var client = await _context.Clients.FirstOrDefaultAsync(c => c.Email == user.Email);

            if (client == null)
            {
                return RedirectToAction("Edit", "Client");
            }

            var trustAccount = await _context.TrustAccounts
                .Include(t => t.Transactions)
                .FirstOrDefaultAsync(t => t.ClientId == client.Id);

            if (trustAccount == null)
            {
                TempData["Info"] = "You don't have a trust account yet. Make a deposit to get started.";
                return RedirectToAction("DepositToTrust");
            }

            var transactions = trustAccount.Transactions?
                .OrderByDescending(t => t.TransactionDate)
                .ToList() ?? new List<TrustTransaction>();

            var viewModel = new ClientTrustViewModel
            {
                TrustAccount = trustAccount,
                RecentTransactions = transactions,
                Client = client
            };

            return View(viewModel);
        }

        // GET: Retainer/TrustTransactionHistory/Export - Export transaction history to CSV
        [HttpGet]
        public async Task<IActionResult> ExportTrustTransactions()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await _context.Users.FindAsync(userId.Value);
            var client = await _context.Clients.FirstOrDefaultAsync(c => c.Email == user.Email);

            if (client == null)
            {
                return RedirectToAction("Edit", "Client");
            }

            var trustAccount = await _context.TrustAccounts
                .Include(t => t.Transactions)
                .FirstOrDefaultAsync(t => t.ClientId == client.Id);

            if (trustAccount == null)
            {
                TempData["Error"] = "No trust account found.";
                return RedirectToAction("MyTrustAccount");
            }

            var transactions = trustAccount.Transactions?
                .OrderByDescending(t => t.TransactionDate)
                .ToList() ?? new List<TrustTransaction>();

            // Build CSV
            var csv = new StringBuilder();
            csv.AppendLine("Date,Type,Amount,Description,Reference");

            foreach (var transaction in transactions)
            {
                csv.AppendLine($"\"{transaction.TransactionDate:yyyy-MM-dd HH:mm}\",\"{transaction.Type}\",\"{transaction.Amount:N2}\",\"{transaction.Description}\",\"{transaction.Reference}\"");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            var fileName = $"Trust_Transaction_History_{DateTime.Now:yyyyMMdd}.csv";

            return File(bytes, "text/csv", fileName);
        }

        #endregion

        #region Helper Methods

        private async Task LogRetainerAction(int retainerId, string action, string details, int? userId)
        {
            var log = new RetainerActionLog
            {
                RetainerId = retainerId,
                Action = action,
                Details = details,
                UserId = userId,
                CreatedAt = DateTime.Now
            };
            _context.RetainerActionLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        private async Task NotifyLawyerPendingApproval(int retainerId)
        {
            var retainer = await _context.Retainers
                .Include(r => r.Client)
                .Include(r => r.AssignedLawyer)
                .FirstOrDefaultAsync(r => r.Id == retainerId);

            if (retainer?.AssignedLawyer != null)
            {
                await _notifications.QueueAsync(retainer.AssignedLawyer.Id, "RetainerApproval", "Retainer awaiting your approval",
                    $"{retainer.Title} for {retainer.Client?.FullName} requires review.", $"/Retainer/Review/{retainerId}", $"retainer-approval:{retainerId}:{retainer.AssignedLawyer.Id}");
            }
        }

        private async Task NotifyClient(int retainerId, string action)
        {
            var retainer = await _context.Retainers
                .Include(r => r.Client)
                .FirstOrDefaultAsync(r => r.Id == retainerId);

            if (retainer?.Client != null)
            {
                var clientUser = await _context.Users.FirstOrDefaultAsync(x => x.Role == UserRole.Client && x.Email.ToLower() == retainer.Client.Email.ToLower());
                if (clientUser is not null) await _notifications.QueueAsync(clientUser.Id, "RetainerStatus", $"Retainer {action}",
                    $"Your retainer {retainer.Title} was {action}.", $"/Retainer/ClientRetainerDetails/{retainerId}", $"retainer-client:{retainerId}:{action}:{clientUser.Id}");
            }
        }

        private async Task NotifyAdminChangesRequested(int retainerId)
        {
            foreach (var directorId in await _context.Users.Where(x => x.Role == UserRole.Director && x.IsActive).Select(x => x.Id).ToListAsync())
                await _notifications.QueueAsync(directorId, "RetainerChanges", "Retainer changes requested", $"Retainer #{retainerId} requires changes.", $"/Retainer/Details/{retainerId}", $"retainer-changes:{retainerId}:{directorId}");
        }

        private async Task NotifyAdminRejected(int retainerId)
        {
            foreach (var directorId in await _context.Users.Where(x => x.Role == UserRole.Director && x.IsActive).Select(x => x.Id).ToListAsync())
                await _notifications.QueueAsync(directorId, "RetainerRejected", "Retainer rejected", $"Retainer #{retainerId} was rejected by the reviewing lawyer.", $"/Retainer/Details/{retainerId}", $"retainer-rejected:{retainerId}:{directorId}");
        }

        private async Task ProcessRefund(int retainerId, decimal amount)
        {
            if (amount <= 0) throw new InvalidOperationException("Refund amount must be positive.");
            var retainer = await _context.Retainers.Include(x => x.Client).SingleAsync(x => x.Id == retainerId);
            var trust = await _context.TrustAccounts.SingleOrDefaultAsync(x => x.ClientId == retainer.ClientId);
            if (trust is null || trust.Balance < amount) throw new InvalidOperationException("The refund cannot be processed because sufficient cleared trust funds are unavailable.");
            trust.Balance -= amount; trust.TotalWithdrawn += amount; trust.LastUpdated = DateTime.UtcNow;
            _context.TrustTransactions.Add(new TrustTransaction { TrustAccountId=trust.Id,Type=TransactionType.Withdrawal,Amount=amount,
                Description=$"Retainer cancellation refund: {retainer.Title}",Reference=$"REF-{retainerId}-{DateTime.UtcNow:yyyyMMddHHmmss}",TransactionDate=DateTime.UtcNow,AuthorizedBy=GetCurrentUserName() });
            await _context.SaveChangesAsync();
            await LogRetainerAction(retainerId,"RefundProcessed",$"R {amount:N2} refunded from trust after cancellation.",GetCurrentUserId());
        }

        private async Task CreateTrustDeposit(int clientId, decimal amount, string reference)
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
                Description = $"Retainer payment - Reference: {reference}",
                Reference = reference,
                TransactionDate = DateTime.Now
            };

            trustAccount.Balance += amount;
            trustAccount.TotalDeposited += amount;
            trustAccount.LastUpdated = DateTime.Now;

            _context.TrustTransactions.Add(transaction);
            await _context.SaveChangesAsync();
        }

        private async Task<Dictionary<RetainerStatus, int>> GetStatusCounts()
        {
            var counts = await _context.Retainers
                .Where(r => !r.IsDeleted)
                .GroupBy(r => r.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(k => k.Status, v => v.Count);

            return counts;
        }

        private async Task<decimal> GetMonthlyRevenue()
        {
            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            var paidInvoices = await _context.Invoices
                .Where(i => i.PaidDate.HasValue && i.PaidDate.Value >= startOfMonth && i.PaidDate.Value <= endOfMonth && i.Status == InvoiceStatus.Paid)
                .SumAsync(i => i.TotalAmount);

            return paidInvoices;
        }

        private async Task<List<OverduePaymentInfo>> GetOverduePayments()
        {
            var today = DateTime.Now;
            var overdueInvoices = await _context.Invoices
                .Include(i => i.Client)
                .Where(i => i.Status != InvoiceStatus.Paid && i.DueDate < today)
                .Select(i => new OverduePaymentInfo
                {
                    InvoiceId = i.Id,
                    InvoiceNumber = i.InvoiceNumber,
                    ClientName = i.Client.FullName,
                    AmountDue = i.TotalAmount - (i.Payments.Sum(p => p.Amount)),
                    DueDate = i.DueDate,
                    DaysOverdue = (today - i.DueDate).Days
                })
                .ToListAsync();

            return overdueInvoices;
        }

        private string GenerateSignatureToken()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var bytes = new byte[32];
                rng.GetBytes(bytes);
                return Convert.ToBase64String(bytes).Replace("/", "_").Replace("+", "-").TrimEnd('=');
            }
        }

        private string GeneratePaymentReference()
        {
            return $"PAY-{DateTime.Now:yyyyMMddHHmmss}-{RandomNumberGenerator.GetInt32(1000, 9999)}";
        }

        private string GenerateInvoiceNumber()
        {
            var year = DateTime.Now.Year;
            var month = DateTime.Now.Month;
            var count = _context.Invoices.Count(i => i.IssueDate.Year == year) + 1;
            return $"INV-{year}{month:D2}-{count:D4}";
        }

        private async Task<string> GenerateRetainerPdf(Retainer retainer)
        {
            var pdfDirectory = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "retainers");
            if (!Directory.Exists(pdfDirectory))
            {
                Directory.CreateDirectory(pdfDirectory);
            }

            var fileName = $"Retainer_{retainer.Id}_{DateTime.Now:yyyyMMdd}.pdf";
            var filePath = Path.Combine(pdfDirectory, fileName);

            // Generate HTML content for PDF
            var htmlContent = await GenerateRetainerHtml(retainer);

            // TODO: Use a PDF generation library
            await System.IO.File.WriteAllTextAsync(filePath, htmlContent);

            return $"/uploads/retainers/{fileName}";
        }

        private async Task<string> GenerateRetainerHtml(Retainer retainer)
        {
            var client = await _context.Clients.FindAsync(retainer.ClientId);
            var template = await _context.RetainerTemplates.FindAsync(retainer.TemplateId);
            var lawyer = retainer.AssignedLawyerId.HasValue ? await _context.Users.FindAsync(retainer.AssignedLawyerId.Value) : null;

            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Legal Retainer Agreement</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 40px; }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        .title {{ font-size: 24px; font-weight: bold; }}
        .subtitle {{ font-size: 14px; color: #666; }}
        .section {{ margin-bottom: 20px; }}
        .section-title {{ font-size: 18px; font-weight: bold; border-bottom: 1px solid #ccc; margin-bottom: 10px; }}
        .content {{ line-height: 1.6; }}
        .signature-area {{ margin-top: 40px; }}
        .signature-line {{ border-top: 1px solid #000; width: 300px; margin-top: 20px; }}
        .footer {{ margin-top: 50px; font-size: 10px; text-align: center; color: #666; }}
    </style>
</head>
<body>
    <div class='header'>
        <div class='title'>LEGAL RETAINER AGREEMENT</div>
        <div class='subtitle'>Retainer #{retainer.Id} | {retainer.CreatedDate:MMMM dd, yyyy}</div>
    </div>
    
    <div class='section'>
        <div class='section-title'>1. PARTIES</div>
        <div class='content'>
            This Retainer Agreement is entered into between:<br/>
            <strong>Client:</strong> {client?.FullName}<br/>
            <strong>Law Firm:</strong> Simplex Law Firm<br/>
            {(lawyer != null ? $"<strong>Assigned Lawyer:</strong> {lawyer.FullName}<br/>" : "")}
        </div>
    </div>
    
    <div class='section'>
        <div class='section-title'>2. SERVICES</div>
        <div class='content'>
            <strong>Title:</strong> {retainer.Title}<br/><br/>
            <strong>Scope of Work:</strong><br/>
            {retainer.ScopeOfWork}
        </div>
    </div>
    
    <div class='section'>
        <div class='section-title'>3. FEES AND PAYMENT</div>
        <div class='content'>
            <strong>Retainer Type:</strong> {retainer.Type}<br/>
            <strong>Total Amount:</strong> R {retainer.Amount:N2}<br/>
            <strong>Billing Cycle:</strong> {retainer.BillingCycle}<br/>
            {(retainer.IncludedHours > 0 ? $"<strong>Included Hours:</strong> {retainer.IncludedHours}<br/>" : "")}
            {(retainer.OverageRate > 0 ? $"<strong>Overage Rate:</strong> R {retainer.OverageRate:N2}/hour<br/>" : "")}
            {(retainer.RequiresUpfrontPayment ? $"<strong>Payment Due:</strong> Within {retainer.PaymentDueDays} days of approval<br/>" : "")}
        </div>
    </div>
    
    <div class='section'>
        <div class='section-title'>4. TERM</div>
        <div class='content'>
            <strong>Effective Date:</strong> {retainer.StartDate:MMMM dd, yyyy}<br/>
            {(retainer.EndDate.HasValue ? $"<strong>End Date:</strong> {retainer.EndDate.Value:MMMM dd, yyyy}<br/>" : "<strong>End Date:</strong> Ongoing until terminated<br/>")}
        </div>
    </div>
    
    {(!string.IsNullOrEmpty(retainer.SpecialTerms) ? @$"
    <div class='section'>
        <div class='section-title'>5. SPECIAL TERMS</div>
        <div class='content'>
            {retainer.SpecialTerms}
        </div>
    </div>" : "")}
    
    {(retainer.LawyerNotes != null ? @$"
    <div class='section'>
        <div class='section-title'>6. LAWYER NOTES</div>
        <div class='content'>
            {retainer.LawyerNotes}
        </div>
    </div>" : "")}
    
    <div class='signature-area'>
        <div class='section-title'>SIGNATURES</div>
        <div class='content'>
            <p>By signing below, the Client acknowledges that they have read, understood, and agree to be bound by the terms and conditions of this Retainer Agreement.</p>
            
            <div style='display: inline-block; width: 45%;'>
                <div class='signature-line'></div>
                <div><strong>Client Signature:</strong> {retainer.ClientSignatureName ?? "_________________________"}</div>
                <div>Date: {(retainer.SignedDate.HasValue ? retainer.SignedDate.Value.ToString("MMMM dd, yyyy") : "_______________")}</div>
            </div>
            
            <div style='display: inline-block; width: 45%; margin-left: 9%;'>
                <div class='signature-line'></div>
                <div><strong>Law Firm Representative:</strong> _________________________</div>
                <div>Date: {DateTime.Now:MMMM dd, yyyy}</div>
            </div>
        </div>
    </div>
    
    <div class='footer'>
        This is a legally binding agreement. Please retain a copy for your records.
        {(!string.IsNullOrEmpty(retainer.ClientIPAddress) ? $"Signed from IP: {retainer.ClientIPAddress}" : "")}
    </div>
</body>
</html>";

            return html;
        }

        private int? GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId");
        }

        private string GetCurrentUserName()
        {
            return HttpContext.Session.GetString("UserName") ?? "System";
        }

        private UserRole GetCurrentUserRole()
        {
            var roleString = HttpContext.Session.GetString("UserRole");
            if (Enum.TryParse<UserRole>(roleString, out var role))
            {
                return role;
            }
            return UserRole.Client;
        }

        #endregion
    }
}
