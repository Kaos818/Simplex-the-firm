using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Hosting;
using SimplexLawFirm.Controllers;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services.Email;
using SimplexLawFirm.Services.Security;
using SimplexLawFirm.Services.Storage;
using Xunit;

namespace SimplexLawFirm.Tests;

public sealed class ExternalEvidencePortalTests
{
    [Fact]
    public async Task Link_can_be_opened_once_and_second_visit_is_closed()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options); await db.Database.EnsureCreatedAsync();
        var client = new Client { FirstName = "Client", LastName = "One", Email = "client@test", Phone = "1" };
        var lawyer = new ApplicationUser { FullName = "Lawyer", Email = "lawyer@test", PasswordHash = "x", Role = UserRole.Lawyer, IsActive = true };
        db.AddRange(client, lawyer); await db.SaveChangesAsync();
        var matter = new Case { CaseNumber = "MAT-EXT", Title = "External evidence", ClientId = client.Id, LawyerId = lawyer.Id, Status = CaseStatus.Active };
        db.Add(matter); await db.SaveChangesAsync();
        var token = SecureToken.Create();
        db.ExternalEvidenceRequests.Add(new() { CaseId = matter.Id, RecipientEmail = "opponent@test", RecipientName = "Opponent", TokenHash = token.Hash, RequestedByUserId = lawyer.Id, ExpiresAtUtc = DateTime.UtcNow.AddHours(1) });
        await db.SaveChangesAsync();
        var controller = Controller(db);
        var first = await controller.Open(token.Raw, default);
        var second = await controller.Open(token.Raw, default);
        Assert.IsType<ViewResult>(first);
        Assert.Equal("Closed", Assert.IsType<ViewResult>(second).ViewName);
        Assert.NotNull((await db.ExternalEvidenceRequests.SingleAsync()).AccessedAtUtc);
    }

    private static ExternalEvidenceController Controller(ApplicationDbContext db)
    {
        var root = Path.Combine(Path.GetTempPath(), $"simplex-evidence-{Guid.NewGuid():N}"); Directory.CreateDirectory(root);
        var controller = new ExternalEvidenceController(db, new Email(), new ExternalEvidenceStorage(new Environment(root)), new ConfigurationBuilder().Build());
        var http = new DefaultHttpContext(); http.Features.Set<ISessionFeature>(new SessionFeature { Session = new MemorySession() });
        controller.ControllerContext = new ControllerContext { HttpContext = http }; return controller;
    }
    private sealed class Email : IEmailService { public Task QueueAsync(string to, string subject, string html, string text, string deduplicationKey, CancellationToken cancellationToken = default) => Task.CompletedTask; }
    private sealed class Environment(string root) : IWebHostEnvironment { public string ApplicationName { get; set; } = "Tests"; public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider(); public string WebRootPath { get; set; } = root; public string EnvironmentName { get; set; } = "Development"; public string ContentRootPath { get; set; } = root; public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider(); }
    private sealed class SessionFeature : ISessionFeature { public ISession Session { get; set; } = null!; }
    private sealed class MemorySession : ISession
    {
        private readonly Dictionary<string, byte[]> values = [];
        public bool IsAvailable => true; public string Id { get; } = Guid.NewGuid().ToString(); public IEnumerable<string> Keys => values.Keys;
        public void Clear() => values.Clear(); public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask; public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => values.Remove(key); public void Set(string key, byte[] value) => values[key] = value; public bool TryGetValue(string key, out byte[] value) => values.TryGetValue(key, out value!);
    }
}
