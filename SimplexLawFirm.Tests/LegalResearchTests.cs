using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services;
using SimplexLawFirm.Services.Notifications;
using Xunit;

namespace SimplexLawFirm.Tests;

public sealed class LegalResearchTests
{
    [Fact]
    public async Task External_results_rank_binding_authority_before_persuasive_authority()
    {
        await using var f = await Fixture.CreateAsync();
        await f.AddAuthorityAsync("Binding decision", AuthorityRank.Binding, AuthorityTreatment.GoodLaw, false);
        await f.AddAuthorityAsync("Persuasive decision", AuthorityRank.Persuasive, AuthorityTreatment.GoodLaw, false);

        var results = await f.Service.ResearchAsync(f.Lawyer.Id, f.Matter.Id, "contract fairness issue");

        Assert.Equal(2, results.Count);
        Assert.Equal(AuthorityRank.Binding, results[0].Rank);
        Assert.Contains(await f.Db.AuditEntries.ToListAsync(), x => x.Action == "Legal authority research performed");
    }

    [Fact]
    public async Task Adverse_authority_requires_confirmation_and_records_relevance()
    {
        await using var f = await Fixture.CreateAsync();
        var authority = await f.AddAuthorityAsync("Overturned decision", AuthorityRank.Binding, AuthorityTreatment.Overturned, false);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            f.Service.RelyAsync(f.Lawyer.Id, f.Matter.Id, authority.Id, "It concerns the same contract clause.", false));
        var reliance = await f.Service.RelyAsync(f.Lawyer.Id, f.Matter.Id, authority.Id,
            "It concerns the same contract clause and must be distinguished.", true);

        Assert.True(reliance.AdverseTreatmentConfirmed);
        Assert.NotEmpty(reliance.RelevanceReason);
        Assert.Contains(await f.Db.AuditEntries.ToListAsync(), x => x.Action == "Authority attached to matter");
    }

    [Fact]
    public async Task External_outage_returns_only_internal_fallback_and_records_limitation()
    {
        await using var f = await Fixture.CreateAsync(externalSourcesAvailable: false);
        await f.AddAuthorityAsync("External decision", AuthorityRank.Binding, AuthorityTreatment.GoodLaw, false);
        await f.AddAuthorityAsync("Internal precedent", AuthorityRank.Persuasive, AuthorityTreatment.GoodLaw, true);

        var results = await f.Service.ResearchAsync(f.Lawyer.Id, f.Matter.Id, "contract fairness issue");

        Assert.Single(results);
        Assert.True(results[0].IsInternalFallback);
        var audit = await f.Db.AuditEntries.SingleAsync(x => x.EntityType == "LegalResearch");
        Assert.Contains("\"limitedToInternal\":true", audit.SafeMetadataJson);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        public ApplicationDbContext Db { get; }
        public OperationalUseCaseService Service { get; }
        public ApplicationUser Lawyer { get; private set; } = null!;
        public Case Matter { get; private set; } = null!;

        private Fixture(SqliteConnection connection, ApplicationDbContext db, bool externalSourcesAvailable)
        {
            this.connection = connection;
            Db = db;
            Service = new OperationalUseCaseService(db, new FakeNotifications(), null!,
                Options.Create(new LegalResearchOptions { ExternalSourcesAvailable = externalSourcesAvailable }));
        }

        public static async Task<Fixture> CreateAsync(bool externalSourcesAvailable = true)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
            await db.Database.EnsureCreatedAsync();
            var fixture = new Fixture(connection, db, externalSourcesAvailable);
            fixture.Lawyer = new ApplicationUser { FullName = "Attorney", Email = "attorney@test", PasswordHash = "x", Role = UserRole.Lawyer, IsActive = true };
            var client = new Client { FirstName = "Client", LastName = "One", Email = "client@test", Phone = "1" };
            db.AddRange(fixture.Lawyer, client);
            await db.SaveChangesAsync();
            fixture.Matter = new Case { CaseNumber = "LAW-1", Title = "Research matter", CaseType = "Commercial", ClientId = client.Id, LawyerId = fixture.Lawyer.Id, Status = CaseStatus.Active };
            db.Cases.Add(fixture.Matter);
            await db.SaveChangesAsync();
            return fixture;
        }

        public async Task<LegalAuthority> AddAuthorityAsync(string citation, AuthorityRank rank, AuthorityTreatment treatment, bool internalFallback)
        {
            var authority = new LegalAuthority { Citation = citation, Subject = "Contract", Summary = "Contract fairness issue.", SearchText = "contract fairness issue", Rank = rank, Treatment = treatment, IsInternalFallback = internalFallback };
            Db.LegalAuthorities.Add(authority);
            await Db.SaveChangesAsync();
            return authority;
        }

        public async ValueTask DisposeAsync() { await Db.DisposeAsync(); await connection.DisposeAsync(); }
    }

    private sealed class FakeNotifications : INotificationService
    {
        public Task QueueAsync(int userId, string type, string title, string message, string? actionUrl, string? deduplicationKey, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
