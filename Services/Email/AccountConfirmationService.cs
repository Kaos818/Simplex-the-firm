using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;

namespace SimplexLawFirm.Services.Email;

public interface IAccountConfirmationService
{
    Task IssueAsync(ApplicationUser user, CancellationToken ct = default);
}

/// <summary>Issues one-time account-confirmation links for staff and client accounts created by the firm.</summary>
public sealed class AccountConfirmationService(ApplicationDbContext db, IEmailService email, IConfiguration configuration) : IAccountConfirmationService
{
    public async Task IssueAsync(ApplicationUser user, CancellationToken ct = default)
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        user.EmailConfirmed = false;
        user.EmailConfirmationToken = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
        var configuredBase = configuration["Email:PublicBaseUrl"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(configuredBase))
            throw new InvalidOperationException("Email:PublicBaseUrl is required to issue account-confirmation links.");
        var link = $"{configuredBase}/Home/ConfirmEmail?token={Uri.EscapeDataString(rawToken)}";
        await email.QueueAsync(user.Email, "Confirm your Simplex account",
            $"<p>Hello {System.Net.WebUtility.HtmlEncode(user.FullName)},</p><p><a href=\"{System.Net.WebUtility.HtmlEncode(link)}\">Confirm your email address</a></p>",
            $"Hello {user.FullName}, confirm your email address: {link}",
            $"account-confirmation:{user.Id}:{user.EmailConfirmationToken}", ct);
        await db.SaveChangesAsync(ct);
    }
}
