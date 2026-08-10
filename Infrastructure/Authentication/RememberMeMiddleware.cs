using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;

namespace SimplexLawFirm.Infrastructure.Authentication;

public sealed class RememberMeMiddleware(RequestDelegate next)
{
    private const string CookieName = "Simplex.RememberMe";

    public async Task InvokeAsync(HttpContext context, ApplicationDbContext db)
    {
        if (!context.Session.GetInt32("UserId").HasValue &&
            context.Request.Cookies.TryGetValue(CookieName, out var rawToken) &&
            !string.IsNullOrWhiteSpace(rawToken))
        {
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
            var user = await db.Users.SingleOrDefaultAsync(x => x.RememberMeToken == hash && x.IsActive && x.EmailConfirmed);
            if (user is not null)
            {
                context.Session.SetInt32("UserId", user.Id);
                context.Session.SetString("UserRole", user.Role.ToString());
                context.Session.SetString("UserFullName", user.FullName);
                context.Session.SetString("UserEmail", user.Email);
            }
            else context.Response.Cookies.Delete(CookieName);
        }
        await next(context);
    }
}
