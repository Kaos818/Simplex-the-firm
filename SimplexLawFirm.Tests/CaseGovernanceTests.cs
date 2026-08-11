using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services;
using SimplexLawFirm.Services.Notifications;
using SimplexLawFirm.ViewModels;
using Xunit;

namespace SimplexLawFirm.Tests;

public sealed class CaseGovernanceTests
{
    [Fact]
    public async Task Cost_approaching_value_routes_strategy_to_director()
    {
        await using var f = await Fixture.CreateAsync(3_000);
        var decision = await f.Service.SelectStrategyAsync(f.Attorney.Id, Input(f.Matter.Id, LitigationStrategyType.Trial));
        Assert.Equal(StrategyDecisionStatus.PendingDirectorAuthorisation, decision.Status);
        Assert.True(decision.CostAuthorisationRequired);
        Assert.False(f.Matter.IsCourtReady);
    }

    [Fact]
    public async Task Poor_prospects_trial_requires_written_justification_and_director()
    {
        await using var f = await Fixture.CreateAsync(100_000, .25m);
        var input = Input(f.Matter.Id, LitigationStrategyType.Trial);
        await Assert.ThrowsAsync<ArgumentException>(() => f.Service.SelectStrategyAsync(f.Attorney.Id, input));
        input.LowProspectsJustification = "Trial remains proportionate because decisive expert evidence is expected shortly.";
        var decision = await f.Service.SelectStrategyAsync(f.Attorney.Id, input);
        Assert.True(decision.ProspectsAuthorisationRequired);
        Assert.Equal(StrategyDecisionStatus.PendingDirectorAuthorisation, decision.Status);
    }

