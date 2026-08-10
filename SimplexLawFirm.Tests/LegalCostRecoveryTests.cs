using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services;
using SimplexLawFirm.Services.Email;
using SimplexLawFirm.Services.Notifications;
using SimplexLawFirm.ViewModels;
using Xunit;

namespace SimplexLawFirm.Tests;

public sealed class LegalCostRecoveryTests
{
    [Fact]
    public async Task Assigned_attorney_claim_uses_selected_logged_hour_snapshots()
    {
        await using var fixture = await Fixture.CreateAsync();
        var claim = await fixture.Service.SubmitAsync(fixture.Attorney.Id, ClaimInput(fixture, [fixture.First.Id, fixture.Second.Id], LegalCostRecoveryGround.MissedHearing, "The missed hearing caused avoidable preparation and attendance costs."));
        Assert.Equal(3_500, claim.ClaimedAmount);
        Assert.Equal(LegalCostRecoveryStatus.PendingDirectorApproval, claim.Status);
        Assert.Null(claim.ServedAtUtc);
        Assert.Equal(2, claim.TimeEntries.Count);
    }

    [Fact]
    public async Task Unassigned_attorney_cannot_submit_claim()
    {
        await using var fixture = await Fixture.CreateAsync();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.Service.SubmitAsync(fixture.Director.Id, ClaimInput(fixture, [fixture.First.Id], justification: "This attempt is not made by the assigned attorney.")));
    }

    [Fact]
    public async Task Partial_award_is_bounded_and_service_occurs_after_decision()
    {
        await using var fixture = await Fixture.CreateAsync();
        var claim = await fixture.Service.SubmitAsync(fixture.Attorney.Id, ClaimInput(fixture, [fixture.First.Id], LegalCostRecoveryGround.NonComplianceWithCourtOrder, "Court-order non-compliance caused unnecessary drafting work."));
        await Assert.ThrowsAsync<ArgumentException>(() => fixture.Service.DecideAsync(fixture.Director.Id, new() { ClaimId = claim.Id, Decision = LegalCostRecoveryStatus.PartiallyAwarded, AwardedAmount = claim.ClaimedAmount + 1, DecisionNotes = "Amount exceeds the valid claim total." }));
        claim = await fixture.Service.DecideAsync(fixture.Director.Id, new() { ClaimId = claim.Id, Decision = LegalCostRecoveryStatus.PartiallyAwarded, AwardedAmount = 1_000, DecisionNotes = "Only part of the additional work was reasonably recoverable." });
        Assert.Equal(1_000, claim.AwardedAmount);
        Assert.NotNull(claim.DecidedAtUtc);
        Assert.NotNull(claim.ServedAtUtc);
        Assert.StartsWith($"COST-{claim.Id}-", claim.ServiceDeliveryReference);
        Assert.Equal(2, claim.AuditEntries.Count(x => x.Action is "DirectorDecisionRecorded" or "MarkedServed"));
    }

    [Fact]
    public async Task Only_director_can_record_decision_and_time_entry_cannot_be_reused()
    {
        await using var fixture = await Fixture.CreateAsync();
        var input = ClaimInput(fixture, [fixture.First.Id], LegalCostRecoveryGround.FrivolousOrVexatiousApplication, "The frivolous application caused additional legal research work.");
        var claim = await fixture.Service.SubmitAsync(fixture.Attorney.Id, input);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.Service.DecideAsync(fixture.Attorney.Id, new() { ClaimId = claim.Id, Decision = LegalCostRecoveryStatus.ApprovedInFull, DecisionNotes = "Attorney cannot approve their own submitted claim." }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.SubmitAsync(fixture.Attorney.Id, input));
    }

    [Fact]
    public async Task Rejected_claim_is_not_served_and_does_not_increase_case_recovery_total()
    {
        await using var fixture = await Fixture.CreateAsync();
        var claim = await fixture.Service.SubmitAsync(fixture.Attorney.Id, ClaimInput(fixture, [fixture.First.Id], justification: "The opposing party's conduct caused avoidable attendance work."));
        claim = await fixture.Service.DecideAsync(fixture.Director.Id, new() { ClaimId = claim.Id, Decision = LegalCostRecoveryStatus.Rejected, DecisionNotes = "The evidence does not establish an awardable costs order." });

        Assert.Equal(0, claim.AwardedAmount);
        Assert.Null(claim.ServedAtUtc);
        Assert.Null(claim.ServiceDeliveryReference);
        Assert.Equal(0, fixture.Matter.CostRecoveryAwardedTotal);
        Assert.Contains(claim.AuditEntries, x => x.Action == "NotServed");
    }

    [Fact]
    public async Task Approved_claim_increases_case_recovery_total_and_is_served_once()
    {
        await using var fixture = await Fixture.CreateAsync();
        var claim = await fixture.Service.SubmitAsync(fixture.Attorney.Id, ClaimInput(fixture, [fixture.First.Id], justification: "The opposing party's conduct caused avoidable attendance work."));
        claim = await fixture.Service.DecideAsync(fixture.Director.Id, new() { ClaimId = claim.Id, Decision = LegalCostRecoveryStatus.ApprovedInFull, DecisionNotes = "The complete documented amount is reasonably recoverable." });

        Assert.Equal(claim.ClaimedAmount, fixture.Matter.CostRecoveryAwardedTotal);
        Assert.NotNull(claim.ServedAtUtc);
        Assert.NotNull(claim.ServiceDeliveryReference);
    }
    private static CreateLegalCostRecoveryViewModel ClaimInput(Fixture fixture, List<int> ids, LegalCostRecoveryGround ground = LegalCostRecoveryGround.MissedHearing, string justification = "The conduct caused avoidable additional legal work on this matter.") => new() { CaseId = fixture.Matter.Id, Ground = ground, Justification = justification, OpposingPartyName = "Opposing Counsel", OpposingPartyEmail = "opponent@example.com", TimeEntryIds = ids };

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        public ApplicationDbContext Db { get; }
        public ILegalCostRecoveryService Service { get; }
        public ApplicationUser Attorney { get; private set; } = null!;
        public ApplicationUser Director { get; private set; } = null!;
        public Case Matter { get; private set; } = null!;
        public TimeEntry First { get; private set; } = null!;
        public TimeEntry Second { get; private set; } = null!;
        private Fixture(SqliteConnection connection, ApplicationDbContext db) { this.connection = connection; Db = db; Service = new LegalCostRecoveryService(db, new Notifications(), new Email()); }
        public static async Task<Fixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
            var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options); await db.Database.EnsureCreatedAsync();
            var fixture = new Fixture(connection, db);
            fixture.Attorney = new() { FullName = "Assigned Attorney", Email = "attorney@test", PasswordHash = "x", Role = UserRole.Lawyer, IsActive = true };
            fixture.Director = new() { FullName = "Director", Email = "director@test", PasswordHash = "x", Role = UserRole.Admin, IsActive = true };
            var client = new Client { FirstName = "Client", LastName = "One", Email = "client@test", Phone = "1" };
            db.AddRange(fixture.Attorney, fixture.Director, client); await db.SaveChangesAsync();
            fixture.Matter = new() { CaseNumber = "UC74-001", Title = "Recovery matter", ClientId = client.Id, LawyerId = fixture.Attorney.Id, Status = CaseStatus.Active };
            db.Add(fixture.Matter); await db.SaveChangesAsync();
            fixture.First = new() { CaseId = fixture.Matter.Id, LawyerId = fixture.Attorney.Id, Description = "Avoidable hearing attendance", Date = DateTime.Today, Hours = 2, HourlyRate = 1_000, TotalAmount = 2_000, IsBillable = true };
            fixture.Second = new() { CaseId = fixture.Matter.Id, LawyerId = fixture.Attorney.Id, Description = "Additional drafting", Date = DateTime.Today, Hours = 1, HourlyRate = 1_500, TotalAmount = 1_500, IsBillable = true };
            db.AddRange(fixture.First, fixture.Second); await db.SaveChangesAsync(); return fixture;
        }
        public async ValueTask DisposeAsync() { await Db.DisposeAsync(); await connection.DisposeAsync(); }
    }
    private sealed class Notifications : INotificationService { public Task QueueAsync(int userId, string type, string title, string message, string? actionUrl, string? deduplicationKey, CancellationToken cancellationToken = default) => Task.CompletedTask; }
    private sealed class Email : IEmailService { public Task QueueAsync(string to, string subject, string html, string text, string deduplicationKey, CancellationToken cancellationToken = default) => Task.CompletedTask; }
}
