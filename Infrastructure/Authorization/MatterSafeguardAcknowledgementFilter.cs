using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services;

namespace SimplexLawFirm.Infrastructure.Authorization;

public sealed class MatterSafeguardAcknowledgementFilter(
    ApplicationDbContext db,
    IVulnerableClientService safeguards) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var session = context.HttpContext.Session;
        var staffId = session.GetInt32("UserId");
        var role = session.GetString("UserRole");
        var controller = context.RouteData.Values["controller"]?.ToString() ?? "";
        var action = context.RouteData.Values["action"]?.ToString() ?? "";
        if (!staffId.HasValue || role is not ("Admin" or "Lawyer" or "Paralegal" or "Accountant") ||
            controller == "VulnerableClient")
        {
            await next(); return;
        }
        var caseId = await ResolveCaseIdAsync(controller, action, context.ActionArguments, context.HttpContext.RequestAborted);
        if (!caseId.HasValue || (await safeguards.UnacknowledgedAsync(caseId.Value, staffId.Value, context.HttpContext.RequestAborted)).Count == 0)
        {
            await next(); return;
        }
        var acknowledgementUrl = $"/VulnerableClient/Acknowledge?caseId={caseId.Value}";
        if (context.HttpContext.Request.Headers.Accept.Any(x => x?.Contains("application/json") == true) ||
            context.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            context.Result = new ObjectResult(new { error = "Client safeguards must be acknowledged before continuing.", acknowledgementUrl })
                { StatusCode = StatusCodes.Status409Conflict };
        else context.Result = new RedirectResult(acknowledgementUrl);
    }

    private async Task<int?> ResolveCaseIdAsync(string controller, string action, IDictionary<string, object?> args, CancellationToken ct)
    {
        if (TryInt(args, "caseId", out var explicitId)) return explicitId;
        if (args.Values.OfType<CaseNote>().FirstOrDefault() is { } note) return note.CaseId;
        if (args.Values.OfType<Case>().FirstOrDefault() is { Id: > 0 } matter) return matter.Id;
        if (args.Values.OfType<Document>().FirstOrDefault() is { CaseId: not null } documentInput) return documentInput.CaseId;
        if (args.Values.OfType<TimeEntry>().FirstOrDefault() is { CaseId: > 0 } timeInput) return timeInput.CaseId;
        if (!TryInt(args, "id", out var id) && !TryInt(args, "documentId", out id) &&
            !TryInt(args, "eventId", out id) && !TryInt(args, "retainerId", out id)) return null;
        return controller switch
        {
            "Case" => id,
            "Document" => await db.Documents.Where(x => x.Id == id).Select(x => x.CaseId).SingleOrDefaultAsync(ct),
            "Calendar" when action.StartsWith("Task", StringComparison.OrdinalIgnoreCase) ||
                            action.Contains("Task", StringComparison.OrdinalIgnoreCase) =>
                await db.Tasks.Where(x => x.Id == id).Select(x => x.CaseId).SingleOrDefaultAsync(ct),
            "Calendar" => await db.CalendarEvents.Where(x => x.Id == id).Select(x => x.CaseId).SingleOrDefaultAsync(ct),
            "Retainer" => await db.Retainers.Where(x => x.Id == id).Select(x => x.CaseId).SingleOrDefaultAsync(ct),
            "Billing" when action.Contains("Time", StringComparison.OrdinalIgnoreCase) =>
                await db.TimeEntries.Where(x => x.Id == id).Select(x => (int?)x.CaseId).SingleOrDefaultAsync(ct),
            "Billing" => await db.Invoices.Where(x => x.Id == id).Select(x => x.CaseId).SingleOrDefaultAsync(ct),
            "Practice" when action is "Forecast" or "Reassign" or "Correspondence" => id,
            "Practice" when action.Contains("Handover", StringComparison.OrdinalIgnoreCase) =>
                await db.CaseHandovers.Where(x => x.Id == id).Select(x => (int?)x.CaseId).SingleOrDefaultAsync(ct),
            _ => null
        };
    }

    private static bool TryInt(IDictionary<string, object?> values, string name, out int value)
    {
        if (values.TryGetValue(name, out var raw) && raw is int number && number > 0) { value = number; return true; }
        value = 0; return false;
    }
}
