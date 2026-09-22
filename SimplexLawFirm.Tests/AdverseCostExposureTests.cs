using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services;
using SimplexLawFirm.Services.Notifications;
using Xunit;

namespace SimplexLawFirm.Tests;

public sealed class AdverseCostExposureTests
{
    [Fact]
    public async Task Logs_court_order_calculates_shortfall_and_tracks_review_to_resolution()
    {
        await using var fixture = await Fixture.CreateAsync();
        var item = await fixture.Service.LogAsync(new AdverseCostExposure { CaseId = fixture.Matter.Id, CourtOrderDocumentId = fixture.Order.Id, IssuingCourt = "Durban High Court", RulingDateUtc = DateTime.UtcNow.AddDays(-1), CostOrderAmount = 12_000m }, fixture.Lawyer.Id);

        Assert.Equal(2_000m, item.ExistingCaseCostsSnapshot);
        Assert.Equal(14_000m, item.TotalExposure);
        Assert.Equal(5_000m, item.AvailableTrustRetainerSnapshot);
        Assert.Equal(9_000m, item.Shortfall);
        await fixture.Service.ReviewByAccountantAsync(item.Id, fixture.Accountant.Id, "Trust cover verified; shortfall confirmed.");
        await fixture.Service.UpdateProgressAsync(item.Id, fixture.Lawyer.Id, AdverseCostExposureStatus.Negotiation, 10_000m, "Opposing attorney agreed to negotiate the bill.");
        await fixture.Service.ConfirmDirectorPositionAsync(item.Id, fixture.Director.Id, "Proceed with settlement negotiation up to R10,000.");
        await fixture.Service.ResolveAsync(item.Id, fixture.Lawyer.Id, 9_500m, "Matter settled and exposure closed.");

        Assert.Equal(AdverseCostExposureStatus.Resolved, item.Status);
        Assert.Equal(9_500m, item.FinalSettledAmount);
        Assert.NotNull(item.AccountantReviewedAtUtc);
        Assert.NotNull(item.DirectorReviewedAtUtc);
        Assert.NotNull(item.ResolvedAtUtc);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        public ApplicationDbContext Db { get; } public ApplicationUser Lawyer { get; } public ApplicationUser Accountant { get; } public ApplicationUser Director { get; } public Case Matter { get; } public Document Order { get; } public IAdverseCostExposureService Service { get; }
        private Fixture(SqliteConnection c, ApplicationDbContext db, ApplicationUser lawyer, ApplicationUser accountant, ApplicationUser director, Case matter, Document order) { connection=c; Db=db; Lawyer=lawyer; Accountant=accountant; Director=director; Matter=matter; Order=order; Service=new AdverseCostExposureService(db,new Notifications()); }
        public static async Task<Fixture> CreateAsync()
        {
            var c=new SqliteConnection("Data Source=:memory:"); await c.OpenAsync(); var db=new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(c).Options); await db.Database.EnsureCreatedAsync();
            var lawyer=new ApplicationUser{FullName="Lawyer",Email="lawyer@test",PasswordHash="x",Role=UserRole.Lawyer,IsActive=true}; var accountant=new ApplicationUser{FullName="Accountant",Email="accountant@test",PasswordHash="x",Role=UserRole.Accountant,IsActive=true}; var director=new ApplicationUser{FullName="Director",Email="director@test",PasswordHash="x",Role=UserRole.Director,IsActive=true}; var client=new Client{FirstName="Client",LastName="Test",Email="client@test",Phone="1"}; db.AddRange(lawyer,accountant,director,client); await db.SaveChangesAsync();
            var matter=new Case{CaseNumber="ADVERSE-1",Title="Litigation",ClientId=client.Id,LawyerId=lawyer.Id,Status=CaseStatus.Active}; db.Cases.Add(matter); await db.SaveChangesAsync(); var order=new Document{CaseId=matter.Id,ClientId=client.Id,UploadedById=lawyer.Id,FileName="court-order.pdf",FilePath="orders/court-order.pdf",FileSize="1KB",FileType="application/pdf",Description="Cost order",Category=DocumentCategory.CourtOrders,UploadedAt=DateTime.UtcNow}; db.Documents.Add(order); db.TimeEntries.Add(new TimeEntry{CaseId=matter.Id,LawyerId=lawyer.Id,Description="Court preparation",Date=DateTime.UtcNow,Hours=1,HourlyRate=2_000m,TotalAmount=2_000m}); db.TrustAccounts.Add(new TrustAccount{ClientId=client.Id,Balance=5_000m,TotalDeposited=5_000m,LastUpdated=DateTime.UtcNow}); await db.SaveChangesAsync(); return new Fixture(c,db,lawyer,accountant,director,matter,order);
        }
        public async ValueTask DisposeAsync(){await Db.DisposeAsync();await connection.DisposeAsync();}
    }
    private sealed class Notifications : INotificationService { public Task QueueAsync(int userId,string type,string title,string message,string? actionUrl,string? key,CancellationToken ct=default)=>Task.CompletedTask; }
}
