using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services.Email;

namespace SimplexLawFirm.Controllers;

public sealed class HomeController(
    ApplicationDbContext db,
    IEmailService email,
    IConfiguration configuration) : Controller
{
    private const string RememberCookie = "Simplex.RememberMe";
    private static readonly TimeSpan ResetLifetime = TimeSpan.FromHours(1);

    [HttpGet]
    public IActionResult Index()
    {
        var role = HttpContext.Session.GetString("UserRole");
        return string.IsNullOrWhiteSpace(role) ? View() : RedirectToDashboard(role);
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (!string.IsNullOrWhiteSpace(HttpContext.Session.GetString("UserRole")))
            return RedirectToLocalOrDashboard(returnUrl, HttpContext.Session.GetString("UserRole")!);
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string emailAddress, string password, bool rememberMe = false, string? returnUrl = null, CancellationToken ct = default)
    {
        var normalizedEmail = NormalizeEmail(emailAddress);
        if (normalizedEmail.Length == 0 || string.IsNullOrWhiteSpace(password))
            return LoginError("Please enter both email and password.", returnUrl);

        var user = await db.Users.SingleOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail, ct);
        if (user is null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return LoginError("Invalid email or password.", returnUrl);
        if (!user.IsActive) return LoginError("Account is inactive. Contact the Director.", returnUrl);
        if (!user.EmailConfirmed) return LoginError("Please confirm your email before signing in.", returnUrl);

        SetSession(user);
        user.LastLoginAt = DateTime.UtcNow;

        if (rememberMe)
        {
            var rawToken = GenerateToken();
            user.RememberMeToken = HashToken(rawToken);
            Response.Cookies.Append(RememberCookie, rawToken, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(30),
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Strict,
                Secure = Request.IsHttps
            });
        }
        else
        {
            user.RememberMeToken = null;
            Response.Cookies.Delete(RememberCookie);
        }

        await db.SaveChangesAsync(ct);
        TempData["Success"] = $"Welcome back, {user.FullName}.";
        return RedirectToLocalOrDashboard(returnUrl, user.Role.ToString());
    }

    [HttpGet]
    public IActionResult Register() =>
        string.IsNullOrWhiteSpace(HttpContext.Session.GetString("UserRole"))
            ? View()
            : RedirectToAction(nameof(Index));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken ct = default)
    {
        if (!ModelState.IsValid) return View(model);

        model.Email = NormalizeEmail(model.Email);
        if (await db.Users.AnyAsync(x => x.Email.ToLower() == model.Email, ct))
        {
            ModelState.AddModelError(nameof(model.Email), "An account with this email address already exists.");
            return View(model);
        }

        var rawToken = GenerateToken();
        var user = new ApplicationUser
        {
            FullName = model.FullName.Trim(), Email = model.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
            Role = UserRole.Client, CreatedAt = DateTime.UtcNow,
            IsActive = true, EmailConfirmed = false,
            EmailConfirmationToken = HashToken(rawToken), AssignedCases = []
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        await QueueConfirmationEmailAsync(user.Email, rawToken, ct);

        TempData["Success"] = "Registration successful. Check your email to confirm your account.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return InvalidToken("Invalid confirmation link.");
        var tokenHash = HashToken(token);
        var user = await db.Users.SingleOrDefaultAsync(x => x.EmailConfirmationToken == tokenHash, ct);
        if (user is null) return InvalidToken("Invalid or already-used confirmation link.");

        user.EmailConfirmed = true;
        user.EmailConfirmationToken = null;
        await db.SaveChangesAsync(ct);
        TempData["Success"] = "Email confirmed. You can now sign in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult ForgotPassword() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(string emailAddress, CancellationToken ct = default)
    {
        var normalizedEmail = NormalizeEmail(emailAddress);
        var user = normalizedEmail.Length == 0
            ? null
            : await db.Users.SingleOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail, ct);

        if (user is { IsActive: true, EmailConfirmed: true })
        {
            var rawToken = GenerateToken();
            user.PasswordResetToken = HashToken(rawToken);
            user.PasswordResetTokenExpiry = DateTime.UtcNow.Add(ResetLifetime);
            await db.SaveChangesAsync(ct);
            await QueuePasswordResetEmailAsync(user, rawToken, ct);
        }

        TempData["Success"] = "If an eligible account exists, a password-reset email has been sent.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> ResetPassword(string token, CancellationToken ct = default)
    {
        if (!await IsValidResetTokenAsync(token, ct)) return InvalidToken("Invalid or expired password-reset link.");
        ViewBag.Token = token;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> ResetPassword(string token, string password, string confirmPassword, CancellationToken ct = default)
    {
        ViewBag.Token = token;
        if (password != confirmPassword) return ResetError("Passwords do not match.");
        if (!IsStrongPassword(password))
            return ResetError("Use at least 12 characters with uppercase, lowercase, a number, and a symbol.");

        var tokenHash = HashToken(token);
        var user = await db.Users.SingleOrDefaultAsync(x => x.PasswordResetToken == tokenHash, ct);
        if (user is null || user.PasswordResetTokenExpiry is null || user.PasswordResetTokenExpiry <= DateTime.UtcNow)
            return InvalidToken("Invalid or expired password-reset link.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        user.RememberMeToken = null;
        await db.SaveChangesAsync(ct);

        Response.Cookies.Delete(RememberCookie);
        TempData["Success"] = "Your password has been changed. Sign in with the new password.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken ct = default)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId.HasValue)
        {
            var user = await db.Users.FindAsync([userId.Value], ct);
            if (user is not null) user.RememberMeToken = null;
            await db.SaveChangesAsync(ct);
        }
        HttpContext.Session.Clear();
        Response.Cookies.Delete(RememberCookie);
        TempData["Success"] = "Signed out successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Privacy() => View();

    [HttpGet]
    public IActionResult AccessDenied() => View();

    [HttpGet, ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

    private async Task<bool> IsValidResetTokenAsync(string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;
        var tokenHash = HashToken(token);
        return await db.Users.AnyAsync(x => x.PasswordResetToken == tokenHash &&
            x.PasswordResetTokenExpiry != null && x.PasswordResetTokenExpiry > DateTime.UtcNow, ct);
    }

    private async Task QueueConfirmationEmailAsync(string emailAddress, string rawToken, CancellationToken ct)
    {
        var link = BuildPublicUrl(nameof(ConfirmEmail), rawToken);
        await email.QueueAsync(emailAddress, "Confirm your Simplex account",
            $"<p>Welcome to Simplex.</p><p><a href=\"{WebUtility.HtmlEncode(link)}\">Confirm your email address</a></p>",
            $"Welcome to Simplex. Confirm your email address: {link}",
            $"account-confirmation:{HashToken(rawToken)}", ct);
        await db.SaveChangesAsync(ct);
    }

    private async Task QueuePasswordResetEmailAsync(ApplicationUser user, string rawToken, CancellationToken ct)
    {
        var link = BuildPublicUrl(nameof(ResetPassword), rawToken);
        await email.QueueAsync(user.Email, "Reset your Simplex password",
            $"<p>Hello {WebUtility.HtmlEncode(user.FullName)},</p><p>Use this secure, one-time link to choose a new password:</p><p><a href=\"{WebUtility.HtmlEncode(link)}\">Reset password</a></p><p>The link expires in one hour. If you did not request it, ignore this message.</p>",
            $"Hello {user.FullName}, reset your password using this one-time link: {link}\nThe link expires in one hour.",
            $"password-reset:{HashToken(rawToken)}", ct);
        await db.SaveChangesAsync(ct);
    }

    private string BuildPublicUrl(string action, string token)
    {
        var configuredBase = configuration["Email:PublicBaseUrl"]?.TrimEnd('/');
        var baseUrl = string.IsNullOrWhiteSpace(configuredBase) ? $"{Request.Scheme}://{Request.Host}" : configuredBase;
        return $"{baseUrl}/Home/{action}?token={Uri.EscapeDataString(token)}";
    }

    private void SetSession(ApplicationUser user)
    {
        HttpContext.Session.SetString("UserRole", user.Role == UserRole.Director ? "Admin" : user.Role.ToString());
        HttpContext.Session.SetInt32("UserId", user.Id);
        HttpContext.Session.SetString("UserFullName", user.FullName);
        HttpContext.Session.SetString("UserEmail", user.Email);
    }

    private IActionResult RedirectToDashboard(string role) => role switch
    {
        nameof(UserRole.Director) => RedirectToAction("Admin", "Dashboard"),
        nameof(UserRole.Lawyer) => RedirectToAction("Lawyer", "Dashboard"),
        nameof(UserRole.Paralegal) => RedirectToAction("Paralegal", "Dashboard"),
        nameof(UserRole.Accountant) => RedirectToAction("Accountant", "Dashboard"),
        nameof(UserRole.Client) => RedirectToAction("Client", "Dashboard"),
        _ => RedirectToAction(nameof(Index))
    };

    private IActionResult RedirectToLocalOrDashboard(string? returnUrl, string role) =>
        !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToDashboard(role);

    private IActionResult LoginError(string message, string? returnUrl = null) { TempData["Error"] = message; ViewBag.ReturnUrl = returnUrl; return View(nameof(Login)); }
    private IActionResult ResetError(string message) { TempData["Error"] = message; return View(nameof(ResetPassword)); }
    private IActionResult InvalidToken(string message) { TempData["Error"] = message; return RedirectToAction(nameof(Login)); }

    private static string NormalizeEmail(string? value) => value?.Trim().ToLowerInvariant() ?? string.Empty;
    private static string GenerateToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    internal static string HashToken(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    internal static bool IsStrongPassword(string? value) => value is { Length: >= 12 } && value.Any(char.IsUpper) &&
        value.Any(char.IsLower) && value.Any(char.IsDigit) && value.Any(x => !char.IsLetterOrDigit(x));
}
