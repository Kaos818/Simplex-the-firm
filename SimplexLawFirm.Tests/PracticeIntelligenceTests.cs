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
    public async Task Approved_reassignment_creates_live_handover_with_receiver_already_assigned()
    {
        await using var fixture = await Fixture.CreateAsync();
        var (matter, outgoing, _) = await fixture.SeedMatterAsync();
        var receiver = new ApplicationUser { FullName = "Attorney Two", Email = "two@test", PasswordHash = "x", Role = UserRole.Lawyer, IsActive = true };
        var director = new ApplicationUser { FullName = "Director", Email = "director@test", PasswordHash = "x", Role = UserRole.Admin, IsActive = true };
        fixture.Db.AddRange(receiver, director);
        await fixture.Db.SaveChangesAsync();

        var handover = await fixture.Service.ApproveReassignmentAsync(matter.Id, receiver.Id, director.Id, "Workload reallocation", default);

        Assert.Equal(outgoing.Id, matter.LawyerId);
        Assert.Equal(receiver.Id, handover.ReceivingAttorneyId);
        Assert.Equal(5, handover.Items.Count);
        Assert.Equal(ReassignmentStatus.HandoverPreparing, (await fixture.Db.CaseReassignments.SingleAsync()).Status);
    }

    [Fact]
    public async Task StartHandover_leaves_the_receiving_attorney_unset_until_director_review()
    {
        await using var fixture = await Fixture.CreateAsync();
        var (matter, outgoing, _) = await fixture.SeedMatterAsync();

        var handover = await fixture.Service.StartHandoverAsync(matter.Id, outgoing.Id, "Going on extended leave.");

        Assert.Null(handover.ReceivingAttorneyId);
        Assert.Equal("Going on extended leave.", handover.Notes);
        Assert.Equal(HandoverStatus.Preparing, handover.Status);
        Assert.Equal(5, handover.Items.Count);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.StartHandoverAsync(matter.Id, outgoing.Id, "Second attempt while one is already open."));
    }

    [Fact]
    public async Task Notes_and_every_mandatory_item_are_required_before_a_handover_can_reach_the_director()
    {
        await using var fixture = await Fixture.CreateAsync();
        var (matter, outgoing, _) = await fixture.SeedMatterAsync();
        fixture.Db.Add(new CalendarEvent { Title = "Court deadline", Description = "File answering papers", Location = "Court", CaseId = matter.Id, Type = EventType.Deadline, Status = EventStatus.Scheduled, StartDateTime = DateTime.UtcNow.AddDays(2), EndDateTime = DateTime.UtcNow.AddDays(2).AddHours(1) });
        await fixture.Db.SaveChangesAsync();

        var handover = await fixture.Service.StartHandoverAsync(matter.Id, outgoing.Id, "Workload reallocation.");
        Assert.False(await fixture.Service.MarkHandoverReadyAsync(handover.Id));
        Assert.Equal(HandoverStatus.Preparing, handover.Status);
        Assert.Contains(handover.Items, x => x.Type == "Deadlines" && x.IsMandatory && !x.IsResolved);

        foreach (var item in handover.Items.Where(x => x.IsMandatory)) { item.IsResolved = true; item.ResolutionNote = "Position recorded for the receiving attorney."; }
        handover.Notes = null;
        await fixture.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.MarkHandoverReadyAsync(handover.Id));

        handover.Notes = "Full briefing for whoever receives this matter.";
        await fixture.Db.SaveChangesAsync();
        Assert.True(await fixture.Service.MarkHandoverReadyAsync(handover.Id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.MarkHandoverReadyAsync(handover.Id));
    }

    [Fact]
    public async Task Director_approval_assigns_the_receiver_transfers_the_case_immediately_and_notifies_the_client()
    {
        await using var fixture = await Fixture.CreateAsync();
        var (matter, outgoing, client) = await fixture.SeedMatterAsync();
        var clientUser = new ApplicationUser { FullName = client.FullName, Email = client.Email, PasswordHash = "x", Role = UserRole.Client, IsActive = true };
        var receiver = new ApplicationUser { FullName = "Attorney Two", Email = "two@test", PasswordHash = "x", Role = UserRole.Lawyer, IsActive = true };
        var director = new ApplicationUser { FullName = "Director", Email = "director@test", PasswordHash = "x", Role = UserRole.Admin, IsActive = true };
        fixture.Db.AddRange(clientUser, receiver, director);
        await fixture.Db.SaveChangesAsync();

        var handover = await fixture.Service.StartHandoverAsync(matter.Id, outgoing.Id, "Workload reallocation.");
        foreach (var item in handover.Items.Where(x => x.IsMandatory)) { item.IsResolved = true; item.ResolutionNote = "Done."; }
        await fixture.Db.SaveChangesAsync();
        Assert.True(await fixture.Service.MarkHandoverReadyAsync(handover.Id));
        Assert.Equal(HandoverStatus.PendingDirectorReview, handover.Status);

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.SubmitDirectorReviewAsync(handover.Id, director.Id, true, null, "", null, null));
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.SubmitDirectorReviewAsync(handover.Id, director.Id, true, null, "Missing a receiver.", null, null));

        await fixture.Service.SubmitDirectorReviewAsync(handover.Id, director.Id, true, receiver.Id, "Prepared and reviewed; proceed.", "Client is anxious, call within 24h.", null);

        Assert.Equal(HandoverStatus.Accepted, handover.Status);
        Assert.Equal(receiver.Id, handover.ReceivingAttorneyId);
        Assert.Equal(receiver.Id, matter.LawyerId);
        Assert.Equal(director.Id, handover.DirectorReviewedByUserId);
        Assert.Equal("Client is anxious, call within 24h.", handover.DirectorRiskFlags);
        Assert.NotNull(handover.ClientNotifiedAtUtc);
        Assert.Equal(ReassignmentStatus.Completed, (await fixture.Db.CaseReassignments.SingleAsync()).Status);
        Assert.Single(fixture.SentEmails, m => m.To == client.Email);
    }

    [Fact]
    public async Task Director_can_return_handover_with_a_reason_and_outgoing_attorney_can_cancel_instead_of_retrying()
    {
        await using var fixture = await Fixture.CreateAsync();
        var (matter, outgoing, _) = await fixture.SeedMatterAsync();
        var director = new ApplicationUser { FullName = "Director", Email = "director@test", PasswordHash = "x", Role = UserRole.Admin, IsActive = true };
        fixture.Db.Add(director);
        await fixture.Db.SaveChangesAsync();
        var handover = await fixture.Service.StartHandoverAsync(matter.Id, outgoing.Id, "Workload reallocation.");
        foreach (var item in handover.Items.Where(x => x.IsMandatory)) item.IsResolved = true;
        await fixture.Db.SaveChangesAsync();
        await fixture.Service.MarkHandoverReadyAsync(handover.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.SubmitDirectorReviewAsync(handover.Id, director.Id, false, null, null, null, ""));
        await fixture.Service.SubmitDirectorReviewAsync(handover.Id, director.Id, false, null, null, null, "Notes are too thin, expand the briefing.");
        Assert.Equal(HandoverStatus.Preparing, handover.Status);
        Assert.Contains("too thin", handover.DirectorReturnReason);

        await fixture.Service.CancelHandoverAsync(handover.Id, outgoing.Id);
        Assert.Equal(HandoverStatus.Cancelled, handover.Status);
        // Cancelling frees the matter for a fresh attempt.
        var restarted = await fixture.Service.StartHandoverAsync(matter.Id, outgoing.Id, "Trying again with a fuller briefing.");
        Assert.NotEqual(handover.Id, restarted.Id);
    }

    [Fact]
    public async Task Director_can_dispute_a_specific_item_which_reopens_it_and_notifies_the_outgoing_attorney()
    {
        await using var fixture = await Fixture.CreateAsync();
        var (matter, outgoing, _) = await fixture.SeedMatterAsync();
        var receiver = new ApplicationUser { FullName = "Attorney Two", Email = "two@test", PasswordHash = "x", Role = UserRole.Lawyer, IsActive = true };
        var director = new ApplicationUser { FullName = "Director", Email = "director@test", PasswordHash = "x", Role = UserRole.Admin, IsActive = true };
        fixture.Db.AddRange(receiver, director);
        await fixture.Db.SaveChangesAsync();
        var handover = await fixture.Service.StartHandoverAsync(matter.Id, outgoing.Id, "Workload reallocation.");
        foreach (var item in handover.Items.Where(x => x.IsMandatory)) { item.IsResolved = true; item.ResolutionNote = "Done."; }
        await fixture.Db.SaveChangesAsync();
        await fixture.Service.MarkHandoverReadyAsync(handover.Id);
        Assert.Equal(HandoverStatus.PendingDirectorReview, handover.Status);
        var disputedItem = handover.Items.First(x => x.IsMandatory);

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.DisputeHandoverItemAsync(handover.Id, disputedItem.Id, director.Id, ""));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.Service.DisputeHandoverItemAsync(handover.Id, disputedItem.Id, outgoing.Id, "Not actually done."));

        await fixture.Service.DisputeHandoverItemAsync(handover.Id, disputedItem.Id, director.Id, "The signature bundle is still outstanding.");
        Assert.Equal(HandoverStatus.Preparing, handover.Status);
        Assert.False(disputedItem.IsResolved);
        Assert.Equal("The signature bundle is still outstanding.", disputedItem.DirectorDisputeNote);

        // Once the director has already moved on from review, disputing is no longer allowed.
        disputedItem.IsResolved = true; disputedItem.ResolutionNote = "Fixed."; disputedItem.DirectorDisputeNote = null;
        await fixture.Db.SaveChangesAsync();
        await fixture.Service.MarkHandoverReadyAsync(handover.Id);
        Assert.Equal(HandoverStatus.PendingDirectorReview, handover.Status);
        await fixture.Service.SubmitDirectorReviewAsync(handover.Id, director.Id, true, receiver.Id, "Reviewed and approved.", null, null);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.DisputeHandoverItemAsync(handover.Id, disputedItem.Id, director.Id, "Too late."));
    }

    [Fact]
    public async Task A_query_can_be_raised_on_a_handover_that_is_still_in_progress()
    {
        await using var fixture = await Fixture.CreateAsync();
        var (matter, outgoing, _) = await fixture.SeedMatterAsync();
        var handover = await fixture.Service.StartHandoverAsync(matter.Id, outgoing.Id, "Workload reallocation.");

        await fixture.Service.RaiseHandoverQueryAsync(handover.Id, outgoing.Id, "Does anyone know the status of the settlement offer?");
        Assert.Single(await fixture.Db.HandoverQueries.ToListAsync());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.Service.RaiseHandoverQueryAsync(handover.Id, 999999, "Not party to this handover."));
    }

    [Fact]
    public async Task Resolving_a_complaint_requires_a_mediation_step_and_a_formal_response()
    {
        await using var fixture = await Fixture.CreateAsync();
        var (matter, _, client) = await fixture.SeedMatterAsync();
        var director = new ApplicationUser { FullName = "Senior Director", Email = "director@test", PasswordHash = "x", Role = UserRole.Admin, IsActive = true };
        var otherDirector = new ApplicationUser { FullName = "Other Director", Email = "other@test", PasswordHash = "x", Role = UserRole.Admin, IsActive = true };
        fixture.Db.AddRange(director, otherDirector);
        await fixture.Db.SaveChangesAsync();
        var complaint = await fixture.Service.LodgeComplaintAsync(client.Id, new LodgeComplaintViewModel { CaseId = matter.Id, Category = ComplaintCategory.Delay, Description = "The matter has stalled for weeks with no explanation given to me." });

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.ResolveComplaintAsync(complaint.Id, complaint.RoutedToUserId, ComplaintResolutionOutcome.Upheld, [], "We are sorry.", null));
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.ResolveComplaintAsync(complaint.Id, complaint.RoutedToUserId, ComplaintResolutionOutcome.Upheld, ["Apology issued"], "", null));
        var notReviewer = complaint.RoutedToUserId == director.Id ? otherDirector.Id : director.Id;
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.Service.ResolveComplaintAsync(complaint.Id, notReviewer, ComplaintResolutionOutcome.Upheld, ["Apology issued"], "We are sorry.", null));

        var resolved = await fixture.Service.ResolveComplaintAsync(complaint.Id, complaint.RoutedToUserId, ComplaintResolutionOutcome.Upheld, ["Apology issued", "Fee waived"], "We are sorry for the delay.", "10% fee reduction");
        Assert.Equal(ComplaintStatus.Resolved, resolved.Status);
        Assert.True(resolved.ClientNotifiedOfResolution);
        Assert.Contains("Apology issued", resolved.MediationSteps);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.ResolveComplaintAsync(complaint.Id, complaint.RoutedToUserId, ComplaintResolutionOutcome.Upheld, ["Again"], "Again.", null));
    }

    [Fact]
    public async Task Complaint_appointment_can_be_booked_by_the_reviewer_or_the_matching_client_and_replaces_any_prior_booking()
    {
        await using var fixture = await Fixture.CreateAsync();
        var (matter, _, client) = await fixture.SeedMatterAsync();
        var director = new ApplicationUser { FullName = "Senior Director", Email = "director@test", PasswordHash = "x", Role = UserRole.Admin, IsActive = true };
        var clientUser = new ApplicationUser { FullName = client.FullName, Email = client.Email, PasswordHash = "x", Role = UserRole.Client, IsActive = true };
        var stranger = new ApplicationUser { FullName = "Stranger", Email = "stranger@test", PasswordHash = "x", Role = UserRole.Client, IsActive = true };
        fixture.Db.AddRange(director, clientUser, stranger);
        await fixture.Db.SaveChangesAsync();
        var complaint = await fixture.Service.LodgeComplaintAsync(client.Id, new LodgeComplaintViewModel { CaseId = matter.Id, Category = ComplaintCategory.Delay, Description = "The matter has stalled for weeks with no explanation given to me." });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.Service.BookComplaintAppointmentAsync(complaint.Id, stranger.Id, DateTime.UtcNow.AddDays(2), AppointmentFormat.Video, null));
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.BookComplaintAppointmentAsync(complaint.Id, clientUser.Id, DateTime.UtcNow.AddDays(-1), AppointmentFormat.Video, null));

        var first = await fixture.Service.BookComplaintAppointmentAsync(complaint.Id, clientUser.Id, DateTime.UtcNow.AddDays(2), AppointmentFormat.Video, "Discuss fee");
        Assert.Equal(ComplaintAppointmentStatus.Scheduled, first.Status);
        var second = await fixture.Service.BookComplaintAppointmentAsync(complaint.Id, complaint.RoutedToUserId, DateTime.UtcNow.AddDays(3), AppointmentFormat.InPerson, null);
        Assert.Equal(ComplaintAppointmentStatus.Cancelled, (await fixture.Db.ComplaintAppointments.SingleAsync(x => x.Id == first.Id)).Status);
        Assert.Equal(ComplaintAppointmentStatus.Scheduled, second.Status);
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
        public List<(string To, string Subject)> SentEmails { get; } = [];
        private Fixture(SqliteConnection connection, ApplicationDbContext db)
        {
            this.connection = connection; Db = db;
            Service = new PracticeIntelligenceService(db, new FakeNotifications(), new FakeEmail(SentEmails));
        }
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
    private sealed class FakeEmail(List<(string To, string Subject)> sent) : IEmailService
    {
        public Task QueueAsync(string to, string subject, string html, string text, string deduplicationKey, CancellationToken cancellationToken = default)
        {
            sent.Add((to, subject));
            return Task.CompletedTask;
        }
    }
}
