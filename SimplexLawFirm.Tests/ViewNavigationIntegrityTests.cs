using System.Text.RegularExpressions;
using SimplexLawFirm.Controllers;
using Xunit;

namespace SimplexLawFirm.Tests;

public sealed class ViewNavigationIntegrityTests
{
    private static readonly string ViewsRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Views"));

    [Fact]
    public void Calendar_assets_are_bundled_and_do_not_depend_on_a_public_cdn()
    {
        var root = Path.GetFullPath(Path.Combine(ViewsRoot, ".."));
        var calendarView = File.ReadAllText(Path.Combine(ViewsRoot, "Calendar", "Index.cshtml"));
        var adminLayout = File.ReadAllText(Path.Combine(ViewsRoot, "Shared", "_AdminLayout.cshtml"));
        Assert.Contains("~/lib/fullcalendar/main.min.css", calendarView);
        Assert.Contains("~/lib/fullcalendar/main.min.js", adminLayout);
        Assert.DoesNotContain("cdn.jsdelivr.net/npm/fullcalendar", calendarView, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cdn.jsdelivr.net/npm/fullcalendar", adminLayout, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("AssignedToUserId: @Html.Raw(", calendarView);
        Assert.DoesNotContain("<script src=\"~/lib/fullcalendar/main.min.js\"", calendarView, StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(Path.Combine(root, "wwwroot", "lib", "fullcalendar", "main.min.js")));
        Assert.True(File.Exists(Path.Combine(root, "wwwroot", "lib", "fullcalendar", "main.min.css")));
    }

    [Fact]
    public void Receiving_attorney_handover_view_exposes_governed_response_actions()
    {
        var source = File.ReadAllText(Path.Combine(ViewsRoot, "Practice", "Handover.cshtml"));
        Assert.Contains("/Practice/AcceptHandover", source);
        Assert.Contains("name=\"AcknowledgedItemIds\"", source);
        Assert.Contains("name=\"RiskAcknowledged\"", source);
        Assert.Contains("name=\"SignatureName\"", source);
        Assert.Contains("/Practice/RaiseHandoverQuery/", source);
        Assert.Contains("/Practice/DeclineHandover", source);
        Assert.Contains("isReceiving && Model.Handover.Status == HandoverStatus.Ready", source);
        Assert.Contains("ToString().ToLowerInvariant()", source);

        var receivingQuery = File.ReadAllText(Path.Combine(ViewsRoot, "Practice", "RaiseHandoverQuery.cshtml"));
        Assert.Contains("/Practice/QueryHandover", receivingQuery);
        Assert.Contains("Director's reason", receivingQuery);
        Assert.Contains("name=\"query\"", receivingQuery);

        var directorResponse = File.ReadAllText(Path.Combine(ViewsRoot, "Practice", "ReviewHandoverQuery.cshtml"));
        Assert.Contains("/Practice/RespondToHandoverQuery", directorResponse);
        Assert.Contains("name=\"response\"", directorResponse);
        Assert.Contains("Your original reason", directorResponse);
    }

    [Fact]
    public void Final_director_verdict_and_event_reimbursement_controls_are_visible()
    {
        var handover = File.ReadAllText(Path.Combine(ViewsRoot, "Practice", "Handover.cshtml"));
        Assert.Contains("DirectorQueryVerdictFinal", handover);
        Assert.Contains("No further query may be raised", handover);
        var reimbursement = File.ReadAllText(Path.Combine(ViewsRoot, "Reimbursement", "Review.cshtml"));
        Assert.Contains("name=\"ApprovedAmount\"", reimbursement);
        Assert.Contains("Record final verdict", reimbursement);
    }

    [Fact]
    public void Views_do_not_contain_known_dead_or_placeholder_navigation()
    {
        var forbidden = new[]
        {
            "/Invoice/Details/", "/Billing/EditTimeEntry/", "/Calendar/EditEvent/", "/Calendar/EditTask/",
            "/Payment/Index", "/Case/Notes", "href=\"/Profile\"", "href=\"/Settings\"",
            "href=\"#\"", "href=\"javascript:"
        };
        foreach (var file in Directory.EnumerateFiles(ViewsRoot, "*.cshtml", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(file);
            foreach (var value in forbidden)
                Assert.DoesNotContain(value, source, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Shared_navigation_aliases_have_real_controller_actions()
    {
        AssertAction<RetainerController>("PendingRequests");
        AssertAction<RetainerController>("MyRequests");
        AssertAction<RetainerController>("PaymentHistory");
        AssertAction<BillingController>("MyTimeEntries");
        AssertAction<BillingController>("LawyerTimeEntries");
        AssertAction<CalendarController>("ClientIndex");
        AssertAction<CalendarController>("LawyerIndex");
        AssertAction<DocumentController>("LawyerIndex");
        AssertAction<RetainerController>("RequestService");
    }

    private static void AssertAction<TController>(string name) =>
        Assert.Contains(typeof(TController).GetMethods(), method => method.Name == name && method.IsPublic);
}
