using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using BCrypt.Net;
using SimplexLawFirm.Infrastructure.Authorization;

namespace SimplexLawFirm.Controllers
{
    [RequireSessionRole("Admin"), AutoValidateAntiforgeryToken]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Users/Index
        public async Task<IActionResult> Index(string role = null, bool? isActive = null)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(role) && Enum.TryParse<UserRole>(role, true, out var roleEnum))
            {
                query = query.Where(u => u.Role == roleEnum);
            }

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            var users = await query.OrderBy(u => u.Role).ThenBy(u => u.FullName).ToListAsync();

            ViewBag.SelectedRole = role;
            ViewBag.ShowActive = isActive;
            ViewBag.RoleOptions = Enum.GetValues(typeof(UserRole));
            ViewBag.TotalUsers = users.Count;
            ViewBag.ActiveUsers = users.Count(u => u.IsActive);
            ViewBag.InactiveUsers = users.Count(u => !u.IsActive);

            // Role counts
            ViewBag.AdminCount = users.Count(u => u.Role == UserRole.Admin);
            ViewBag.LawyerCount = users.Count(u => u.Role == UserRole.Lawyer);
            ViewBag.ParalegalCount = users.Count(u => u.Role == UserRole.Paralegal);
            ViewBag.AccountantCount = users.Count(u => u.Role == UserRole.Accountant);
            ViewBag.ClientCount = users.Count(u => u.Role == UserRole.Client);

            return View(users);
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Index");
            }

            // Get lawyer profile if applicable
            if (user.Role == UserRole.Lawyer)
            {
                var lawyerProfile = await _context.LawyerProfiles
                    .Include(lp => lp.Specializations)
                    .FirstOrDefaultAsync(lp => lp.UserId == id);
                ViewBag.LawyerProfile = lawyerProfile;
            }

            // Get client profile if applicable
            if (user.Role == UserRole.Client)
            {
                var client = await _context.Clients.FirstOrDefaultAsync(c => c.Email == user.Email);
                ViewBag.Client = client;
            }

            // Get assigned cases (for lawyers)
            if (user.Role == UserRole.Lawyer)
            {
                var assignedCases = await _context.Cases
                    .Include(c => c.Client)
                    .Where(c => c.LawyerId == id && c.Status != CaseStatus.Archived)
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(10)
                    .ToListAsync();
                ViewBag.AssignedCases = assignedCases;
            }

            ViewBag.LastLogin = user.LastLoginAt?.ToString("dd MMM yyyy HH:mm") ?? "Never";

            return View(user);
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            // Only show staff roles for admin creation (exclude Client)
            var staffRoles = new List<UserRole>
            {
                UserRole.Admin,
                UserRole.Lawyer,
                UserRole.Paralegal,
                UserRole.Accountant
            };
            ViewBag.Roles = new SelectList(staffRoles);
            return View(new ApplicationUser { IsActive = true, EmailConfirmed = true, CreatedAt = DateTime.Now });
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please fix the errors.";
                ViewBag.Roles = new SelectList(new[]
                {
            UserRole.Admin,
            UserRole.Lawyer,
            UserRole.Paralegal,
            UserRole.Accountant
        });
                return View(model);
            }

            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                TempData["Error"] = "User already exists.";
                return View(model);
            }

            var password = string.IsNullOrEmpty(model.Password)
                ? "Password123!"
                : model.Password;

            var user = new ApplicationUser
            {
                FullName = model.FullName,
                Email = model.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = model.Role,
                IsActive = model.IsActive,
                EmailConfirmed = true,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Staff member created successfully!";
            return RedirectToAction("Index");
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Index");
            }

            // Don't allow role change for clients through this controller
            var staffRoles = new List<UserRole> { UserRole.Admin, UserRole.Lawyer, UserRole.Paralegal, UserRole.Accountant };
            ViewBag.Roles = new SelectList(staffRoles, user.Role);
            return View(user);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ApplicationUser user)
        {
            if (id != user.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.Users.FindAsync(id);
                    if (existing == null)
                    {
                        return NotFound();
                    }

                    var oldRole = existing.Role;
                    var oldStatus = existing.IsActive;

                    existing.FullName = user.FullName;
                    existing.Email = user.Email;
                    existing.Role = user.Role;
                    existing.IsActive = user.IsActive;

                    await _context.SaveChangesAsync();

                    await CreateAuditLog($"User updated: {existing.FullName}. Role: {oldRole} -> {existing.Role}, Status: {oldStatus} -> {existing.IsActive}");

                    // If role changed to Lawyer, create lawyer profile
                    if (existing.Role == UserRole.Lawyer && !await _context.LawyerProfiles.AnyAsync(lp => lp.UserId == id))
                    {
                        var lawyerProfile = new LawyerProfile
                        {
                            UserId = id,
                            IsActive = true,
                            HourlyRate = 2000,
                            YearsOfExperience = 0,
                            BarNumber = GenerateBarNumber(),
                            OfficeLocation = "Main Office",
                            Bio = "Please complete your profile."
                        };
                        _context.LawyerProfiles.Add(lawyerProfile);
                        await _context.SaveChangesAsync();
                    }

                    TempData["Success"] = "User updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Users.Any(e => e.Id == id))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction("Details", new { id = user.Id });
            }

            var staffRoles = new List<UserRole> { UserRole.Admin, UserRole.Lawyer, UserRole.Paralegal, UserRole.Accountant };
            ViewBag.Roles = new SelectList(staffRoles, user.Role);
            return View(user);
        }

        // POST: Users/ResetPassword
        [HttpPost]
        public async Task<IActionResult> ResetPassword(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            var newPassword = GenerateRandomPassword();
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;
            await _context.SaveChangesAsync();

            await CreateAuditLog($"Password reset for user: {user.FullName}");

            // In a real app, send email with new password
            return Json(new { success = true, message = $"Password reset successfully. New password: {newPassword}" });
        }

        // POST: Users/ToggleStatus
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            // Don't allow deactivating own account
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId == id)
            {
                return Json(new { success = false, message = "You cannot deactivate your own account" });
            }

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();

            await CreateAuditLog($"User status changed: {user.FullName} -> {(user.IsActive ? "Activated" : "Deactivated")}");

            var status = user.IsActive ? "activated" : "deactivated";
            return Json(new { success = true, message = $"User {status} successfully", isActive = user.IsActive });
        }

        // POST: Users/Delete/5 (Soft delete)
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            // Don't allow deleting self
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId == id)
            {
                return Json(new { success = false, message = "Cannot delete your own account" });
            }

            // Soft delete - just deactivate
            user.IsActive = false;
            await _context.SaveChangesAsync();

            await CreateAuditLog($"User deactivated: {user.FullName}");

            return Json(new { success = true, message = "User deactivated successfully" });
        }

        // GET: Users/Profile (for current logged-in user)
        public async Task<IActionResult> Profile()
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

            return View(user);
        }

        // POST: Users/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ApplicationUser model, string currentPassword, string newPassword, string confirmPassword)
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

            // Update name
            user.FullName = model.FullName;

            // Update password if provided
            if (!string.IsNullOrEmpty(newPassword))
            {
                if (string.IsNullOrEmpty(currentPassword))
                {
                    TempData["Error"] = "Current password is required to change password.";
                    return View("Profile", user);
                }

                if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
                {
                    TempData["Error"] = "Current password is incorrect.";
                    return View("Profile", user);
                }

                if (newPassword != confirmPassword)
                {
                    TempData["Error"] = "New password and confirmation do not match.";
                    return View("Profile", user);
                }

                if (newPassword.Length < 6)
                {
                    TempData["Error"] = "Password must be at least 6 characters.";
                    return View("Profile", user);
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                await CreateAuditLog($"Password changed for user: {user.FullName}");
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Profile updated successfully!";

            return RedirectToAction("Profile");
        }

        // GET: Users/GetLawyers (for dropdowns - ONLY LAWYERS)
        [HttpGet]
        public async Task<IActionResult> GetLawyers()
        {
            var lawyers = await _context.Users
                .Where(u => u.Role == UserRole.Lawyer && u.IsActive)
                .Select(u => new { u.Id, u.FullName, u.Email })
                .OrderBy(u => u.FullName)
                .ToListAsync();

            return Json(lawyers);
        }

        // GET: Users/GetStaff (for admin dropdowns - excludes clients)
        [HttpGet]
        public async Task<IActionResult> GetStaff()
        {
            var staff = await _context.Users
                .Where(u => u.Role != UserRole.Client && u.IsActive)
                .Select(u => new { u.Id, u.FullName, u.Email, u.Role })
                .OrderBy(u => u.Role)
                .ThenBy(u => u.FullName)
                .ToListAsync();

            return Json(staff);
        }

        // GET: Users/GetUsersByRole (with role filtering)
        [HttpGet]
        public async Task<IActionResult> GetUsersByRole(UserRole role)
        {
            var users = await _context.Users
                .Where(u => u.Role == role && u.IsActive)
                .Select(u => new { u.Id, u.FullName, u.Email })
                .ToListAsync();

            return Json(users);
        }

        // GET: Users/GetLawyerAvailability (check if lawyer is available on a date)
        [HttpGet]
        public async Task<IActionResult> GetLawyerAvailability(int lawyerId, DateTime date)
        {
            var events = await _context.CalendarEvents
                .Where(e => e.AssignedToUserId == lawyerId &&
                           e.StartDateTime.Date == date.Date &&
                           e.Status != EventStatus.Cancelled)
                .Select(e => new { e.StartDateTime, e.EndDateTime, e.Title })
                .ToListAsync();

            var isAvailable = !events.Any();
            return Json(new { isAvailable, busySlots = events });
        }

        // GET: Users/AuditLogs (View system audit logs)
        public async Task<IActionResult> AuditLogs(DateTime? startDate, DateTime? endDate, string userEmail = null)
        {
            // This would typically query an AuditLog table
            // For now, we'll return a view with placeholder
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd") ?? DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd") ?? DateTime.Now.ToString("yyyy-MM-dd");
            ViewBag.UserEmail = userEmail;

            var users = await _context.Users
                .Select(u => new { u.Id, u.Email, u.FullName })
                .ToListAsync();
            ViewBag.Users = new SelectList(users, "Email", "FullName");

            return View();
        }

        // GET: Users/GetAuditLogs (AJAX endpoint for audit logs)
        [HttpGet]
        public async Task<IActionResult> GetAuditLogs(DateTime startDate, DateTime endDate, string userEmail = null)
        {
            // This is a placeholder - in a real implementation, you would have an AuditLog table
            // For now, return some sample data
            var sampleLogs = new List<object>
            {
                new { Timestamp = DateTime.Now.AddHours(-1), User = "admin@simplex.com", Action = "User login", Details = "Successful login", IpAddress = "192.168.1.1" },
                new { Timestamp = DateTime.Now.AddHours(-2), User = "lawyer@simplex.com", Action = "Case viewed", Details = "Viewed case PI-2026-0001", IpAddress = "192.168.1.2" },
                new { Timestamp = DateTime.Now.AddHours(-3), User = "admin@simplex.com", Action = "Document uploaded", Details = "Uploaded contract.pdf to case BC-2026-0003", IpAddress = "192.168.1.1" },
                new { Timestamp = DateTime.Now.AddDays(-1), User = "client@simplex.com", Action = "Retainer signed", Details = "Signed retainer #123", IpAddress = "192.168.1.5" }
            };

            return Json(sampleLogs);
        }

        // GET: Users/PerformanceReport
        public async Task<IActionResult> PerformanceReport(DateTime? startDate, DateTime? endDate)
        {
            startDate ??= DateTime.Now.AddMonths(-1);
            endDate ??= DateTime.Now;

            var lawyers = await _context.Users
                .Where(u => u.Role == UserRole.Lawyer && u.IsActive)
                .ToListAsync();

            var lawyerPerformance = new List<object>();

            foreach (var lawyer in lawyers)
            {
                var cases = await _context.Cases
                    .Where(c => c.LawyerId == lawyer.Id && c.CreatedAt >= startDate && c.CreatedAt <= endDate)
                    .ToListAsync();

                var closedCases = cases.Count(c => c.Status == CaseStatus.Closed);
                var activeCases = cases.Count(c => c.Status == CaseStatus.Active);

                // Get billable hours from TimeEntries
                var billableHours = await _context.TimeEntries
                    .Where(t => t.LawyerId == lawyer.Id && t.Date >= startDate && t.Date <= endDate && t.IsBillable)
                    .SumAsync(t => t.Hours);

                lawyerPerformance.Add(new
                {
                    LawyerName = lawyer.FullName,
                    TotalCases = cases.Count,
                    ActiveCases = activeCases,
                    ClosedCases = closedCases,
                    BillableHours = billableHours,
                    EstimatedRevenue = billableHours * 2000 // Assuming avg rate of R2000/hour
                });
            }

            ViewBag.StartDate = startDate.Value.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate.Value.ToString("yyyy-MM-dd");
            ViewBag.LawyerPerformance = lawyerPerformance;
            ViewBag.TotalCases = await _context.Cases.CountAsync(c => c.CreatedAt >= startDate && c.CreatedAt <= endDate);
            ViewBag.TotalBillableHours = await _context.TimeEntries
                .Where(t => t.Date >= startDate && t.Date <= endDate && t.IsBillable)
                .SumAsync(t => t.Hours);

            return View();
        }

        // Helper Methods
        private string GenerateBarNumber()
        {
            var year = DateTime.Now.Year;
            var count = _context.LawyerProfiles.Count() + 1;
            return $"BAR-{year}-{count:D4}";
        }

        private string GenerateRandomPassword(int length = 10)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
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
}
