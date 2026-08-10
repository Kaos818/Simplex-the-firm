using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MailKit;
using MailKit.Net.Smtp;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using SimplexLawFirm.Models.Beneficiaries;
using SimplexLawFirm.Services.Email;
using Xunit;

namespace SimplexLawFirm.Tests;

public sealed class EmailOutboxTests
{
    [Fact]
    public async Task Deduplication_key_prevents_duplicate_email()
    {
        await using var f=await Fixture.Create("Development");
        var service=new EmailOutboxService(f.Db);
        await service.QueueAsync("a@example.test","Subject","<p>x</p>","x","same");
        await f.Db.SaveChangesAsync();
        await service.QueueAsync("a@example.test","Subject","<p>x</p>","x","same");
        await f.Db.SaveChangesAsync();
        Assert.Single(f.Db.EmailOutboxMessages);
    }

    [Fact]
    public async Task Development_delivery_marks_message_sent()
    {
        await using var f=await Fixture.Create("Development");
        f.Db.EmailOutboxMessages.Add(Message());await f.Db.SaveChangesAsync();
        await f.Worker.DeliverOnceAsync(default);
        f.Db.ChangeTracker.Clear();
        Assert.Equal(EmailOutboxStatus.Sent,(await f.Db.EmailOutboxMessages.SingleAsync()).Status);
    }

    [Fact]
    public async Task Smtp_failure_schedules_retry_without_exposing_credentials()
    {
        await using var f=await Fixture.Create("Production",new EmailOptions{Username="smtp-user",Password="super-secret"});
        f.Db.EmailOutboxMessages.Add(Message());await f.Db.SaveChangesAsync();
        await f.Worker.DeliverOnceAsync(default);
        f.Db.ChangeTracker.Clear();
        var item=await f.Db.EmailOutboxMessages.SingleAsync();
        Assert.Equal(EmailOutboxStatus.RetryScheduled,item.Status);Assert.Equal(1,item.AttemptCount);Assert.NotNull(item.NextAttemptAtUtc);
        Assert.DoesNotContain("super-secret",item.LastError??"");Assert.DoesNotContain("smtp-user",item.LastError??"");
        Assert.Equal("Failure [redacted] [redacted]",EmailOutboxWorker.Sanitize("Failure super-secret smtp-user",new(){Username="smtp-user",Password="super-secret"}));
    }

    [Fact]
    public void Permanent_smtp_recipient_failure_is_not_retried()
    {
        var failure = new SmtpCommandException(SmtpErrorCode.RecipientNotAccepted, SmtpStatusCode.MailboxUnavailable, "550 5.1.1 recipient disabled");
        Assert.True(EmailOutboxWorker.IsPermanentDeliveryFailure(failure));
    }

    [Fact]
    public async Task Invalid_recipient_is_rejected_before_it_enters_the_outbox()
    {
        await using var f = await Fixture.Create("Development");
        var service = new EmailOutboxService(f.Db);
        await Assert.ThrowsAsync<ArgumentException>(() => service.QueueAsync("not an email", "Subject", "<p>x</p>", "x", "invalid"));
        Assert.Empty(f.Db.EmailOutboxMessages);
    }

    private static EmailOutboxMessage Message()=>new(){ToAddress="a@example.test",Subject="Test",HtmlBody="<p>Test</p>",TextBody="Test",DeduplicationKey=Guid.NewGuid().ToString("N")};
    private sealed class Fixture:IAsyncDisposable
    {
        private readonly SqliteConnection connection;private readonly ServiceProvider provider;public ApplicationDbContext Db{get;}public EmailOutboxWorker Worker{get;}
        private Fixture(SqliteConnection c,ServiceProvider p,ApplicationDbContext db,EmailOutboxWorker worker){connection=c;provider=p;Db=db;Worker=worker;}
        public static async Task<Fixture>Create(string environment,EmailOptions? options=null)
        {
            var c=new SqliteConnection("Data Source=:memory:");await c.OpenAsync();
            var dbOptions=new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(c).Options;
            await using(var setup=new TestDbContext(dbOptions)){await setup.Database.EnsureCreatedAsync();}
            var services=new ServiceCollection();services.AddScoped<ApplicationDbContext>(_=>new TestDbContext(dbOptions));var provider=services.BuildServiceProvider();
            var db=provider.GetRequiredService<ApplicationDbContext>();var env=new TestEnvironment(environment);
            var worker=new EmailOutboxWorker(provider.GetRequiredService<IServiceScopeFactory>(),Options.Create(options??new EmailOptions()),env,NullLogger<EmailOutboxWorker>.Instance);
            return new Fixture(c,provider,db,worker);
        }
        public async ValueTask DisposeAsync(){await Db.DisposeAsync();await provider.DisposeAsync();await connection.DisposeAsync();}
    }
    private sealed class TestDbContext(DbContextOptions<ApplicationDbContext> options):ApplicationDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder b){base.OnModelCreating(b);b.Entity<CalendarEvent>().Property(x=>x.RowVersion).IsConcurrencyToken().ValueGeneratedNever();
            b.Entity<Beneficiary>().Property(x=>x.RowVersion).IsConcurrencyToken().ValueGeneratedNever();b.Entity<FacialVerificationSession>().Property(x=>x.RowVersion).IsConcurrencyToken().ValueGeneratedNever();}
    }
    private sealed class TestEnvironment(string name):IWebHostEnvironment
    {
        public string EnvironmentName{get;set;}=name;public string ApplicationName{get;set;}="Tests";
        public string WebRootPath{get;set;}=Path.Combine(Path.GetTempPath(),"simplex-email-tests","wwwroot");
        public string ContentRootPath{get;set;}=Path.Combine(Path.GetTempPath(),"simplex-email-tests",Guid.NewGuid().ToString("N"));
        public IFileProvider WebRootFileProvider{get;set;}=new NullFileProvider();public IFileProvider ContentRootFileProvider{get;set;}=new NullFileProvider();
    }
}
