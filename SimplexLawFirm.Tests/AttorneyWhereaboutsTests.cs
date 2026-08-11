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

public sealed class AttorneyWhereaboutsTests
{
    [Fact]
    public async Task CheckIn_verifies_location_within_geofence()
    {
        await using var f = await Fixture.CreateAsync();
        f.Db.KnownVenues.Add(new KnownVenue { Name = "Durban High Court", Latitude = -29.8579, Longitude = 31.0292 });
        await f.Db.SaveChangesAsync();

        var item = await f.Service.CheckInAsync(f.AttorneyId, null, "Durban High Court", DateTime.UtcNow.AddHours(1), -29.8579, 31.0292);
        Assert.True(item.LocationVerified);
        Assert.NotNull(item.DistanceFromVenueMeters);
        Assert.True(item.DistanceFromVenueMeters < 1);
    }

    [Fact]
    public async Task CheckIn_refuses_when_far_from_venue_without_a_reason()
    {
        await using var f = await Fixture.CreateAsync();
        f.Db.KnownVenues.Add(new KnownVenue { Name = "Durban High Court", Latitude = -29.8579, Longitude = 31.0292 });
        await f.Db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            f.Service.CheckInAsync(f.AttorneyId, null, "Durban High Court", DateTime.UtcNow.AddHours(1), -26.2041, 28.0473));

        var item = await f.Service.CheckInAsync(f.AttorneyId, null, "Durban High Court", DateTime.UtcNow.AddHours(1), -26.2041, 28.0473, "Signal only picked up nearby");
        Assert.False(item.LocationVerified);
        Assert.Equal("Signal only picked up nearby", item.LocationOverrideReason);
    }

    [Fact]
    public async Task Emergency_notifies_and_checkout_clears_it()
    {
        await using var f = await Fixture.CreateAsync();
        var item = await f.Service.CheckInAsync(f.AttorneyId, null, "Some court", DateTime.UtcNow.AddHours(1));
        await f.Service.RaiseEmergencyAsync(f.AttorneyId, item.Id);
        Assert.Equal(WhereaboutStatus.Emergency, (await f.Db.AttorneyWhereabouts.FindAsync(item.Id))!.Status);
        await f.Service.CheckOutAsync(f.AttorneyId, item.Id);
        Assert.Equal(WhereaboutStatus.Returned, (await f.Db.AttorneyWhereabouts.FindAsync(item.Id))!.Status);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        public ApplicationDbContext Db { get; }
        public IOperationalUseCaseService Service { get; }
        public int AttorneyId { get; private set; }

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

            var attorney = new ApplicationUser { FullName = "A. Attorney", Email = "attorney@example.test", PasswordHash = "x", Role = UserRole.Lawyer, IsActive = true };
            db.Users.Add(attorney);
            await db.SaveChangesAsync();
            f.AttorneyId = attorney.Id;
            return f;
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
