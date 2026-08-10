using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services;
using SimplexLawFirm.Services.Notifications;
using SimplexLawFirm.ViewModels;
using Xunit;

namespace SimplexLawFirm.Tests;

public class PracticeIntelligenceTests
{
    [Fact]
    public async Task Forecast_refuses_when_comparable_sample_is_too_small()
    {
        await using var fixture = await Fixture.CreateAsync();
        var (matter, lawyer, _) = await fixture.SeedMatterAsync();
        var result = await fixture.Service.CreateForecastAsync(matter.Id, lawyer.Id);
        Assert.Equal(ForecastStatus.Refused, result.Status);
        Assert.Contains("3 comparable", result.RefusalReason);
    }

    [Fact]
    public async Task Forecast_is_locked_and_scores_against_closed_outcome()
    {
        await using var fixture = await Fixture.CreateAsync();
        var (matter, lawyer, client) = await fixture.SeedMatterAsync();
        for (var i = 0; i < 3; i++)
            fixture.Db.Cases.Add(new Case { CaseNumber = $"H-{i}", Title = $"Historic {i}", ClientId = client.Id, LawyerId = lawyer.Id, CaseType = matter.CaseType, Status = CaseStatus.Closed, RecordedOutcome = i == 0 ? ForecastResult.Unsuccessful : ForecastResult.Successful });
        fixture.Db.ClientForecastRequests.Add(new ClientForecastRequest { CaseId = matter.Id, ClientId = client.Id, ClientMessage = "Please assess prospects" });
        await fixture.Db.SaveChangesAsync();

        var forecast = await fixture.Service.CreateForecastAsync(matter.Id, lawyer.Id);
        var original = forecast.Probability;
        Assert.Equal(ForecastStatus.Draft, forecast.Status);
        Assert.Null(forecast.LockedAtUtc);
        Assert.Equal(ForecastRequestStatus.Pending, (await fixture.Db.ClientForecastRequests.SingleAsync()).Status);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.ScoreForecastAsync(matter.Id, ForecastResult.Successful));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => fixture.Service.LockForecastAsync(forecast.Id, 101, true, "Invalid percentage"));
        await fixture.Service.LockForecastAsync(forecast.Id, 70, true, "Professional view");
        Assert.Equal(ForecastRequestStatus.Fulfilled, (await fixture.Db.ClientForecastRequests.SingleAsync()).Status);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.LockForecastAsync(forecast.Id, 10, false, "Rewrite"));
        forecast.Probability = .01m;
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Db.SaveChangesAsync());
        fixture.Db.Entry(forecast).Property(x => x.Probability).CurrentValue = original;
        fixture.Db.Entry(forecast).Property(x => x.Probability).IsModified = false;
        await fixture.Service.ScoreForecastAsync(matter.Id, ForecastResult.Successful);

        var scored = await fixture.Db.CaseForecasts.AsNoTracking().SingleAsync(x => x.Id == forecast.Id);
        Assert.Equal(ForecastStatus.Scored, scored.Status);
        Assert.Equal(original, scored.Probability);
        Assert.InRange(scored.AccuracyScore!.Value, 0, 1);
        Assert.Equal(2, await fixture.Db.ForecastCalibrations.CountAsync());
        Assert.Equal(ForecastRequestStatus.Fulfilled, (await fixture.Db.ClientForecastRequests.SingleAsync()).Status);
    }

    [Fact]
    public async Task Approved_reassignment_creates_live_handover_and_transfers_nothing_before_acceptance()
    {
        await using var fixture = await Fixture.CreateAsync();
        var (matter, outgoing, _) = await fixture.SeedMatterAsync();
        var receiver = new ApplicationUser { FullName = "Attorney Two", Email = "two@test", PasswordHash = "x", Role = UserRole.Lawyer, IsActive = true };
        var director = new ApplicationUser { FullName = "Director", Email = "director@test", PasswordHash = "x", Role = UserRole.Admin, IsActive = true };
        fixture.Db.AddRange(receiver, director);
        await fixture.Db.SaveChangesAsync();

        var handover = await fixture.Service.ApproveReassignmentAsync(matter.Id, receiver.Id, director.Id, "Workload reallocation", default);

        Assert.Equal(outgoing.Id, matter.LawyerId);
        Assert.Equal(5, handover.Items.Count);
        Assert.Equal(ReassignmentStatus.HandoverPreparing, (await fixture.Db.CaseReassignments.SingleAsync()).Status);
        Assert.True(await fixture.Service.MarkHandoverReadyAsync(handover.Id));
    }

    [Fact]
    public async Task Live_mandatory_blockers_prevent_readiness_until_the_outgoing_attorney_records_positions()
    {
        await using var fixture = await Fixture.CreateAsync();
        var (matter, outgoing, _) = await fixture.SeedMatterAsync();
        var receiver = new ApplicationUser { FullName = "Attorney Two", Email = "two@test", PasswordHash = "x", Role = UserRole.Lawyer, IsActive = true };
        var director = new ApplicationUser { FullName = "Director", Email = "director@test", PasswordHash = "x", Role = UserRole.Admin, IsActive = true };
        fixture.Db.AddRange(receiver, director, new CalendarEvent { Title = "Court deadline", Description = "File answering papers", Location = "Court", CaseId = matter.Id, Type = EventType.Deadline, Status = EventStatus.Scheduled, StartDateTime = DateTime.UtcNow.AddDays(2), EndDateTime = DateTime.UtcNow.AddDays(2).AddHours(1) });
        await fixture.Db.SaveChangesAsync();

        var handover = await fixture.Service.ApproveReassignmentAsync(matter.Id, receiver.Id, director.Id, "Workload reallocation");
        Assert.False(await fixture.Service.MarkHandoverReadyAsync(handover.Id));
        Assert.Equal(HandoverStatus.Preparing, handover.Status);
        Assert.Contains(handover.Items, x => x.Type == "Deadlines" && x.IsMandatory && !x.IsResolved);

        foreach (var item in handover.Items.Where(x => x.IsMandatory)) { item.IsResolved = true; item.ResolutionNote = "Position recorded for the receiving attorney."; }
        await fixture.Db.SaveChangesAsync();
        Assert.True(await fixture.Service.MarkHandoverReadyAsync(handover.Id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.MarkHandoverReadyAsync(handover.Id));
    }

    [Fact]
    public async Task Complaint_naming_director_routes_to_alternate_and_restricts_named_director()
    {
        await using var fixture = await Fixture.CreateAsync();
        var (matter, _, client) = await fixture.SeedMatterAsync();
        var named = new ApplicationUser { FullName = "Director Named", Email = "named@test", PasswordHash = "x", Role = UserRole.Admin, IsActive = true };
        var alternate = new ApplicationUser { FullName = "Director Alternate", Email = "alternate@test", PasswordHash = "x", Role = UserRole.Admin, IsActive = true };
        fixture.Db.AddRange(named, alternate);
        await fixture.Db.SaveChangesAsync();

        var complaint = await fixture.Service.LodgeComplaintAsync(client.Id, new LodgeComplaintViewModel { CaseId = matter.Id, Category = ComplaintCategory.Conduct, Description = "Director Named treated the client unfairly during the consultation." });

        Assert.Equal(alternate.Id, complaint.RoutedToUserId);
        Assert.Contains(named.Id.ToString(), complaint.RestrictedUserIds);
        Assert.Equal(2, await fixture.Db.StaffServiceRecordEntries.CountAsync());
    }

    [Fact]
    public async Task Complaint_duplicate_requires_confirmation_and_routes_away_from_matter_lawyer()
    {
        await using var fixture = await Fixture.CreateAsync();
        var (matter, lawyer, client) = await fixture.SeedMatterAsync();
        var director = new ApplicationUser { FullName = "Senior Director", Email = "director@test", PasswordHash = "x", Role = UserRole.Admin, IsActive = true };
        fixture.Db.Users.Add(director);
        await fixture.Db.SaveChangesAsync();
        var input = new LodgeComplaintViewModel { CaseId = matter.Id, Category = ComplaintCategory.Communication, Description = "There has been no response for several weeks." };

        var first = await fixture.Service.LodgeComplaintAsync(client.Id, input);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.LodgeComplaintAsync(client.Id, input));
        Assert.Equal(director.Id, first.RoutedToUserId);
        Assert.Contains(lawyer.Id.ToString(), first.RestrictedUserIds);
        Assert.False(string.IsNullOrWhiteSpace(first.ReferenceNumber));
        Assert.Single(await fixture.Db.StaffServiceRecordEntries.ToListAsync());
    }

    [Fact]
    public async Task Complaint_requires_an_active_or_closed_client_matter_and_starts_the_category_clock()
    {
        await using var fixture = await Fixture.CreateAsync();
        var (matter, _, client) = await fixture.SeedMatterAsync();
        var director = new ApplicationUser { FullName = "Senior Director", Email = "director@test", PasswordHash = "x", Role = UserRole.Admin, IsActive = true };
        fixture.Db.Users.Add(director); await fixture.Db.SaveChangesAsync();
        var submitted = DateTime.UtcNow;
        var complaint = await fixture.Service.LodgeComplaintAsync(client.Id, new LodgeComplaintViewModel { CaseId = matter.Id, Category = ComplaintCategory.Conduct, Description = "The service conduct requires formal independent review and a response." });
        Assert.False(string.IsNullOrWhiteSpace(complaint.ReferenceNumber));
        Assert.InRange(complaint.ResponseDueAtUtc, submitted.AddDays(5).AddMinutes(-1), submitted.AddDays(5).AddMinutes(1));
        matter.Status = CaseStatus.Archived; await fixture.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.LodgeComplaintAsync(client.Id, new LodgeComplaintViewModel { CaseId = matter.Id, Category = ComplaintCategory.Delay, Description = "The archived matter should not accept a new service complaint through this workflow." }));
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        public ApplicationDbContext Db { get; }
        public IPracticeIntelligenceService Service { get; }
        private Fixture(SqliteConnection connection, ApplicationDbContext db) { this.connection = connection; Db = db; Service = new PracticeIntelligenceService(db, new FakeNotifications()); }
        public static async Task<Fixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
            var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
            await db.Database.EnsureCreatedAsync();
            return new Fixture(connection, db);
        }
        public async Task<(Case Matter, ApplicationUser Lawyer, Client Client)> SeedMatterAsync()
        {
            var lawyer = new ApplicationUser { FullName = "Attorney One", Email = $"{Guid.NewGuid()}@test", PasswordHash = "x", Role = UserRole.Lawyer, IsActive = true };
            var client = new Client { FirstName = "Client", LastName = "One", Email = $"{Guid.NewGuid()}@test", Phone = "1" };
            Db.AddRange(lawyer, client); await Db.SaveChangesAsync();
            var matter = new Case { CaseNumber = $"M-{Guid.NewGuid():N}", Title = "Current matter", ClientId = client.Id, LawyerId = lawyer.Id, CaseType = "Commercial", EvidenceStrength = .7m, Status = CaseStatus.Active };
            Db.Cases.Add(matter); await Db.SaveChangesAsync();
            return (matter, lawyer, client);
        }
        public async ValueTask DisposeAsync() { await Db.DisposeAsync(); await connection.DisposeAsync(); }
    }
    private sealed class FakeNotifications : INotificationService
    {
        public Task QueueAsync(int userId, string type, string title, string message, string? actionUrl, string? deduplicationKey, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
