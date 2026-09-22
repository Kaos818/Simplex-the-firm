using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services;
using SimplexLawFirm.Services.Notifications;
using Xunit;

namespace SimplexLawFirm.Tests;

public sealed class ClientResponseDelayTests
{
    [Fact]
    public async Task Request_uses_the_configured_sla_then_overdue_monitor_alerts_once_and_allows_extension()
    {
        await using var fixture = await Fixture.CreateAsync();
        var request = await fixture.Service.CreateAsync(new CaseDocumentRequest { CaseId = fixture.Matter.Id, RequestType = ClientResponseRequestType.Document, Title = "Signed mandate", RequiredDocuments = "Signed mandate", DueAtUtc = DateTime.UtcNow.AddYears(1) }, fixture.Lawyer.Id);

        Assert.Equal(7, request.ResponseSlaDaysSnapshot);
        Assert.InRange(request.DueAtUtc, DateTime.UtcNow.AddDays(7).AddMinutes(-1), DateTime.UtcNow.AddDays(7).AddMinutes(1));
        request.DueAtUtc = DateTime.UtcNow.AddMinutes(-1);
        await fixture.Db.SaveChangesAsync();
        await fixture.Service.MonitorOverdueAsync(DateTime.UtcNow);
        await fixture.Service.MonitorOverdueAsync(DateTime.UtcNow.AddMinutes(1));

        Assert.Equal(CaseDocumentRequestStatus.Overdue, request.Status);
        Assert.NotNull(request.OverdueAtUtc);
        Assert.Single(fixture.Notifications.Messages, x => x.Type == "ClientResponseOverdue");
        Assert.Contains(await fixture.Db.AuditEntries.ToListAsync(), x => x.Action == "Client response overdue");

        var newDeadline = DateTime.UtcNow.AddDays(3);
        await fixture.Service.RecordActionAsync(request.Id, fixture.Lawyer.Id, ClientResponseDelayAction.DeadlineExtended, "Client requested a short extension.", newDeadline);
        Assert.Equal(CaseDocumentRequestStatus.Requested, request.Status);
        Assert.Equal(ClientResponseDelayAction.DeadlineExtended, request.LastDelayAction);
        Assert.InRange(request.DueAtUtc, newDeadline.AddSeconds(-1), newDeadline.AddSeconds(1));
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        public ApplicationDbContext Db { get; }
        public ApplicationUser Lawyer { get; }
        public Case Matter { get; }
        public Notifications Notifications { get; }
        public IClientResponseDelayService Service { get; }
        private Fixture(SqliteConnection connection, ApplicationDbContext db, ApplicationUser lawyer, Case matter, Notifications notifications) { this.connection = connection; Db = db; Lawyer = lawyer; Matter = matter; Notifications = notifications; Service = new ClientResponseDelayService(db, notifications); }
        public static async Task<Fixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
            var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options); await db.Database.EnsureCreatedAsync();
            var lawyer = new ApplicationUser { FullName = "Attorney", Email = "attorney@test", PasswordHash = "x", Role = UserRole.Lawyer, IsActive = true };
            var client = new Client { FirstName = "Client", LastName = "Test", Email = "client@test", Phone = "0123456789" };
            db.AddRange(lawyer, client); await db.SaveChangesAsync();
            var matter = new Case { CaseNumber = "RESP-01", Title = "Response test", ClientId = client.Id, LawyerId = lawyer.Id, Status = CaseStatus.Active }; db.Cases.Add(matter); await db.SaveChangesAsync();
            var notifications = new Notifications(); return new Fixture(connection, db, lawyer, matter, notifications);
        }
        public async ValueTask DisposeAsync() { await Db.DisposeAsync(); await connection.DisposeAsync(); }
    }

    private sealed class Notifications : INotificationService
    {
        public List<(int UserId, string Type)> Messages { get; } = [];
        public Task QueueAsync(int userId, string type, string title, string message, string? actionUrl, string? deduplicationKey, CancellationToken cancellationToken = default) { Messages.Add((userId, type)); return Task.CompletedTask; }
    }
}
