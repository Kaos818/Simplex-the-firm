using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services;
using SimplexLawFirm.Services.Notifications;
using SimplexLawFirm.Services.Email;
using Xunit;

namespace SimplexLawFirm.Tests;

public sealed class OperationalResearchTests
{
    [Fact]
    public async Task Rely_refuses_without_a_relevance_reason()
    {
        await using var f = await Fixture.CreateAsync();
        var authority = await f.Authority("Good Law Case", AuthorityTreatment.GoodLaw);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            f.Service.RelyAsync(f.AttorneyId, f.CaseId, authority.Id, "", false));
    }

    [Fact]
    public async Task Rely_refuses_adverse_authority_without_express_confirmation()
    {
        await using var f = await Fixture.CreateAsync();
        var authority = await f.Authority("Overturned Case", AuthorityTreatment.Overturned);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            f.Service.RelyAsync(f.AttorneyId, f.CaseId, authority.Id, "Relevant to the point", false));
        var reliance = await f.Service.RelyAsync(f.AttorneyId, f.CaseId, authority.Id, "Relevant, cited to distinguish", true);
        Assert.True(reliance.AdverseTreatmentConfirmed);
    }

    [Fact]
    public async Task Research_persists_a_thread_entry_that_GetThreadAsync_returns()
    {
        await using var f = await Fixture.CreateAsync();
        await f.Authority("Contract materiality principle", AuthorityTreatment.GoodLaw, searchText: "materiality breach contract");
        await f.Service.ResearchAsync(f.AttorneyId, f.CaseId, "materiality breach contract cancellation");
        var thread = await f.Service.GetThreadAsync(f.CaseId, f.AttorneyId);
        Assert.Single(thread);
        Assert.Equal("materiality breach contract cancellation", thread[0].Issue);
        Assert.Equal(1, thread[0].ResultCount);
    }

    [Fact]
    public async Task GuidedResearch_splits_good_law_as_supporting_and_adverse_as_against()
    {
        await using var f = await Fixture.CreateAsync();
        await f.Authority("Supporting authority", AuthorityTreatment.GoodLaw, searchText: "materiality breach cancellation lease");
        await f.Authority("Distinguished authority", AuthorityTreatment.Distinguished, searchText: "materiality breach cancellation lease");
        var (supports, against) = await f.Service.GuidedResearchAsync(f.AttorneyId, f.CaseId,
            "whether materiality alone justifies cancellation", "materiality breach cancellation lease", "argue materiality is the only test");
        Assert.Single(supports);
        Assert.Equal(AuthorityTreatment.GoodLaw, supports[0].Treatment);
        Assert.Single(against);
        Assert.Equal(AuthorityTreatment.Distinguished, against[0].Treatment);
    }

    [Fact]
    public async Task RecordDisagreement_rejects_empty_note_and_persists_a_valid_one()
    {
        await using var f = await Fixture.CreateAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            f.Service.RecordDisagreementAsync(f.AttorneyId, f.CaseId, "Some topic", ""));
        var recorded = await f.Service.RecordDisagreementAsync(f.AttorneyId, f.CaseId, "Some topic", "I don't think this fully settles the point.");
        Assert.Equal("Some topic", recorded.Topic);
        Assert.Single(f.Db.ResearchDisagreements.Where(x => x.CaseId == f.CaseId));
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        public ApplicationDbContext Db { get; }
        public IOperationalUseCaseService Service { get; }
        public int AttorneyId { get; private set; }
        public int CaseId { get; private set; }

        private Fixture(SqliteConnection connection, ApplicationDbContext db)
        {
            this.connection = connection; Db = db;
            Service = new OperationalUseCaseService(db, new NoopNotifications(), new NoopEmail(),
                Options.Create(new LegalResearchOptions { ExternalSourcesAvailable = false }));
        }

        public static async Task<Fixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
            await db.Database.EnsureCreatedAsync();
            var f = new Fixture(connection, db);

            var client = new Client { Email = "client@example.test", Phone = "0123456789", FirstName = "Test", LastName = "Client" };
            db.Clients.Add(client);
            var attorney = new ApplicationUser { FullName = "A. Attorney", Email = "attorney@example.test", PasswordHash = "x", Role = UserRole.Lawyer, IsActive = true };
            db.Users.Add(attorney);
            await db.SaveChangesAsync();

            var matter = new Case { Title = "Test matter", Status = CaseStatus.Active, ClientId = client.Id, LawyerId = attorney.Id, CaseNumber = "T-0001" };
            db.Cases.Add(matter);
            await db.SaveChangesAsync();

            f.AttorneyId = attorney.Id; f.CaseId = matter.Id;
            return f;
        }

        public async Task<LegalAuthority> Authority(string citation, AuthorityTreatment treatment, string? searchText = null, bool internalFallback = true)
        {
            var authority = new LegalAuthority
            {
                Citation = citation, Subject = "Test", Summary = citation, SearchText = searchText ?? citation,
                Rank = AuthorityRank.Binding, Treatment = treatment, IsInternalFallback = internalFallback
            };
            Db.LegalAuthorities.Add(authority); await Db.SaveChangesAsync(); return authority;
        }

        public async ValueTask DisposeAsync() { await Db.DisposeAsync(); await connection.DisposeAsync(); }
    }

    private sealed class NoopNotifications : INotificationService
    {
        public Task QueueAsync(int userId, string type, string title, string message, string? actionUrl, string? deduplicationKey, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NoopEmail : IEmailService
    {
        public Task QueueAsync(string to, string subject, string html, string text, string deduplicationKey, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
