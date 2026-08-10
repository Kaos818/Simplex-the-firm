using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services.Billing;
using SimplexLawFirm.Services.Email;
using SimplexLawFirm.Models.Beneficiaries;
using Xunit;

namespace SimplexLawFirm.Tests;

public sealed class AppointmentBillingTests
{
    [Theory]
    [InlineData(AppointmentResponseStatus.Pending, EventStatus.Scheduled)]
    [InlineData(AppointmentResponseStatus.Rejected, EventStatus.Scheduled)]
    [InlineData(AppointmentResponseStatus.Accepted, EventStatus.Cancelled)]
    public async Task Non_billable_appointments_are_never_billed(AppointmentResponseStatus response, EventStatus status)
    {
        await using var fixture = await Fixture.Create();
        fixture.AddEvent(response, status, DateTime.UtcNow.AddHours(-1));
        await fixture.Db.SaveChangesAsync();
        Assert.Equal(0, await fixture.Service.ProcessDueAsync(DateTime.UtcNow));
        Assert.Empty(fixture.Db.AppointmentBillingRecords);
    }

    [Fact]
    public async Task Future_accepted_appointment_is_not_billed()
    {
        await using var fixture = await Fixture.Create();
        fixture.AddEvent(AppointmentResponseStatus.Accepted, EventStatus.Scheduled, DateTime.UtcNow.AddHours(1));
        await fixture.Db.SaveChangesAsync();
        Assert.Equal(0, await fixture.Service.ProcessDueAsync(DateTime.UtcNow));
    }

    [Fact]
    public async Task Accepted_ended_appointment_without_retainer_is_invoiced_once()
    {
        await using var fixture = await Fixture.Create();
        fixture.AddEvent(AppointmentResponseStatus.Accepted, EventStatus.Scheduled, DateTime.UtcNow.AddHours(-1));
        await fixture.Db.SaveChangesAsync();
        Assert.Equal(1, await fixture.Service.ProcessDueAsync(DateTime.UtcNow));
        Assert.Equal(0, await fixture.Service.ProcessDueAsync(DateTime.UtcNow));
        Assert.Single(fixture.Db.Invoices);
        Assert.Single(fixture.Db.AppointmentBillingRecords);
        Assert.Single(fixture.Email.Keys);
    }

