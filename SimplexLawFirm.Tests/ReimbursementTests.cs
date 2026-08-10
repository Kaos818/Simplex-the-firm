using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Controllers;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services;
using SimplexLawFirm.Services.Notifications;
using SimplexLawFirm.Services.Storage;
using SimplexLawFirm.ViewModels;
using Xunit;

namespace SimplexLawFirm.Tests;

public sealed class ReimbursementTests
{
    [Fact]
    public async Task Claim_is_refused_when_no_corresponding_activity_exists()
    {
        await using var f = await Fixture.CreateAsync();
        var claim = await f.Service.BeginAsync(f.Lawyer.Id, Input(f.Matter.Id, 400));
        Assert.Equal(ReimbursementStatus.RefusedValidation, claim.Status);
        Assert.Contains("No appointment, court event, or logged working hours", claim.ValidationFailureReason);
        Assert.Single(await f.Db.ReimbursementAuditEntries.ToListAsync());
    }

    [Fact]
    public async Task Only_assigned_attorney_can_claim()
    {
        await using var f = await Fixture.CreateAsync();
        var other = new ApplicationUser { FullName = "Other Lawyer", Email = "other@test", PasswordHash = "x", Role = UserRole.Lawyer, IsActive = true };
        f.Db.Users.Add(other);
        await f.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Service.BeginAsync(other.Id, Input(f.Matter.Id, 400)));
    }

    [Fact]
    public async Task Closed_matter_cannot_accept_a_reimbursement_claim()
    {
        await using var f = await Fixture.CreateAsync();
        f.Matter.Status = CaseStatus.Closed;
        await f.Db.SaveChangesAsync();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => f.Service.BeginAsync(f.Lawyer.Id, Input(f.Matter.Id, 400)));

        Assert.Contains("active matter", error.Message);
        Assert.Empty(await f.Db.ReimbursementClaims.ToListAsync());
    }

    [Fact]
    public async Task Scheduled_event_does_not_prove_work_took_place_but_completed_event_does()
    {
        await using var f = await Fixture.CreateAsync();
        var appointment = new CalendarEvent
        {
            Title = "Client appointment", Description = "", Location = "Office",
            StartDateTime = DateTime.Today.AddHours(9), EndDateTime = DateTime.Today.AddHours(10),
            CaseId = f.Matter.Id, Type = EventType.Appointment, Status = EventStatus.Scheduled,
            Color = "", MeetingLink = "", CompletionNotes = ""
        };
        f.Db.CalendarEvents.Add(appointment);
        await f.Db.SaveChangesAsync();
        var refused = await f.Service.BeginAsync(f.Lawyer.Id, Input(f.Matter.Id, 400));
        Assert.Equal(ReimbursementStatus.RefusedValidation, refused.Status);
        appointment.Status = EventStatus.Completed;
        appointment.ActualStartTime = appointment.StartDateTime;
        await f.Db.SaveChangesAsync();
        var accepted = await f.Service.BeginAsync(f.Lawyer.Id, Input(f.Matter.Id, 450));
        Assert.Equal(ReimbursementStatus.DraftProofRequired, accepted.Status);
        Assert.Equal(ReimbursementActivityType.Appointment, accepted.MatchedActivityType);
    }

    [Fact]
    public async Task Within_limit_recoverable_claim_is_auto_approved_payable_and_disbursement()
    {
        await using var f = await Fixture.CreateAsync();
        await f.AddTimeAsync();
        var claim = await f.Service.BeginAsync(f.Lawyer.Id, Input(f.Matter.Id, 1_000));
        claim = await f.Service.SubmitProofAsync(f.Lawyer.Id, claim.Id, Proof("travel.pdf"));

        Assert.Equal(ReimbursementStatus.AutoApproved, claim.Status);
        Assert.Equal(ExpenseClassification.ClientRecoverable, claim.Classification);
        Assert.NotNull(await f.Db.AttorneyReimbursementPayables.SingleOrDefaultAsync(x => x.ReimbursementClaimId == claim.Id));
        Assert.NotNull(await f.Db.MatterDisbursements.SingleOrDefaultAsync(x => x.ReimbursementClaimId == claim.Id));
    }

    [Fact]
    public async Task Firm_overhead_reimburses_attorney_but_is_not_recharged()
    {
        await using var f = await Fixture.CreateAsync();
        await f.AddTimeAsync();
        var input = Input(f.Matter.Id, 300);
        input.ExpenseType = ReimbursementExpenseType.Meals;
        var claim = await f.Service.BeginAsync(f.Lawyer.Id, input);
        claim = await f.Service.SubmitProofAsync(f.Lawyer.Id, claim.Id, Proof("meal.pdf"));

        Assert.Equal(ExpenseClassification.FirmOverhead, claim.Classification);
        Assert.NotNull(await f.Db.AttorneyReimbursementPayables.SingleOrDefaultAsync(x => x.ReimbursementClaimId == claim.Id));
        Assert.Null(await f.Db.MatterDisbursements.SingleOrDefaultAsync(x => x.ReimbursementClaimId == claim.Id));
    }

    [Fact]
    public async Task Duplicate_claim_is_refused()
    {
        await using var f = await Fixture.CreateAsync();
        await f.AddTimeAsync();
        var first = await f.Service.BeginAsync(f.Lawyer.Id, Input(f.Matter.Id, 800));
        await f.Service.SubmitProofAsync(f.Lawyer.Id, first.Id, Proof("first.pdf"));
        var second = await f.Service.BeginAsync(f.Lawyer.Id, Input(f.Matter.Id, 800));
        second = await f.Service.SubmitProofAsync(f.Lawyer.Id, second.Id, Proof("second.pdf"));
        Assert.Equal(ReimbursementStatus.RefusedValidation, second.Status);
        Assert.Contains("duplicates", second.ValidationFailureReason);
    }

    [Fact]
    public async Task Above_limit_claim_requires_director_and_refusal_records_reason()
    {
        await using var f = await Fixture.CreateAsync();
        await f.AddTimeAsync();
        var claim = await f.Service.BeginAsync(f.Lawyer.Id, Input(f.Matter.Id, 3_000));
        claim = await f.Service.SubmitProofAsync(f.Lawyer.Id, claim.Id, Proof("large.pdf"));
        Assert.Equal(ReimbursementStatus.PendingDirector, claim.Status);
        Assert.True(claim.ExceedsPolicyLimit);
        var director = new ApplicationUser { FullName = "Director", Email = "director@test", PasswordHash = "x", Role = UserRole.Admin, IsActive = true };
        f.Db.Users.Add(director);
        await f.Db.SaveChangesAsync();
        await f.Service.DecideAsync(claim.Id, director.Id, false, "Expense was not reasonably incurred.");
        Assert.Equal(ReimbursementStatus.Refused, claim.Status);
        Assert.Null(await f.Db.AttorneyReimbursementPayables.SingleOrDefaultAsync(x => x.ReimbursementClaimId == claim.Id));
    }

    [Fact]
    public async Task Director_approval_creates_payable_and_recoverable_disbursement_once()
    {
        await using var f = await Fixture.CreateAsync();
        await f.AddTimeAsync();
        var claim = await f.Service.BeginAsync(f.Lawyer.Id, Input(f.Matter.Id, 3_000));
        claim = await f.Service.SubmitProofAsync(f.Lawyer.Id, claim.Id, Proof("director-approve.pdf"));
        var director = new ApplicationUser { FullName = "Director", Email = "director-approve@test", PasswordHash = "x", Role = UserRole.Admin, IsActive = true };
        f.Db.Users.Add(director);
        await f.Db.SaveChangesAsync();

        await f.Service.DecideAsync(claim.Id, director.Id, true, "The expense was reasonably incurred for the matter.");

        Assert.Equal(ReimbursementStatus.Approved, claim.Status);
        Assert.NotNull(await f.Db.AttorneyReimbursementPayables.SingleOrDefaultAsync(x => x.ReimbursementClaimId == claim.Id));
        Assert.NotNull(await f.Db.MatterDisbursements.SingleOrDefaultAsync(x => x.ReimbursementClaimId == claim.Id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service.DecideAsync(claim.Id, director.Id, true, "Trying to approve the same claim again."));
    }

    [Fact]
    public async Task Matter_terms_override_default_recoverability()
    {
        await using var f = await Fixture.CreateAsync();
        await f.AddTimeAsync();
        f.Db.MatterExpenseTerms.Add(new MatterExpenseTerm
        {
            CaseId = f.Matter.Id, ExpenseType = ReimbursementExpenseType.Travel,
            Classification = ExpenseClassification.FirmOverhead, Reason = "Fixed fee includes attorney travel."
        });
        await f.Db.SaveChangesAsync();
        var claim = await f.Service.BeginAsync(f.Lawyer.Id, Input(f.Matter.Id, 1_000));
        claim = await f.Service.SubmitProofAsync(f.Lawyer.Id, claim.Id, Proof("fixed-fee-travel.pdf"));
        Assert.Equal(ExpenseClassification.FirmOverhead, claim.Classification);
        Assert.Contains("Fixed fee", claim.ClassificationReason);
        Assert.Empty(await f.Db.MatterDisbursements.ToListAsync());
    }

    [Fact]
    public async Task Submitted_claim_evidence_and_classification_are_immutable()
    {
        await using var f = await Fixture.CreateAsync();
        await f.AddTimeAsync();
        var claim = await f.Service.BeginAsync(f.Lawyer.Id, Input(f.Matter.Id, 1_000));
        claim = await f.Service.SubmitProofAsync(f.Lawyer.Id, claim.Id, Proof("locked.pdf"));
        claim.Amount = 1;
        claim.Classification = ExpenseClassification.FirmOverhead;
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Db.SaveChangesAsync());
    }

    [Fact]
    public async Task Non_director_cannot_decide_routed_claim()
    {
        await using var f = await Fixture.CreateAsync();
        await f.AddTimeAsync();
        var claim = await f.Service.BeginAsync(f.Lawyer.Id, Input(f.Matter.Id, 3_000));
        claim = await f.Service.SubmitProofAsync(f.Lawyer.Id, claim.Id, Proof("director-only.pdf"));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            f.Service.DecideAsync(claim.Id, f.Lawyer.Id, true, "Attempted unauthorised approval."));
        Assert.Equal(ReimbursementStatus.PendingDirector, claim.Status);
    }

    [Fact]
    public async Task Next_invoice_absorbs_pending_recoverable_disbursement_once()
    {
        await using var f = await Fixture.CreateAsync();
        await f.AddTimeAsync();
        var claim = await f.Service.BeginAsync(f.Lawyer.Id, Input(f.Matter.Id, 1_000));
        await f.Service.SubmitProofAsync(f.Lawyer.Id, claim.Id, Proof("invoice.pdf"));
        var controller = new BillingController(f.Db, new FakeEstimateService());
        controller.TempData = new TempDataDictionary(new DefaultHttpContext(), new FakeTempDataProvider());
        var invoice = new Invoice
        {
            CaseId = f.Matter.Id, ClientId = f.Client.Id, Amount = 5_000, TaxAmount = 750,
            Description = "Professional fees", IssueDate = DateTime.Today, DueDate = DateTime.Today.AddDays(30), Status = InvoiceStatus.Draft
        };

        await controller.CreateInvoice(invoice);

        Assert.Equal(6_000, invoice.Amount);
        Assert.Equal(6_750, invoice.TotalAmount);
        var disbursement = await f.Db.MatterDisbursements.SingleAsync();
        Assert.Equal(invoice.Id, disbursement.InvoiceId);
        Assert.NotNull(disbursement.InvoicedAtUtc);
    }

    private static BeginReimbursementViewModel Input(int caseId, decimal amount) => new()
    {
        CaseId = caseId, ExpenseType = ReimbursementExpenseType.Travel,
        ExpenseDate = DateTime.Today, Amount = amount, Description = "Travel required for the client meeting."
    };

    private static IFormFile Proof(string name)
    {
        var bytes = System.Text.Encoding.ASCII.GetBytes("%PDF-1.4 proof");
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "Proof", name) { Headers = new HeaderDictionary(), ContentType = "application/pdf" };
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        public ApplicationDbContext Db { get; }
        public IReimbursementService Service { get; }
        public ApplicationUser Lawyer { get; private set; } = null!;
        public Client Client { get; private set; } = null!;
        public Case Matter { get; private set; } = null!;
        private Fixture(SqliteConnection connection, ApplicationDbContext db)
        {
            this.connection = connection;
            Db = db;
            Service = new ReimbursementService(db, new FakeStorage(), new FakeNotifications());
        }
        public static async Task<Fixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
            await db.Database.EnsureCreatedAsync();
            var f = new Fixture(connection, db);
            f.Lawyer = new ApplicationUser { FullName = "Attorney One", Email = "lawyer@test", PasswordHash = "x", Role = UserRole.Lawyer, IsActive = true };
            f.Client = new Client { FirstName = "Client", LastName = "One", Email = "client@test", Phone = "1" };
            db.AddRange(f.Lawyer, f.Client);
            await db.SaveChangesAsync();
            f.Matter = new Case { CaseNumber = "MAT-001", Title = "Claim matter", CaseType = "Commercial", ClientId = f.Client.Id, LawyerId = f.Lawyer.Id, Status = CaseStatus.Active };
            db.Cases.Add(f.Matter);
            await db.SaveChangesAsync();
            return f;
        }
        public async Task AddTimeAsync()
        {
            Db.TimeEntries.Add(new TimeEntry
            {
                CaseId = Matter.Id, LawyerId = Lawyer.Id, Description = "Client meeting",
                Date = DateTime.Today.AddHours(10), Hours = 1, HourlyRate = 2_000, TotalAmount = 2_000, IsBillable = true
            });
            await Db.SaveChangesAsync();
        }
        public async ValueTask DisposeAsync() { await Db.DisposeAsync(); await connection.DisposeAsync(); }
    }

    private sealed class FakeStorage : IReimbursementProofStorage
    {
        public Task<SecureStoredFile> StoreAsync(int claimId, IFormFile file, CancellationToken ct = default) =>
            Task.FromResult(new SecureStoredFile(file.FileName, "proof.bin", $"{claimId}/proof.bin", "application/pdf", file.Length, $"HASH-{file.FileName}"));
        public Task<Stream> OpenReadAsync(string relativePath, CancellationToken ct = default) => Task.FromResult<Stream>(new MemoryStream());
        public Task DeleteAsync(string relativePath, CancellationToken ct = default) => Task.CompletedTask;
    }
    private sealed class FakeNotifications : INotificationService
    {
        public Task QueueAsync(int userId, string type, string title, string message, string? actionUrl, string? deduplicationKey, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
    private sealed class FakeEstimateService : IMatterCostEstimateService
    {
        public Task<CostEstimateEnquiry> BeginAsync(string matterType, int? clientId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<MatterCostEstimate> CalculateAndLockAsync(int enquiryId, CreateCostEstimateViewModel input, CancellationToken ct = default) => throw new NotImplementedException();
        public Task LinkToMatterAsync(int estimateId, int caseId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task TryAutoLinkAsync(int caseId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<InvoiceEstimateAuthorisation?> EvaluateInvoiceAsync(Invoice invoice, CancellationToken ct = default) => Task.FromResult<InvoiceEstimateAuthorisation?>(null);
        public Task ApproveVarianceAsync(int invoiceId, int directorId, string reason, CancellationToken ct = default) => throw new NotImplementedException();
    }
    private sealed class FakeTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
