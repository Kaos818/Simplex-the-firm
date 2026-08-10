using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services;
using SimplexLawFirm.Services.CurrentUser;

namespace SimplexLawFirm.Infrastructure.Authorization;

public sealed class SupportPersonEnforcementMiddleware(RequestDelegate next)
{
    private static readonly string[] ExemptPaths =
        ["/Home/Logout", "/VulnerableClient/SupportRequired", "/Notification/"];

    public async Task InvokeAsync(HttpContext context, ICurrentClientService currentClient,
        IVulnerableClientService safeguards, ApplicationDbContext db)
    {
        if (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method) ||
            HttpMethods.IsOptions(context.Request.Method) ||
            context.Session.GetString("UserRole") != "Client" ||
            ExemptPaths.Any(x => context.Request.Path.StartsWithSegments(x, StringComparison.OrdinalIgnoreCase)))
        {
            await next(context); return;
        }
        var client = await currentClient.GetAsync(context.RequestAborted);
        if (client == null) { await next(context); return; }
        var required = await db.VulnerableClientFlags.AnyAsync(x => x.ClientId == client.Id &&
            x.Safeguard == ClientSafeguard.SupportPerson &&
            (x.Status == VulnerableFlagStatus.PendingReview || x.Status == VulnerableFlagStatus.Confirmed ||
             x.Status == VulnerableFlagStatus.Escalated), context.RequestAborted);
        if (!required || await safeguards.HasActiveSupportSessionAsync(client.Id, context.RequestAborted))
        {
            await next(context); return;
        }
        if (context.Request.Headers.Accept.Any(x => x?.Contains("application/json") == true) ||
            context.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "A recorded support person must be present before this self-service action can continue." });
            return;
        }
        context.Response.Redirect("/VulnerableClient/SupportRequired");
    }
}