    [Fact]
    public async Task Concurrent_workers_cannot_double_bill_the_same_appointment()
    {
        var database = $"billing-{Guid.NewGuid():N}";
        var connectionString = $"Data Source={database};Mode=Memory;Cache=Shared;Default Timeout=10";
        await using var anchor = new SqliteConnection(connectionString); await anchor.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connectionString).Options;
        await using (var seed = new TestDbContext(options))
        {
            await seed.Database.EnsureCreatedAsync();
            var client = new Client { FirstName="Concurrent", LastName="Client", Email="concurrent@example.test", Phone="1", Cases=[] };
            seed.Clients.Add(client); await seed.SaveChangesAsync();
            seed.CalendarEvents.Add(new CalendarEvent { Title="Concurrent appointment",Description="",Location="Office",MeetingLink="",Color="",CompletionNotes="",RecurrenceRule="",
                ClientId=client.Id,AppointmentFee=100,PaymentDueDays=7,StartDateTime=DateTime.UtcNow.AddHours(-2),EndDateTime=DateTime.UtcNow.AddHours(-1),
                ClientResponseStatus=AppointmentResponseStatus.Accepted,Status=EventStatus.Scheduled,Type=EventType.Appointment,Attendees=[],Reminders=[],ChildEvents=[] });
            await seed.SaveChangesAsync();
        }
        await using var db1 = new TestDbContext(options); await using var db2 = new TestDbContext(options);
        var first = new AppointmentBillingService(db1, new CapturingEmail());
        var second = new AppointmentBillingService(db2, new CapturingEmail());
        var outcomes = await Task.WhenAll(RunWorker(first), RunWorker(second));
        await using var verify = new TestDbContext(options);
        Assert.Equal(1, await verify.AppointmentBillingRecords.CountAsync());
        Assert.Equal(1, await verify.Invoices.CountAsync());
        Assert.Equal(1, outcomes.Sum());
    }

    private static async Task<int> RunWorker(AppointmentBillingService service)
    {
        try { return await service.ProcessDueAsync(DateTime.UtcNow); }
        catch (SqliteException ex) when (ex.SqliteErrorCode is 5 or 6) { return 0; }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException sqlite && sqlite.SqliteErrorCode is 5 or 6 or 19) { return 0; }
    }

    [Fact]
    public async Task Insufficient_trust_is_unchanged_and_full_fee_is_invoiced()
    {
        await using var fixture = await Fixture.Create();
        var retainer = fixture.AddRetainer();
        fixture.Db.TrustAccounts.Add(new TrustAccount { ClientId=fixture.Client.Id, Balance=50, TotalDeposited=50, Transactions=[] });
        fixture.AddEvent(AppointmentResponseStatus.Accepted, EventStatus.Scheduled, DateTime.UtcNow.AddHours(-1), retainer.Id);
        await fixture.Db.SaveChangesAsync();
        await fixture.Service.ProcessDueAsync(DateTime.UtcNow);
        Assert.Equal(50, (await fixture.Db.TrustAccounts.SingleAsync()).Balance);
        Assert.Equal(100, (await fixture.Db.Invoices.SingleAsync()).TotalAmount);
        Assert.Empty(fixture.Db.TrustTransactions);
    }

    [Fact]
    public async Task Sufficient_trust_creates_one_withdrawal_without_negative_balance()
    {
        await using var fixture = await Fixture.Create();
        var retainer = fixture.AddRetainer();
        fixture.Db.TrustAccounts.Add(new TrustAccount { ClientId=fixture.Client.Id, Balance=150, TotalDeposited=150, Transactions=[] });
        fixture.AddEvent(AppointmentResponseStatus.Accepted, EventStatus.Scheduled, DateTime.UtcNow.AddHours(-1), retainer.Id);
        await fixture.Db.SaveChangesAsync();
        await fixture.Service.ProcessDueAsync(DateTime.UtcNow);
        Assert.Equal(50, (await fixture.Db.TrustAccounts.SingleAsync()).Balance);
        Assert.Equal(50, (await fixture.Db.Retainers.SingleAsync()).AvailableBalance);
        Assert.Single(fixture.Db.TrustTransactions);
        Assert.True((await fixture.Db.Invoices.SingleAsync()).IsPaid);
    }

    [Fact]
    public async Task Paid_invoice_receives_no_penalty()
    {
        await using var fixture = await Fixture.Create();
        var value=fixture.AddEvent(AppointmentResponseStatus.Accepted,EventStatus.Scheduled,DateTime.UtcNow.AddDays(-10));
        value.BillingProcessed=true; value.LatePenaltyType=LatePenaltyType.FixedAmount; value.LatePenaltyValue=25;
        var invoice=new Invoice { ClientId=fixture.Client.Id,InvoiceNumber="PAID",Amount=100,TotalAmount=100,IssueDate=DateTime.UtcNow.AddDays(-10),
            DueDate=DateTime.UtcNow.AddDays(-5),CreatedAt=DateTime.UtcNow,CreatedDate=DateTime.UtcNow,Status=InvoiceStatus.Paid,IsPaid=true };
        fixture.Db.Invoices.Add(invoice); await fixture.Db.SaveChangesAsync(); value.GeneratedInvoiceId=invoice.Id; await fixture.Db.SaveChangesAsync();
        Assert.Equal(0,await fixture.Service.ApplyPenaltiesAsync(DateTime.UtcNow));
        Assert.Empty(fixture.Db.InvoicePenalties);
    }

    [Fact]
    public async Task Late_invoice_is_escalated_but_penalty_is_not_automatically_applied()
    {
        await using var fixture = await Fixture.Create();
        var value=fixture.AddEvent(AppointmentResponseStatus.Accepted,EventStatus.Scheduled,DateTime.UtcNow.AddDays(-10));
        value.BillingProcessed=true; value.LatePenaltyType=LatePenaltyType.Percentage; value.LatePenaltyValue=10; value.LatePenaltyGraceDays=1;
        var invoice=new Invoice { ClientId=fixture.Client.Id,InvoiceNumber="DUE",Amount=100,TotalAmount=100,IssueDate=DateTime.UtcNow.AddDays(-10),
            DueDate=DateTime.UtcNow.AddDays(-5),CreatedAt=DateTime.UtcNow,CreatedDate=DateTime.UtcNow,Status=InvoiceStatus.Sent };
        fixture.Db.Invoices.Add(invoice); await fixture.Db.SaveChangesAsync(); value.GeneratedInvoiceId=invoice.Id; await fixture.Db.SaveChangesAsync();
        Assert.Equal(1,await fixture.Service.ApplyPenaltiesAsync(DateTime.UtcNow));
        Assert.Equal(0,await fixture.Service.ApplyPenaltiesAsync(DateTime.UtcNow));
        Assert.Equal(100,(await fixture.Db.Invoices.SingleAsync()).TotalAmount);
        Assert.Equal(InvoiceStatus.Overdue, (await fixture.Db.Invoices.SingleAsync()).Status);
        Assert.Empty(fixture.Db.InvoicePenalties);
    }

    [Fact]
    public async Task Accountant_applies_only_preagreed_penalty_with_a_reason()
    {
        await using var fixture = await Fixture.Create();
        var accountant = new ApplicationUser { FullName="Test Accountant", Email="accounts@example.test", PasswordHash="x", Role=UserRole.Accountant, IsActive=true, AssignedCases=[] };
        fixture.Db.Users.Add(accountant);
        var appointment=fixture.AddEvent(AppointmentResponseStatus.Accepted,EventStatus.Scheduled,DateTime.UtcNow.AddDays(-10));
        appointment.BillingProcessed=true; appointment.LatePenaltyType=LatePenaltyType.Percentage; appointment.LatePenaltyValue=10; appointment.LatePenaltyGraceDays=1;
        var invoice=new Invoice { ClientId=fixture.Client.Id,InvoiceNumber="OVERDUE",Amount=100,TotalAmount=100,IssueDate=DateTime.UtcNow.AddDays(-10),DueDate=DateTime.UtcNow.AddDays(-5),CreatedAt=DateTime.UtcNow,CreatedDate=DateTime.UtcNow,Status=InvoiceStatus.Overdue, Payments=[] };
        fixture.Db.Invoices.Add(invoice); await fixture.Db.SaveChangesAsync(); appointment.GeneratedInvoiceId=invoice.Id; await fixture.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.ApplyPenaltyAsync(invoice.Id, accountant.Id, "vague", DateTime.UtcNow));
        await fixture.Service.ApplyPenaltyAsync(invoice.Id, accountant.Id, "Invoice remains unpaid after the agreed grace period.", DateTime.UtcNow);
        Assert.Equal(110, (await fixture.Db.Invoices.SingleAsync()).TotalAmount);
        var penalty = await fixture.Db.InvoicePenalties.SingleAsync();
        Assert.Equal(accountant.Id, penalty.AppliedByAccountantId);
        Assert.Contains("agreed grace period", penalty.Reason);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        public ApplicationDbContext Db { get; }
        public CapturingEmail Email { get; } = new();
        public AppointmentBillingService Service { get; }
        public Client Client { get; }
        private Fixture(SqliteConnection connection, ApplicationDbContext db, Client client)
        { this.connection=connection; Db=db; Client=client; Service=new(db,Email); }
        public static async Task<Fixture> Create()
        {
            var connection=new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
            var db=new TestDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
            await db.Database.EnsureCreatedAsync();
            var client=new Client { FirstName="Test", LastName="Client", Email="client@example.test", Phone="1", Cases=[] };
            db.Clients.Add(client); await db.SaveChangesAsync();
            return new Fixture(connection,db,client);
        }
        public Retainer AddRetainer()
        {
            var value=new Retainer { ClientId=Client.Id, Title="Active", ScopeOfWork="Test", SpecialTerms="", Status=RetainerStatus.Active,
                Amount=500, AmountPaid=150, AvailableBalance=150, PaymentSchedules=[],Payments=[],ActionLogs=[],Renewals=[] };
            Db.Retainers.Add(value); Db.SaveChanges(); return value;
        }
        public CalendarEvent AddEvent(AppointmentResponseStatus response, EventStatus status, DateTime end, int? retainerId=null)
        {
            var value=new CalendarEvent { Title="Consultation",Description="",Location="Office",MeetingLink="",Color="",CompletionNotes="",RecurrenceRule="",
                ClientId=Client.Id,RetainerId=retainerId,AppointmentFee=100,PaymentDueDays=7,StartDateTime=end.AddHours(-1),EndDateTime=end,
                ClientResponseStatus=response,Status=status,Type=EventType.Appointment,Attendees=[],Reminders=[],ChildEvents=[] };
            Db.CalendarEvents.Add(value); return value;
        }
        public async ValueTask DisposeAsync() { await Db.DisposeAsync(); await connection.DisposeAsync(); }
    }
    private sealed class TestDbContext(DbContextOptions<ApplicationDbContext> options) : ApplicationDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<CalendarEvent>().Property(x=>x.RowVersion).IsConcurrencyToken().ValueGeneratedNever();
            builder.Entity<Beneficiary>().Property(x=>x.RowVersion).IsConcurrencyToken().ValueGeneratedNever();
            builder.Entity<FacialVerificationSession>().Property(x=>x.RowVersion).IsConcurrencyToken().ValueGeneratedNever();
        }
    }
    private sealed class CapturingEmail : IEmailService
    {
        public HashSet<string> Keys { get; }=[];
        public Task QueueAsync(string to,string subject,string html,string text,string key,CancellationToken cancellationToken=default)
        { Keys.Add(key); return Task.CompletedTask; }
    }
}
