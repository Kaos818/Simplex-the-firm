using System.Text.RegularExpressions;
using SimplexLawFirm.Controllers;
using Xunit;

namespace SimplexLawFirm.Tests;

public sealed class ViewNavigationIntegrityTests
{
    private static readonly string ViewsRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Views"));

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