    [Fact]
    public async Task Court_ready_requires_authorised_strategy_and_every_mandatory_document()
    {
        await using var f = await Fixture.CreateAsync(100_000);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service.MarkCourtReadyAsync(f.Attorney.Id, f.Matter.Id));
        var decision = await f.Service.SelectStrategyAsync(f.Attorney.Id, Input(f.Matter.Id, LitigationStrategyType.Negotiate));
        Assert.Equal(StrategyDecisionStatus.Authorised, decision.Status);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service.MarkCourtReadyAsync(f.Attorney.Id, f.Matter.Id));
        Assert.Contains("Client mandate", error.Message);
        f.Db.Documents.Add(new Document { CaseId = f.Matter.Id, RequirementCode = "CLIENT_MANDATE", FileName = "mandate.pdf", FilePath = "/x", FileSize = "1 KB", FileType = "application/pdf", Description = "Signed mandate", UploadedAt = DateTime.UtcNow });
        await f.Db.SaveChangesAsync();
        await f.Service.MarkCourtReadyAsync(f.Attorney.Id, f.Matter.Id);
        Assert.True(f.Matter.IsCourtReady);
    }

    [Fact]
    public async Task External_one_time_portal_document_satisfies_readiness_requirement()
    {
        await using var f = await Fixture.CreateAsync(100_000);
        await f.Service.SelectStrategyAsync(f.Attorney.Id, Input(f.Matter.Id, LitigationStrategyType.Settle));
        var request = new ExternalEvidenceRequest { CaseId = f.Matter.Id, RecipientName = "Opponent", RecipientEmail = "opponent@test", TokenHash = "HASH", RequestedByUserId = f.Attorney.Id, ExpiresAtUtc = DateTime.UtcNow.AddDays(1), AccessedAtUtc = DateTime.UtcNow, ClosedAtUtc = DateTime.UtcNow };
        f.Db.Add(request); await f.Db.SaveChangesAsync();
        f.Db.ExternalEvidenceDocuments.Add(new() { RequestId = request.Id, OriginalFileName = "mandate.pdf", Purpose = "Signed authority supplied by the opposing side", RequirementCode = "CLIENT_MANDATE", RelativePath = "1/x", ContentType = "application/pdf", SizeBytes = 10, Sha256Hash = "ABC" });
        await f.Db.SaveChangesAsync();
        var report = await f.Service.ReviewReadinessAsync(f.Matter.Id, f.Attorney.Id);
        Assert.True(report.Items.Single().IsHeld);
        await f.Service.MarkCourtReadyAsync(f.Attorney.Id, f.Matter.Id);
        Assert.True(f.Matter.IsCourtReady);
    }

    [Fact]
    public async Task Director_waiver_allows_unobtainable_mandatory_document_with_audit_reason()
    {
        await using var f = await Fixture.CreateAsync(100_000);
        await f.Service.SelectStrategyAsync(f.Attorney.Id, Input(f.Matter.Id, LitigationStrategyType.Mediate));
        var requirement = await f.Db.CaseDocumentRequirements.SingleAsync();
        var waiver = await f.Service.RequestWaiverAsync(f.Attorney.Id, f.Matter.Id, requirement.Id, "The issuing authority confirmed the original record was destroyed.");
        await f.Service.DecideWaiverAsync(f.Director.Id, waiver.Id, true, "Independent confirmation is sufficient to proceed exceptionally.");
        await f.Service.MarkCourtReadyAsync(f.Attorney.Id, f.Matter.Id);
        Assert.True(f.Matter.IsCourtReady);
        Assert.Contains(await f.Db.AuditEntries.ToListAsync(), x => x.Action == "Mandatory document waiver approved");
    }

    [Fact]
    public async Task A_second_waiver_request_for_the_same_missing_document_is_refused_while_pending()
    {
        await using var f = await Fixture.CreateAsync(100_000);
        var requirement = await f.Db.CaseDocumentRequirements.SingleAsync();
        await f.Service.RequestWaiverAsync(f.Attorney.Id, f.Matter.Id, requirement.Id, "The original document cannot be obtained from the issuing body.");

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            f.Service.RequestWaiverAsync(f.Attorney.Id, f.Matter.Id, requirement.Id, "Submitting the same request again."));

        Assert.Contains("already pending", error.Message);
        Assert.Single(await f.Db.CaseDocumentWaivers.ToListAsync());
    }

    [Fact]
    public async Task Material_cost_change_reopens_strategy_and_removes_court_ready_state()
    {
        await using var f = await Fixture.CreateAsync(100_000);
        await f.Service.SelectStrategyAsync(f.Attorney.Id, Input(f.Matter.Id, LitigationStrategyType.Negotiate));
        f.Db.Documents.Add(new Document { CaseId = f.Matter.Id, RequirementCode = "CLIENT_MANDATE", FileName = "mandate.pdf", FilePath = "/x", FileSize = "1 KB", FileType = "application/pdf", Description = "Signed mandate", UploadedAt = DateTime.UtcNow });
        await f.Db.SaveChangesAsync(); await f.Service.MarkCourtReadyAsync(f.Attorney.Id, f.Matter.Id);
        f.Db.TimeEntries.Add(new TimeEntry { CaseId = f.Matter.Id, LawyerId = f.Attorney.Id, Description = "Unexpected hearing", Date = DateTime.UtcNow, Hours = 10, HourlyRate = 1_000, TotalAmount = 10_000, IsBillable = true }); await f.Db.SaveChangesAsync();
        Assert.True(await f.Service.RetestStrategyAsync(f.Matter.Id));
        Assert.True(f.Matter.StrategyReviewRequired); Assert.False(f.Matter.IsCourtReady);
    }

    [Fact]
    public async Task Final_court_reminder_escalates_missing_mandatory_documents_to_director()
    {
        await using var f = await Fixture.CreateAsync(100_000);
        f.Db.CalendarEvents.Add(new CalendarEvent { Title = "Trial", Description = "", Location = "Court", StartDateTime = DateTime.UtcNow.AddHours(12), EndDateTime = DateTime.UtcNow.AddHours(14), Type = EventType.Hearing, Status = EventStatus.Scheduled, CaseId = f.Matter.Id, Color = "", MeetingLink = "", CompletionNotes = "", RecurrenceRule = "", Attendees = [], Reminders = [], ChildEvents = [] });
        await f.Db.SaveChangesAsync();
        Assert.True(await f.Service.RunGovernanceAsync(DateTime.UtcNow) > 0);
        Assert.Contains(f.Notifications.Items, x => x.UserId == f.Director.Id && x.Type == "FinalDocumentReadinessEscalation");
    }

    [Fact]
    public async Task Readiness_dashboard_scopes_to_the_requesting_attorney_but_shows_everything_to_the_director()
    {
        await using var f = await Fixture.CreateAsync(100_000);
        var otherAttorney = new ApplicationUser { FullName = "Other Attorney", Email = "other@test", PasswordHash = "x", Role = UserRole.Lawyer, IsActive = true };
        f.Db.Add(otherAttorney); await f.Db.SaveChangesAsync();
        var otherClient = new Client { FirstName = "Other", LastName = "Client", Email = "other-client@test", Phone = "2" };
        f.Db.Add(otherClient); await f.Db.SaveChangesAsync();
        f.Db.Cases.Add(new Case { CaseNumber = "GOV-002", Title = "Second matter", CaseType = "Commercial", MatterValue = 50_000, ClientId = otherClient.Id, LawyerId = otherAttorney.Id, Status = CaseStatus.Active });
        await f.Db.SaveChangesAsync();

        var attorneyView = await f.Service.ReadinessDashboardAsync(f.Attorney.Id);
        Assert.Single(attorneyView);
        Assert.Equal(f.Matter.Id, attorneyView[0].Case.Id);
        Assert.Equal(ReadinessDashboardStatus.Blocked, attorneyView[0].Status);

        var directorView = await f.Service.ReadinessDashboardAsync(null);
        Assert.Equal(2, directorView.Count);
    }

    [Fact]
    public async Task Readiness_dashboard_reports_no_court_date_when_none_is_scheduled_rather_than_a_default_date()
    {
        await using var f = await Fixture.CreateAsync(100_000);
        var rows = await f.Service.ReadinessDashboardAsync(null);
        Assert.Null(rows.Single().NextCourtDate);
    }

    [Fact]
    public async Task Opening_the_readiness_page_records_a_review_that_history_can_list()
    {
        await using var f = await Fixture.CreateAsync(100_000);
        await f.Service.ReviewReadinessAsync(f.Matter.Id, f.Attorney.Id);
        await f.Service.ReviewReadinessAsync(f.Matter.Id, f.Attorney.Id);
        var history = await f.Service.ReadinessHistoryAsync(f.Matter.Id, f.Attorney.Id);
        Assert.Equal(2, history.Count);
        Assert.True(history[0].ReviewedAtUtc >= history[1].ReviewedAtUtc);
    }

    [Fact]
    public async Task Readiness_history_refuses_a_lawyer_who_is_not_assigned_to_the_matter()
    {
        await using var f = await Fixture.CreateAsync(100_000);
        var otherAttorney = new ApplicationUser { FullName = "Other Attorney", Email = "other2@test", PasswordHash = "x", Role = UserRole.Lawyer, IsActive = true };
        f.Db.Add(otherAttorney); await f.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Service.ReadinessHistoryAsync(f.Matter.Id, otherAttorney.Id));
    }

    private static SelectLitigationStrategyViewModel Input(int caseId, LitigationStrategyType strategy) => new() { CaseId = caseId, Strategy = strategy, Reasoning = "This strategy is proportionate to the client's objectives and available evidence." };

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        public ApplicationDbContext Db { get; } public ICaseGovernanceService Service { get; } public Notifications Notifications { get; } = new();
        public ApplicationUser Attorney { get; private set; } = null!; public ApplicationUser Director { get; private set; } = null!; public Case Matter { get; private set; } = null!;
        private Fixture(SqliteConnection connection, ApplicationDbContext db) { this.connection = connection; Db = db; Service = new CaseGovernanceService(db, Notifications); }
        public static async Task<Fixture> CreateAsync(decimal value, decimal prospects = .70m)
        {
            var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync(); var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options); await db.Database.EnsureCreatedAsync();
            var f = new Fixture(connection, db); f.Attorney = new() { FullName = "Attorney", Email = "attorney@test", PasswordHash = "x", Role = UserRole.Lawyer, IsActive = true }; f.Director = new() { FullName = "Director", Email = "director@test", PasswordHash = "x", Role = UserRole.Admin, IsActive = true }; var client = new Client { FirstName = "Client", LastName = "One", Email = "client@test", Phone = "1" }; db.AddRange(f.Attorney, f.Director, client); await db.SaveChangesAsync();
            f.Matter = new() { CaseNumber = "GOV-001", Title = "Governed matter", CaseType = "Commercial", MatterValue = value, EvidenceStrength = prospects, ClientId = client.Id, LawyerId = f.Attorney.Id, Status = CaseStatus.Active }; db.Add(f.Matter); db.CaseDocumentRequirements.Add(new() { CaseType = "General", Code = "CLIENT_MANDATE", Name = "Client mandate", Description = "Signed authority", Category = DocumentCategory.Contracts, Importance = DocumentRequirementImportance.Mandatory, DisplayOrder = 1 }); await db.SaveChangesAsync(); return f;
        }
        public async ValueTask DisposeAsync() { await Db.DisposeAsync(); await connection.DisposeAsync(); }
    }
    public sealed class Notifications : INotificationService { public List<(int UserId,string Type)> Items { get; }=[]; public Task QueueAsync(int userId, string type, string title, string message, string? actionUrl, string? deduplicationKey, CancellationToken cancellationToken = default) { Items.Add((userId,type)); return Task.CompletedTask; } }
}
