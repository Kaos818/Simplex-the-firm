using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using SimplexLawFirm.Infrastructure.Authorization;

namespace SimplexLawFirm.Controllers
{
    [RequireSessionUser]
    public class DocumentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public DocumentController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Document/Index
        public async Task<IActionResult> Index(int? caseId, int? clientId, DocumentCategory? category = null)
        {
            if (HttpContext.Session.GetString("UserRole") == "Client") return RedirectToAction(nameof(ClientIndex));
            var query = _context.Documents
                .Include(d => d.UploadedBy)
                .Include(d => d.Case)
                .Include(d => d.Client)
                .AsQueryable();
            query = ApplyDocumentScope(query);

            if (caseId.HasValue)
            {
                query = query.Where(d => d.CaseId == caseId.Value);
                ViewBag.CurrentCase = await _context.Cases.FindAsync(caseId.Value);
            }

            if (clientId.HasValue)
            {
                query = query.Where(d => d.ClientId == clientId.Value);
                ViewBag.CurrentClient = await _context.Clients.FindAsync(clientId.Value);
            }

            if (category.HasValue)
            {
                query = query.Where(d => d.Category == category.Value);
            }

            var documents = await query.OrderByDescending(d => d.UploadedAt).ToListAsync();

            // Get statistics
            ViewBag.TotalDocuments = documents.Count;
            ViewBag.TotalSize = FormatFileSize(documents.Sum(d => ParseFileSize(d.FileSize)));
            ViewBag.Categories = Enum.GetValues(typeof(DocumentCategory));
            ViewBag.SelectedCategory = category;

            // Get recent uploads (last 7 days)
            ViewBag.RecentUploads = documents.Count(d => d.UploadedAt >= DateTime.Now.AddDays(-7));

            return View(documents);
        }

        [RequireSessionRole("Client")]
        public async Task<IActionResult> ClientIndex(CancellationToken ct)
        {
            var email = HttpContext.Session.GetString("UserEmail");
            var userId = HttpContext.Session.GetInt32("UserId")!.Value;
            var clientId = await _context.Clients.Where(x => x.Email == email).Select(x => (int?)x.Id).SingleOrDefaultAsync(ct);
            if (clientId is null) return Forbid();
            var documents = await _context.Documents.Include(x => x.UploadedBy).Include(x => x.Case).Include(x => x.SharedWith).ThenInclude(x => x.SharedByUser)
                .Where(x => x.ClientId == clientId || (x.CaseId != null && x.Case!.ClientId == clientId) || x.SharedWith.Any(s => s.SharedWithUserId == userId && (s.ExpiresAt == null || s.ExpiresAt > DateTime.Now)))
                .OrderByDescending(x => x.UploadedAt).ToListAsync(ct);
            return View(documents);
        }

        public IActionResult LawyerIndex() => RedirectToAction(nameof(Index));

        // GET: Document/Upload
        [RequireSessionRole("Admin", "Lawyer", "Paralegal")]
        public async Task<IActionResult> Upload(int? caseId, int? clientId, string? requirementCode)
        {
            ViewBag.Cases = new SelectList(_context.Cases, "Id", "Title");
            ViewBag.Clients = new SelectList(_context.Clients, "Id", "FullName");
            ViewBag.Categories = new SelectList(Enum.GetValues(typeof(DocumentCategory)));

            var model = new Document();
            if (caseId.HasValue) model.CaseId = caseId.Value;
            if (clientId.HasValue) model.ClientId = clientId.Value;
            await LoadRequirementsAsync(caseId);
            if (!string.IsNullOrWhiteSpace(requirementCode)) model.RequirementCode = requirementCode;

            return View(model);
        }

