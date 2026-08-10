using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services;
using SimplexLawFirm.Services.Notifications;
using SimplexLawFirm.ViewModels;
using Xunit;

namespace SimplexLawFirm.Tests;

public sealed class MatterCostEstimateTests
{
    [Fact]
    public async Task Quote_is_refused_and_gap_recorded_without_usable_history()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.SeedRateAsync();
        await fixture.SeedHistoryAsync(includeCosts: false);

        var enquiry = await fixture.Service.BeginAsync("Commercial", null);

        var refusal = await fixture.Db.MatterCostEstimates.SingleAsync(x => x.CostEstimateEnquiryId == enquiry.Id);
        Assert.Equal(CostEstimateStatus.Declined, refusal.Status);
        Assert.Contains("usable time or billing history", refusal.DeclineReason);
        Assert.Equal(EstimateGapStatus.Open, (await fixture.Db.CostEstimateCoverageGaps.SingleAsync()).Status);
    }

    [Fact]
    public async Task Estimate_uses_live_rates_and_history_then_locks_all_financial_inputs()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.SeedRateAsync();
        await fixture.SeedHistoryAsync(includeCosts: true);
        var enquiry = await fixture.Service.BeginAsync("Commercial", null);

        var estimate = await fixture.Service.CalculateAndLockAsync(enquiry.Id, Input());

        Assert.Equal(CostEstimateStatus.Locked, estimate.Status);
        Assert.True(estimate.TotalHigh > estimate.TotalLow);
        Assert.Equal(estimate.ProfessionalFeesLow + estimate.DisbursementsLow + estimate.VatLow, estimate.TotalLow);
        Assert.Contains("Attorney One", estimate.RatesSnapshotJson);
        Assert.Contains("HistoricalAverageHours", estimate.AssumptionsSnapshotJson);
        estimate.TotalHigh++;
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Db.SaveChangesAsync());
    }

    [Fact]
    public async Task Linked_estimate_blocks_out_of_tolerance_invoice_until_director_approval()
    {
        await using var fixture = await Fixture.CreateAsync();
        var (lawyer, client) = await fixture.SeedRateAsync();
        await fixture.SeedHistoryAsync(includeCosts: true, lawyer, client);
        var enquiry = await fixture.Service.BeginAsync("Commercial", client.Id);
        var estimate = await fixture.Service.CalculateAndLockAsync(enquiry.Id, Input(client.Email));
        var matter = new Case
        {
            CaseNumber = "LIVE-001", Title = "Live commercial matter", CaseType = "Commercial",
            ClientId = client.Id, LawyerId = lawyer.Id, Status = CaseStatus.Active
        };
        fixture.Db.Cases.Add(matter);
        await fixture.Db.SaveChangesAsync();
        await fixture.Service.LinkToMatterAsync(estimate.Id, matter.Id);
        var invoice = new Invoice
        {
            InvoiceNumber = "INV-UC04", ClientId = client.Id, CaseId = matter.Id,
            Amount = estimate.TotalHigh * 2, TotalAmount = estimate.TotalHigh * 2,
            IssueDate = DateTime.Today, DueDate = DateTime.Today.AddDays(30), Status = InvoiceStatus.Sent
        };
        fixture.Db.Invoices.Add(invoice);

        var request = await fixture.Service.EvaluateInvoiceAsync(invoice);
        await fixture.Db.SaveChangesAsync();

        Assert.NotNull(request);
        Assert.True(invoice.RequiresEstimateAuthorisation);
        Assert.Equal(InvoiceStatus.Draft, invoice.Status);
        Assert.Equal(estimate.Id, invoice.MatterCostEstimateId);
        var director = new ApplicationUser
        {
            FullName = "Director One", Email = "director@test.local", PasswordHash = "x",
            Role = UserRole.Admin, IsActive = true
        };
        fixture.Db.Users.Add(director);
        await fixture.Db.SaveChangesAsync();
        await fixture.Service.ApproveVarianceAsync(invoice.Id, director.Id, "Scope expansion approved after client discussion.");

        Assert.False(invoice.RequiresEstimateAuthorisation);
        Assert.True((await fixture.Db.InvoiceEstimateAuthorisations.SingleAsync()).IsApproved);
    }

    [Fact]
    public async Task New_matching_matter_auto_links_and_invoice_inside_range_needs_no_authorisation()
    {
        await using var fixture = await Fixture.CreateAsync();
        var (lawyer, client) = await fixture.SeedRateAsync();
        await fixture.SeedHistoryAsync(includeCosts: true, lawyer, client);
        var enquiry = await fixture.Service.BeginAsync("Commercial", client.Id);
        var estimate = await fixture.Service.CalculateAndLockAsync(enquiry.Id, Input(client.Email));
        var matter = new Case
        {
            CaseNumber = "LIVE-CASE-002", Title = "Automatically linked matter", CaseType = "commercial",
            ClientId = client.Id, LawyerId = lawyer.Id, Status = CaseStatus.Active
        };
        fixture.Db.Cases.Add(matter);
        await fixture.Db.SaveChangesAsync();

        await fixture.Service.TryAutoLinkAsync(matter.Id);

        Assert.Equal(CostEstimateStatus.Linked, estimate.Status);
        Assert.Equal(matter.Id, estimate.LinkedCaseId);
        var invoice = new Invoice
        {
            InvoiceNumber = "INV-WITHIN-RANGE", ClientId = client.Id, CaseId = matter.Id,
            Amount = (estimate.TotalLow + estimate.TotalHigh) / 2,
            TotalAmount = (estimate.TotalLow + estimate.TotalHigh) / 2,
            IssueDate = DateTime.Today, DueDate = DateTime.Today.AddDays(30), Status = InvoiceStatus.Draft
        };
        fixture.Db.Invoices.Add(invoice);
        var request = await fixture.Service.EvaluateInvoiceAsync(invoice);
        await fixture.Db.SaveChangesAsync();

        Assert.Null(request);
        Assert.False(invoice.RequiresEstimateAuthorisation);
        Assert.Equal(estimate.Id, invoice.MatterCostEstimateId);
        Assert.Empty(await fixture.Db.InvoiceEstimateAuthorisations.ToListAsync());
    }

    [Fact]
    public async Task Quote_is_refused_when_live_charge_out_rates_are_missing()
    {
        await using var fixture = await Fixture.CreateAsync();
        var (lawyer, client) = await fixture.SeedRateAsync();
        await fixture.SeedHistoryAsync(includeCosts: true, lawyer, client);
        var profile = await fixture.Db.LawyerProfiles.SingleAsync();
        profile.IsActive = false;
        await fixture.Db.SaveChangesAsync();

        var enquiry = await fixture.Service.BeginAsync("Commercial", client.Id);

        var refusal = await fixture.Db.MatterCostEstimates.SingleAsync(x => x.CostEstimateEnquiryId == enquiry.Id);
        Assert.Equal(CostEstimateStatus.Declined, refusal.Status);
        Assert.Contains("charge-out rates", refusal.DeclineReason);
    }

    [Fact]
    public async Task Estimate_cannot_be_linked_to_a_closed_matter()
    {
        await using var fixture = await Fixture.CreateAsync();
        var (lawyer, client) = await fixture.SeedRateAsync();
        await fixture.SeedHistoryAsync(includeCosts: true, lawyer, client);
        var enquiry = await fixture.Service.BeginAsync("Commercial", client.Id);
        var estimate = await fixture.Service.CalculateAndLockAsync(enquiry.Id, Input(client.Email));
        var closedMatter = await fixture.Db.Cases.FirstAsync(x => x.Status == CaseStatus.Closed);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.LinkToMatterAsync(estimate.Id, closedMatter.Id));

        Assert.Contains("active instructed matter", error.Message);
        Assert.Equal(CostEstimateStatus.Locked, estimate.Status);
    }

    [Fact]
    public async Task Empty_matter_type_is_rejected_before_an_enquiry_is_created()
    {
        await using var fixture = await Fixture.CreateAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.BeginAsync(null!, null));

        Assert.Empty(await fixture.Db.CostEstimateEnquiries.ToListAsync());
    }

    [Fact]
    public async Task Downloadable_estimate_is_a_complete_pdf()
    {
        var estimate = new MatterCostEstimate
        {
            Id = 42, Version = 1, ComparableMatterCount = 3, ProfessionalFeesLow = 10_000,
            ProfessionalFeesHigh = 15_000, DisbursementsLow = 1_000, DisbursementsHigh = 2_000,
            VatLow = 1_650, VatHigh = 2_550, TotalLow = 12_650, TotalHigh = 19_550,
            Enquiry = new CostEstimateEnquiry
            {
                ContactName = "Test Client", MatterType = "Commercial", MatterValue = 500_000,
                Urgency = MatterUrgency.Standard, DocumentReadiness = DocumentReadiness.Complete
            }
        };

        var bytes = new EstimatePdfService().Create(estimate);

        Assert.StartsWith("%PDF-1.4", System.Text.Encoding.ASCII.GetString(bytes, 0, 8));
        Assert.True(bytes.Length > 1_000);
        Assert.EndsWith("%%EOF", System.Text.Encoding.ASCII.GetString(bytes[^5..]));
    }

    private static CreateCostEstimateViewModel Input(string email = "client@test.local") => new()
    {
        ContactName = "Client One", Email = email, MatterType = "Commercial", MatterValue = 750_000,
        Urgency = MatterUrgency.Urgent, RequiresCourtProceedings = true,
        DocumentReadiness = DocumentReadiness.Partial
    };

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        public ApplicationDbContext Db { get; }
        public IMatterCostEstimateService Service { get; }

        private Fixture(SqliteConnection connection, ApplicationDbContext db)
        {
            this.connection = connection;
            Db = db;
            Service = new MatterCostEstimateService(db, new FakeNotifications());
        }

        public static async Task<Fixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
            await db.Database.EnsureCreatedAsync();
            return new Fixture(connection, db);
        }

        public async Task<(ApplicationUser Lawyer, Client Client)> SeedRateAsync()
        {
            var lawyer = new ApplicationUser
            {
                FullName = "Attorney One", Email = $"{Guid.NewGuid():N}@test.local", PasswordHash = "x",
                Role = UserRole.Lawyer, IsActive = true
            };
            var client = new Client { FirstName = "Client", LastName = "One", Email = "client@test.local", Phone = "1" };
            Db.AddRange(lawyer, client);
            await Db.SaveChangesAsync();
            Db.LawyerProfiles.Add(new LawyerProfile
            {
                UserId = lawyer.Id, HourlyRate = 2_500, BarNumber = "BAR-1", Bio = "",
                OfficeLocation = "Johannesburg", IsActive = true
            });
            await Db.SaveChangesAsync();
            return (lawyer, client);
        }

        public async Task SeedHistoryAsync(bool includeCosts, ApplicationUser? lawyer = null, Client? client = null)
        {
            lawyer ??= await Db.Users.SingleAsync(x => x.Role == UserRole.Lawyer);
            client ??= await Db.Clients.FirstAsync();
            for (var i = 0; i < 3; i++)
            {
                var matter = new Case
                {
                    CaseNumber = $"HIST-{i}", Title = $"Historic {i}", CaseType = "Commercial",
                    ClientId = client.Id, LawyerId = lawyer.Id, Status = CaseStatus.Closed
                };
                Db.Cases.Add(matter);
                await Db.SaveChangesAsync();
                if (includeCosts)
                    Db.TimeEntries.Add(new TimeEntry
                    {
                        CaseId = matter.Id, LawyerId = lawyer.Id, Description = "Historic legal work",
                        Date = DateTime.UtcNow.AddMonths(-i - 1), Hours = 10 + i, HourlyRate = 2_500,
                        TotalAmount = (10 + i) * 2_500, IsBillable = true, CreatedAt = DateTime.UtcNow
                    });
            }
            await Db.SaveChangesAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    private sealed class FakeNotifications : INotificationService
    {
        public Task QueueAsync(int userId, string type, string title, string message, string? actionUrl,
            string? deduplicationKey, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
