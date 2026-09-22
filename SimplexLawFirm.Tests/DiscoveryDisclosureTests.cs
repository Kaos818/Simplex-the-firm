using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services;
using SimplexLawFirm.Services.Email;
using SimplexLawFirm.Services.Notifications;
using Xunit;

namespace SimplexLawFirm.Tests;

public sealed class DiscoveryDisclosureTests
{
    [Fact]
    public async Task Privilege_review_then_reciprocal_exchange_tracks_compliance_and_escalation()
    {
        await using var fixture = await Fixture.CreateAsync();
        var bundle = await fixture.Service.CreateAsync(fixture.Matter.Id, fixture.Attorney.Id, "Opposing Counsel", "opponent@test.example", [fixture.Disclosable.Id, fixture.Privileged.Id], new Dictionary<int, string> { [fixture.Privileged.Id] = "Legal advice privilege" });
        Assert.Equal(DiscoveryBundleStatus.AwaitingPrivilegeReview, bundle.Status);

        var pending = bundle.Items.Single(x => x.DocumentId == fixture.Privileged.Id);
        await fixture.Service.DecideAsync(pending.Id, fixture.Director.Id, true, "Contains confidential legal advice.");
        Assert.Equal(DiscoveryBundleStatus.ReadyForExchange, bundle.Status);

        await fixture.Service.FinaliseAsync(bundle.Id, fixture.Attorney.Id, "https://example.test/DiscoveryDisclosure/Portal?token=" + bundle.ExchangeToken);
        Assert.Equal(DiscoveryBundleStatus.Exchanged, bundle.Status);
        Assert.Single(fixture.Email.Messages);
        await fixture.Service.MarkAccessedAsync(bundle.ExchangeToken);
        Assert.NotNull(bundle.AccessedAtUtc);

        await fixture.Service.RecordOpposingBundleAsync(bundle.Id, [("opponent-disclosure.pdf", "1/opponent.bin", "application/pdf", 42)]);
        Assert.Equal(DiscoveryBundleStatus.Received, bundle.Status);
        await fixture.Service.SetComplianceAsync(bundle.Id, fixture.Attorney.Id, false, "Missing expert report and damages schedule.");
        Assert.Equal(DiscoveryBundleStatus.GapsIdentified, bundle.Status);
        await fixture.Service.EscalateGapsAsync(bundle.Id, fixture.Attorney.Id);
        Assert.Equal(2, fixture.Email.Messages.Count);
        Assert.Single(await fixture.Db.OpposingDisclosureDocuments.Where(x => x.DiscoveryBundleId == bundle.Id).ToListAsync());
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        public ApplicationDbContext Db { get; } public IDiscoveryDisclosureService Service { get; } public FakeEmail Email { get; } public ApplicationUser Attorney { get; } public ApplicationUser Director { get; } public Case Matter { get; } public Document Disclosable { get; } public Document Privileged { get; }
        private Fixture(SqliteConnection connection, ApplicationDbContext db, FakeEmail email, ApplicationUser attorney, ApplicationUser director, Case matter, Document disclosable, Document privileged) { this.connection = connection; Db = db; Email = email; Service = new DiscoveryDisclosureService(db, new Notices(), email); Attorney = attorney; Director = director; Matter = matter; Disclosable = disclosable; Privileged = privileged; }
        public static async Task<Fixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync(); var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options); await db.Database.EnsureCreatedAsync();
            var attorney = new ApplicationUser { FullName = "Attorney", Email = "attorney@test", PasswordHash = "x", Role = UserRole.Lawyer, IsActive = true }; var director = new ApplicationUser { FullName = "Director", Email = "director@test", PasswordHash = "x", Role = UserRole.Director, IsActive = true }; var client = new Client { FirstName = "Client", LastName = "Test", Email = "client@test", Phone = "1" }; db.AddRange(attorney, director, client); await db.SaveChangesAsync();
            var matter = new Case { CaseNumber = "DISC-1", Title = "Discovery", ClientId = client.Id, LawyerId = attorney.Id, Status = CaseStatus.Active }; db.Cases.Add(matter); await db.SaveChangesAsync();
            var disclosable = new Document { CaseId = matter.Id, ClientId = client.Id, UploadedById = attorney.Id, FileName = "statement.pdf", FilePath = "1/statement.bin", FileSize = "1 KB", FileType = "application/pdf", Description = "Statement", Category = DocumentCategory.Evidence, UploadedAt = DateTime.UtcNow }; var privileged = new Document { CaseId = matter.Id, ClientId = client.Id, UploadedById = attorney.Id, FileName = "advice.pdf", FilePath = "1/advice.bin", FileSize = "1 KB", FileType = "application/pdf", Description = "Advice", Category = DocumentCategory.Internal, UploadedAt = DateTime.UtcNow }; db.AddRange(disclosable, privileged); await db.SaveChangesAsync(); return new Fixture(connection, db, new FakeEmail(), attorney, director, matter, disclosable, privileged);
        }
        public async ValueTask DisposeAsync() { await Db.DisposeAsync(); await connection.DisposeAsync(); }
    }
    private sealed class Notices : INotificationService { public Task QueueAsync(int userId, string type, string title, string message, string? actionUrl, string? key, CancellationToken ct = default) => Task.CompletedTask; }
    private sealed class FakeEmail : IEmailService { public List<string> Messages { get; } = []; public Task QueueAsync(string to, string subject, string html, string text, string deduplicationKey, CancellationToken ct = default) { Messages.Add(subject); return Task.CompletedTask; } }
}