        // POST: Document/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireSessionRole("Admin", "Lawyer", "Paralegal")]
        public async Task<IActionResult> Upload(Document model, IFormFile file)
        {
            if (model.CaseId.HasValue && !string.IsNullOrWhiteSpace(model.RequirementCode))
            {
                var caseType = await _context.Cases.Where(x => x.Id == model.CaseId).Select(x => x.CaseType).SingleOrDefaultAsync();
                if (caseType is null || !await _context.CaseDocumentRequirements.AnyAsync(x => x.IsActive && x.Code == model.RequirementCode && (x.CaseType == caseType || x.CaseType == "General")))
                { ModelState.AddModelError(nameof(model.RequirementCode), "Select a valid document requirement for this matter."); await LoadRequirementsAsync(model.CaseId); return View(model); }
            }
            if (file != null && file.Length > 0)
            {
                // Validate file type
                var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".jpg", ".png", ".txt", ".msg", ".eml" };
                var fileExt = Path.GetExtension(file.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExt))
                {
                    TempData["Error"] = "Invalid file type. Allowed: PDF, DOC, XLS, JPG, PNG, TXT, MSG, EML";
                    await LoadRequirementsAsync(model.CaseId);
                    return View(model);
                }

                // Validate file size (max 50MB)
                if (file.Length > 50 * 1024 * 1024)
                {
                    TempData["Error"] = "File size exceeds 50MB limit.";
                    await LoadRequirementsAsync(model.CaseId);
                    return View(model);
                }

