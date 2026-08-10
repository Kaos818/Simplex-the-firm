using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Controllers;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using SimplexLawFirm.Models.Beneficiaries;
using SimplexLawFirm.Services.Email;
using SimplexLawFirm.Services.Notifications;
using SimplexLawFirm.Services.Security;
using SimplexLawFirm.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace SimplexLawFirm.Tests;

public sealed class AppointmentResponseTests
{
    [Fact]
    public async Task Opening_confirmation_get_does_not_mutate_response()
    {
        await using var fixture=await Fixture.Create();
        var result=await fixture.Controller.Confirm(fixture.Token,AppointmentResponseStatus.Accepted,default);
        Assert.IsType<ViewResult>(result);
        Assert.Equal(AppointmentResponseStatus.Pending,(await fixture.Db.CalendarEvents.SingleAsync()).ClientResponseStatus);
        Assert.Null((await fixture.Db.AppointmentInvitations.SingleAsync()).UsedAtUtc);
    }

    [Theory]
    [InlineData(AppointmentResponseStatus.Accepted)]
    [InlineData(AppointmentResponseStatus.Rejected)]
    public async Task Valid_post_records_response_once_and_deduplicates_staff(AppointmentResponseStatus response)
    {
        await using var fixture=await Fixture.Create(sameCreatorAndLawyer:true);
        var result=await fixture.Controller.Confirm(fixture.Token,response,"Confirmed",default);
        Assert.IsType<ViewResult>(result);
        Assert.Equal(response,(await fixture.Db.CalendarEvents.SingleAsync()).ClientResponseStatus);
        Assert.NotNull((await fixture.Db.AppointmentInvitations.SingleAsync()).UsedAtUtc);
        Assert.Single(fixture.Db.SystemNotifications);
        Assert.Single(fixture.Email.Keys);
        Assert.IsType<BadRequestObjectResult>(await fixture.Controller.Confirm(fixture.Token,response,null,default));
    }

    [Theory]
    [InlineData(true,false)]
    [InlineData(false,true)]
    public async Task Expired_or_revoked_token_is_rejected(bool expired,bool revoked)
    {
        await using var fixture=await Fixture.Create(expired:expired,revoked:revoked);
        Assert.IsType<ViewResult>(await fixture.Controller.Confirm(fixture.Token,AppointmentResponseStatus.Accepted,default));
        var view=(ViewResult)await fixture.Controller.Confirm(fixture.Token,AppointmentResponseStatus.Accepted,default);
        Assert.Equal("Invalid",view.ViewName);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        public ApplicationDbContext Db { get; }
        public CapturingEmail Email { get; }=new();
        public AppointmentResponseController Controller { get; }
        public string Token { get; }
        private Fixture(SqliteConnection connection,ApplicationDbContext db,string token)
        {
            this.connection=connection; Db=db; Token=token;
            var notifications = new NotificationService(db);
            Controller=new AppointmentResponseController(db,notifications,Email,
                new VulnerableClientService(db, notifications, Options.Create(new VulnerableClientOptions())))
            { ControllerContext=new ControllerContext { HttpContext=new DefaultHttpContext() } };
        }
        public static async Task<Fixture> Create(bool sameCreatorAndLawyer=false,bool expired=false,bool revoked=false)
        {
            var connection=new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
            var db=new TestDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
            await db.Database.EnsureCreatedAsync();
            var creator=new ApplicationUser { FullName="Creator",Email="creator@example.test",PasswordHash="x",Role=UserRole.Lawyer,CreatedAt=DateTime.UtcNow,AssignedCases=[] };
            var lawyer=sameCreatorAndLawyer?creator:new ApplicationUser { FullName="Lawyer",Email="lawyer@example.test",PasswordHash="x",Role=UserRole.Lawyer,CreatedAt=DateTime.UtcNow,AssignedCases=[] };
            db.Users.Add(creator); if(!sameCreatorAndLawyer) db.Users.Add(lawyer);
            var client=new Client { FirstName="Client",Email="client@example.test",Phone="1",Cases=[] }; db.Clients.Add(client); await db.SaveChangesAsync();
            var value=new CalendarEvent { Title="Appointment",Description="",Location="Office",MeetingLink="",Color="",CompletionNotes="",RecurrenceRule="",
                ClientId=client.Id,CreatedByUserId=creator.Id,AssignedToUserId=lawyer.Id,AppointmentFee=100,StartDateTime=DateTime.UtcNow.AddDays(1),
                EndDateTime=DateTime.UtcNow.AddDays(1).AddHours(1),ClientResponseStatus=AppointmentResponseStatus.Pending,Status=EventStatus.Scheduled,
                Type=EventType.Appointment,Attendees=[],Reminders=[],ChildEvents=[] };
            db.CalendarEvents.Add(value); await db.SaveChangesAsync();
            var token=SecureToken.Create();
            db.AppointmentInvitations.Add(new AppointmentInvitation { CalendarEventId=value.Id,TokenHash=token.Hash,CreatedAtUtc=DateTime.UtcNow,
                ExpiresAtUtc=expired?DateTime.UtcNow.AddMinutes(-1):DateTime.UtcNow.AddHours(1),RevokedAtUtc=revoked?DateTime.UtcNow:null });
            await db.SaveChangesAsync(); return new Fixture(connection,db,token.Raw);
        }
        public async ValueTask DisposeAsync(){await Db.DisposeAsync();await connection.DisposeAsync();}
    }
    private sealed class TestDbContext(DbContextOptions<ApplicationDbContext> options):ApplicationDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder builder){base.OnModelCreating(builder);
            builder.Entity<CalendarEvent>().Property(x=>x.RowVersion).IsConcurrencyToken().ValueGeneratedNever();
            builder.Entity<Beneficiary>().Property(x=>x.RowVersion).IsConcurrencyToken().ValueGeneratedNever();
            builder.Entity<FacialVerificationSession>().Property(x=>x.RowVersion).IsConcurrencyToken().ValueGeneratedNever();}
    }
    private sealed class CapturingEmail:IEmailService
    {
        public HashSet<string> Keys { get; }=[];
        public Task QueueAsync(string to,string subject,string html,string text,string key,CancellationToken cancellationToken=default){Keys.Add(key);return Task.CompletedTask;}
    }
}