                // Create directory structure
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "documents");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                // Generate unique filename
                var uniqueFileName = $"{Guid.NewGuid()}_{DateTime.Now:yyyyMMddHHmmss}_{SanitizeFileName(file.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Check if this is a new version of an existing document
                if (model.ParentDocumentId.HasValue)
                {
                    var parentDoc = await _context.Documents.FindAsync(model.ParentDocumentId.Value);
                    if (parentDoc != null)
                    {
                        model.VersionNumber = (parentDoc.VersionNumber ?? 0) + 1;
                        model.FileName = parentDoc.FileName;
                    }
                }
                else
                {
                    model.VersionNumber = 1;
                }

                // Save to database
                model.FileName = file.FileName;
                model.FilePath = $"/uploads/documents/{uniqueFileName}";
                model.FileSize = FormatFileSize(file.Length);
                model.FileType = file.ContentType;
                model.UploadedAt = DateTime.Now;
                model.UploadedById = HttpContext.Session.GetInt32("UserId");

                _context.Documents.Add(model);
                await _context.SaveChangesAsync();

                // Create audit log entry
                await CreateAuditLog($"Document uploaded: {file.FileName} (Case: {model.CaseId}, Client: {model.ClientId})");

                TempData["Success"] = $"Document '{file.FileName}' uploaded successfully!";
                return RedirectToAction("Index", new { caseId = model.CaseId, clientId = model.ClientId });
            }

            TempData["Error"] = "Please select a file to upload.";
            await LoadRequirementsAsync(model.CaseId);
            return View(model);
        }

        // GET: Document/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var document = await _context.Documents
                .Include(d => d.UploadedBy)
                .Include(d => d.Case)
                .Include(d => d.Client)
                .Include(d => d.SharedWith)
                    .ThenInclude(s => s.SharedWithUser)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (document == null)
            {
                TempData["Error"] = "Document not found.";
                return RedirectToAction("Index");
            }
            if (!await CanAccessAsync(document)) return Forbid();

            // Get version history
            var versions = await _context.Documents
                .Where(d => d.ParentDocumentId == id || d.Id == id)
                .OrderByDescending(d => d.VersionNumber)
                .ToListAsync();

            ViewBag.Versions = versions;
            ViewBag.IsLatestVersion = versions.FirstOrDefault()?.Id == id;

            return View(document);
        }

        // GET: Document/Download/5
        public async Task<IActionResult> Download(int id)
        {
            var document = await _context.Documents.Include(x => x.Case).Include(x => x.SharedWith).SingleOrDefaultAsync(x => x.Id == id);
            if (document == null)
            {
                TempData["Error"] = "Document not found.";
                return RedirectToAction("Index");
            }
            if (!await CanAccessAsync(document)) return Forbid();

            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, document.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(filePath))
            {
                TempData["Error"] = "File not found on server.";
                return RedirectToAction("Index");
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);

            // Track download for audit
            await CreateAuditLog($"Document downloaded: {document.FileName}");

            return File(fileBytes, document.FileType ?? "application/octet-stream", document.FileName);
        }

        // GET: Document/Preview/5
        public async Task<IActionResult> Preview(int id)
        {
            var document = await _context.Documents.Include(x => x.Case).Include(x => x.SharedWith).SingleOrDefaultAsync(x => x.Id == id);
            if (document == null)
            {
                return NotFound();
            }
            if (!await CanAccessAsync(document)) return Forbid();

            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, document.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);

            // For PDFs, display inline; for others, force download
            if (document.FileType == "application/pdf")
            {
                Response.Headers["Content-Disposition"] = "inline";
                return File(fileBytes, document.FileType);
            }

            return File(fileBytes, document.FileType ?? "application/octet-stream", document.FileName);
        }

        // POST: Document/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireSessionRole("Admin", "Lawyer", "Paralegal")]
        public async Task<IActionResult> Delete(int id)
        {
            var document = await _context.Documents
                .Include(d => d.SharedWith)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (document == null)
            {
                return Json(new { success = false, message = "Document not found" });
            }

            // Delete physical file
            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, document.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            // Delete all shares
            if (document.SharedWith != null)
            {
                _context.DocumentShares.RemoveRange(document.SharedWith);
            }

            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();

            await CreateAuditLog($"Document deleted: {document.FileName}");

            return Json(new { success = true, message = "Document deleted successfully." });
        }

        // GET: Document/Share/5
        [RequireSessionRole("Admin", "Lawyer", "Paralegal")]
        public async Task<IActionResult> Share(int id)
        {
            var document = await _context.Documents.FindAsync(id);
            if (document == null)
            {
                TempData["Error"] = "Document not found.";
                return RedirectToAction("Index");
            }

            var users = await _context.Users
                .Where(u => u.IsActive && u.Role != UserRole.Admin)
                .Select(u => new { u.Id, Name = $"{u.FullName} ({u.Email})" })
                .ToListAsync();

            ViewBag.Users = new SelectList(users, "Id", "Name");
            ViewBag.Document = document;

            return View();
        }

        // POST: Document/Share
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireSessionRole("Admin", "Lawyer", "Paralegal")]
        public async Task<IActionResult> Share(int documentId, int userId, int daysValid = 7)
        {
            var existingShare = await _context.DocumentShares
                .FirstOrDefaultAsync(s => s.DocumentId == documentId && s.SharedWithUserId == userId);

            if (existingShare != null)
            {
                TempData["Warning"] = "Document already shared with this user.";
                return RedirectToAction("Index");
            }

            var share = new DocumentShare
            {
                DocumentId = documentId,
                SharedWithUserId = userId,
                SharedByUserId = HttpContext.Session.GetInt32("UserId").Value,
                SharedAt = DateTime.Now,
                ExpiresAt = daysValid > 0 ? DateTime.Now.AddDays(daysValid) : null,
                AccessToken = GenerateAccessToken()
            };

            _context.DocumentShares.Add(share);
            await _context.SaveChangesAsync();

            await CreateAuditLog($"Document shared with user {userId}");

            TempData["Success"] = "Document shared successfully!";
            return RedirectToAction("Details", new { id = documentId });
        }

        // GET: Document/SharedWithMe
        public async Task<IActionResult> SharedWithMe()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Index", "Home");
            }

            var sharedDocuments = await _context.DocumentShares
                .Include(s => s.Document)
                    .ThenInclude(d => d.UploadedBy)
                .Include(s => s.Document)
                    .ThenInclude(d => d.Case)
                .Include(s => s.SharedByUser)
                .Where(s => s.SharedWithUserId == userId && (s.ExpiresAt == null || s.ExpiresAt > DateTime.Now))
                .OrderByDescending(s => s.SharedAt)
                .Select(s => s.Document)
                .ToListAsync();

            return View(sharedDocuments);
        }

        // GET: Document/GetSharedLink/5
        [RequireSessionRole("Admin", "Lawyer", "Paralegal")]
        public async Task<IActionResult> GetSharedLink(int id, int daysValid = 7)
        {
            var document = await _context.Documents.FindAsync(id);
            if (document == null)
            {
                return Json(new { success = false, error = "Document not found" });
            }

            var share = new DocumentShare
            {
                DocumentId = id,
                SharedWithUserId = 0, // Public share
                SharedByUserId = HttpContext.Session.GetInt32("UserId").Value,
                SharedAt = DateTime.Now,
                ExpiresAt = DateTime.Now.AddDays(daysValid),
                AccessToken = GenerateAccessToken()
            };

            _context.DocumentShares.Add(share);
            await _context.SaveChangesAsync();

            var shareUrl = $"{Request.Scheme}://{Request.Host}/Document/PublicAccess/{share.AccessToken}";

            return Json(new { success = true, shareUrl = shareUrl, expiresAt = share.ExpiresAt });
        }

        // GET: Document/PublicAccess/{token}
        public async Task<IActionResult> PublicAccess(string token)
        {
            var share = await _context.DocumentShares
                .Include(s => s.Document)
                .FirstOrDefaultAsync(s => s.AccessToken == token && (s.ExpiresAt == null || s.ExpiresAt > DateTime.Now));

            if (share == null || share.Document == null)
            {
                TempData["Error"] = "Invalid or expired share link.";
                return RedirectToAction("Index", "Home");
            }

            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, share.Document.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(filePath))
            {
                TempData["Error"] = "File not found.";
                return RedirectToAction("Index", "Home");
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);

            // Track access
            await CreateAuditLog($"Public access: {share.Document.FileName} via token");

            return File(fileBytes, share.Document.FileType ?? "application/octet-stream", share.Document.FileName);
        }

        // GET: Document/BulkUpload
        [RequireSessionRole("Admin", "Lawyer", "Paralegal")]
        public IActionResult BulkUpload(int? caseId, int? clientId)
        {
            ViewBag.Cases = new SelectList(_context.Cases, "Id", "Title");
            ViewBag.Clients = new SelectList(_context.Clients, "Id", "FullName");
            ViewBag.Categories = new SelectList(Enum.GetValues(typeof(DocumentCategory)));

            ViewBag.CaseId = caseId;
            ViewBag.ClientId = clientId;

            return View();
        }

        // POST: Document/BulkUpload
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireSessionRole("Admin", "Lawyer", "Paralegal")]
        public async Task<IActionResult> BulkUpload(List<IFormFile> files, int? caseId, int? clientId, DocumentCategory category, string description)
        {
            if (files == null || !files.Any())
            {
                TempData["Error"] = "No files selected for upload.";
                return RedirectToAction("BulkUpload");
            }

            var uploadCount = 0;
            var errors = new List<string>();

            foreach (var file in files)
            {
                if (file != null && file.Length > 0)
                {
                    try
                    {
                        var document = new Document
                        {
                            CaseId = caseId,
                            ClientId = clientId,
                            Category = category,
                            Description = description,
                            UploadedAt = DateTime.Now,
                            UploadedById = HttpContext.Session.GetInt32("UserId")
                        };

                        // Save file
                        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "documents");
                        if (!Directory.Exists(uploadsFolder))
                            Directory.CreateDirectory(uploadsFolder);

                        var uniqueFileName = $"{Guid.NewGuid()}_{DateTime.Now:yyyyMMddHHmmss}_{SanitizeFileName(file.FileName)}";
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        document.FileName = file.FileName;
                        document.FilePath = $"/uploads/documents/{uniqueFileName}";
                        document.FileSize = FormatFileSize(file.Length);
                        document.FileType = file.ContentType;
                        document.VersionNumber = 1;

                        _context.Documents.Add(document);
                        uploadCount++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{file.FileName}: {ex.Message}");
                    }
                }
            }

            await _context.SaveChangesAsync();

            if (uploadCount > 0)
            {
                TempData["Success"] = $"{uploadCount} document(s) uploaded successfully!";
                if (errors.Any())
                {
                    TempData["Warning"] = $"Uploaded {uploadCount} files with {errors.Count} errors.";
                }
            }
            else
            {
                TempData["Error"] = "No files were uploaded successfully.";
            }

            return RedirectToAction("Index", new { caseId = caseId, clientId = clientId });
        }

        // Helper Methods
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private long ParseFileSize(string sizeString)
        {
            if (string.IsNullOrEmpty(sizeString)) return 0;

            var parts = sizeString.Split(' ');
            if (parts.Length != 2) return 0;

            if (!double.TryParse(parts[0], out var value)) return 0;

            return parts[1] switch
            {
                "KB" => (long)(value * 1024),
                "MB" => (long)(value * 1024 * 1024),
                "GB" => (long)(value * 1024 * 1024 * 1024),
                _ => (long)value
            };
        }

        private string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        }

        private IQueryable<Document> ApplyDocumentScope(IQueryable<Document> query)
        {
            var role = HttpContext.Session.GetString("UserRole");
            var userId = HttpContext.Session.GetInt32("UserId")!.Value;
            return role == "Lawyer" ? query.Where(x => x.CaseId == null || x.Case!.LawyerId == userId || x.UploadedById == userId || x.SharedWith.Any(s => s.SharedWithUserId == userId && (s.ExpiresAt == null || s.ExpiresAt > DateTime.Now))) : query;
        }

        private async Task LoadRequirementsAsync(int? caseId)
        {
            if (!caseId.HasValue) { ViewBag.Requirements = Array.Empty<CaseDocumentRequirement>(); return; }
            var caseType = await _context.Cases.Where(x => x.Id == caseId).Select(x => x.CaseType).SingleOrDefaultAsync();
            ViewBag.Requirements = await _context.CaseDocumentRequirements.Where(x => x.IsActive && (x.CaseType == caseType || x.CaseType == "General")).OrderBy(x => x.DisplayOrder).ToListAsync();
        }

        private async Task<bool> CanAccessAsync(Document document)
        {
            var role = HttpContext.Session.GetString("UserRole");
            var userId = HttpContext.Session.GetInt32("UserId")!.Value;
            if (role is "Admin" or "Paralegal" or "Accountant") return true;
            if (document.UploadedById == userId || document.SharedWith?.Any(x => x.SharedWithUserId == userId && (x.ExpiresAt == null || x.ExpiresAt > DateTime.Now)) == true) return true;
            if (role == "Lawyer") return document.CaseId.HasValue && (document.Case?.LawyerId ?? await _context.Cases.Where(x => x.Id == document.CaseId).Select(x => x.LawyerId).SingleOrDefaultAsync()) == userId;
            if (role != "Client") return false;
            var email = HttpContext.Session.GetString("UserEmail");
            var clientId = await _context.Clients.Where(x => x.Email == email).Select(x => (int?)x.Id).SingleOrDefaultAsync();
            return clientId.HasValue && (document.ClientId == clientId || document.CaseId.HasValue && await _context.Cases.AnyAsync(x => x.Id == document.CaseId && x.ClientId == clientId));
        }

        private string GenerateAccessToken()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("/", "_").Replace("+", "-");
        }

        private async Task CreateAuditLog(string action)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            System.Diagnostics.Debug.WriteLine($"AUDIT: User {userId} - {action} at {DateTime.Now}");
        }
    }
}
