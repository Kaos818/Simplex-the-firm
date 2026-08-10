using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SimplexLawFirm.Controllers;
using SimplexLawFirm.Data;
using SimplexLawFirm.Infrastructure.Authorization;
using SimplexLawFirm.Models;
using SimplexLawFirm.Models.Beneficiaries;
using SimplexLawFirm.Services.CurrentUser;
using SimplexLawFirm.Services.Email;
using SimplexLawFirm.Services.Storage;
using SimplexLawFirm.Services.Verification;
using Xunit;

namespace SimplexLawFirm.Tests;

public sealed class StakeholderIsolationTests
{
    [Theory]
    [InlineData(nameof(DashboardController.Admin))]
    [InlineData(nameof(DashboardController.Lawyer))]
    [InlineData(nameof(DashboardController.Client))]
    [InlineData(nameof(DashboardController.Accountant))]
    [InlineData(nameof(DashboardController.Paralegal))]
    public void Every_stakeholder_dashboard_has_a_server_side_role_boundary(string action)
    {
        var method=typeof(DashboardController).GetMethod(action);
        Assert.NotNull(method);
        Assert.NotEmpty(method!.GetCustomAttributes(typeof(RequireSessionRoleAttribute),inherit:true));
    }

    [Fact]
    public void Client_calendar_has_an_independent_client_only_boundary()
    {
        Assert.NotEmpty(typeof(ClientCalendarController).GetCustomAttributes(typeof(RequireSessionRoleAttribute), true));
        Assert.Contains("_ClientLayout", File.ReadAllText(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Views/ClientCalendar/Index.cshtml"))));
    }

    [Theory]
    [InlineData("Director", true)]
    [InlineData("Admin", true)]
    [InlineData("Lawyer", false)]
    [InlineData("Client", false)]
    public void Only_director_identity_has_firm_wide_calendar_visibility(string role, bool expected) =>
        Assert.Equal(expected, CalendarController.HasFirmWideCalendarAccess(role));

    [Theory]
    [InlineData(nameof(ClientController.Index))]
    [InlineData(nameof(ClientController.Details))]
    [InlineData(nameof(ClientController.Create))]
    [InlineData(nameof(ClientController.Edit))]
    [InlineData(nameof(ClientController.Delete))]
    public void Staff_client_records_have_explicit_role_boundaries(string action)
    {
        Assert.Contains(typeof(ClientController).GetMethods().Where(x => x.Name == action),
            method => method.GetCustomAttributes(typeof(RequireSessionRoleAttribute), true).Any());
    }

    [Fact]
    public void Ajax_without_session_receives_401()
    {
        var http=new DefaultHttpContext();http.Request.Headers.Accept="application/json";SetSession(http,new MemorySession());
        var context=FilterContext(http);new RequireSessionRoleAttribute("Client").OnAuthorization(context);
        Assert.IsType<UnauthorizedResult>(context.Result);
    }

    [Fact]
    public void Stakeholder_with_wrong_role_is_forbidden()
    {
        var session=new MemorySession();session.SetInt32("UserId",1);session.SetString("UserRole","Lawyer");
        var http=new DefaultHttpContext();SetSession(http,session);var context=FilterContext(http);
        new RequireSessionRoleAttribute("Client").OnAuthorization(context);
        Assert.IsType<ForbidResult>(context.Result);
    }

    [Fact]
    public async Task Client_cannot_open_another_clients_beneficiary()
    {
        await using var connection=new SqliteConnection("Data Source=:memory:");await connection.OpenAsync();
        var db=new TestDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);await db.Database.EnsureCreatedAsync();
        var owner=new Client{FirstName="Owner",Email="owner@example.test",Phone="1",Cases=[]};
        var other=new Client{FirstName="Other",Email="other@example.test",Phone="1",Cases=[]};db.AddRange(owner,other);await db.SaveChangesAsync();
        var beneficiary=new Beneficiary{BenefactorClientId=other.Id,FirstName="Private",LastName="Stakeholder",Email="b@example.test",Phone="1",
            IdentificationNumber="1",RelationshipToBenefactor="Child",AssetAccessTerms="Private terms",EntitlementDescription="Private entitlement"};
        db.Beneficiaries.Add(beneficiary);await db.SaveChangesAsync();
        var controller=new BeneficiaryController(db,new FixedClient(owner),new NoEmail(),new ConfigurationBuilder().Build(),new NoStorage(),new NoVerification());
        Assert.IsType<NotFoundResult>(await controller.Details(beneficiary.Id,default));
    }

    [Fact]
    public async Task Beneficiary_termination_revokes_every_portal_credential_and_sends_email()
    {
        await using var connection=new SqliteConnection("Data Source=:memory:");await connection.OpenAsync();
        var db=new TestDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);await db.Database.EnsureCreatedAsync();
        var owner=new Client{FirstName="Owner",Email="owner@example.test",Phone="1",Cases=[]};var creator=new ApplicationUser{FullName="Owner User",Email=owner.Email,PasswordHash="x",Role=UserRole.Client,AssignedCases=[]};db.AddRange(owner,creator);await db.SaveChangesAsync();
        var beneficiary=new Beneficiary{BenefactorClientId=owner.Id,FirstName="Ben",LastName="Person",Email="ben@example.test",Phone="1",IdentificationNumber="1",RelationshipToBenefactor="Child",AssetAccessTerms="Terms",EntitlementDescription="Entitlement",Status=BeneficiaryStatus.Approved,PortalAccessEnabled=true,PortalPasswordHash=BCrypt.Net.BCrypt.HashPassword("Secret!123456")};
        db.Add(beneficiary);await db.SaveChangesAsync();
        var token=SimplexLawFirm.Services.Security.SecureToken.Create();db.BeneficiaryInvitations.Add(new(){BeneficiaryId=beneficiary.Id,CreatedByUserId=creator.Id,TokenHash=token.Hash,CreatedAtUtc=DateTime.UtcNow,ExpiresAtUtc=DateTime.UtcNow.AddDays(1)});
        db.FacialVerificationSessions.Add(new(){Id=Guid.NewGuid(),BeneficiaryId=beneficiary.Id,ChallengeJson="[]",Status=FacialVerificationStatus.ReadyForCapture,ConsentNoticeVersion="1",ExpiresAtUtc=DateTime.UtcNow.AddMinutes(10)});await db.SaveChangesAsync();
        var mail=new CaptureEmail();var http=new DefaultHttpContext();SetSession(http,new MemorySession());var controller=new BeneficiaryController(db,new FixedClient(owner),mail,new ConfigurationBuilder().Build(),new NoStorage(),new NoVerification()){ControllerContext=new ControllerContext{HttpContext=http}};

        Assert.IsType<RedirectToActionResult>(await controller.Deactivate(beneficiary.Id,"Removed by benefactor",default));

        Assert.Equal(BeneficiaryStatus.Suspended,beneficiary.Status);Assert.False(beneficiary.PortalAccessEnabled);Assert.Null(beneficiary.PortalPasswordHash);
        Assert.All(db.BeneficiaryInvitations,x=>Assert.NotNull(x.RevokedAtUtc));Assert.All(db.FacialVerificationSessions,x=>Assert.Equal(FacialVerificationStatus.Cancelled,x.Status));
        Assert.Contains(mail.Messages,x=>x.Subject.Contains("terminated")&&x.Text.Contains("revoked",StringComparison.OrdinalIgnoreCase));
    }

    private static AuthorizationFilterContext FilterContext(HttpContext http)=>new(
        new ActionContext(http,new RouteData(),new ActionDescriptor()),[]);
    private static void SetSession(HttpContext http,ISession session)=>http.Features.Set<ISessionFeature>(new Feature{Session=session});
    private sealed class Feature:ISessionFeature{public ISession Session{get;set;}=null!;}
    private sealed class MemorySession:ISession
    {
        private readonly Dictionary<string,byte[]> data=[];public bool IsAvailable=>true;public string Id=>"test";public IEnumerable<string> Keys=>data.Keys;
        public void Clear()=>data.Clear();public Task CommitAsync(CancellationToken ct=default)=>Task.CompletedTask;public Task LoadAsync(CancellationToken ct=default)=>Task.CompletedTask;
        public void Remove(string key)=>data.Remove(key);public void Set(string key,byte[] value)=>data[key]=value;public bool TryGetValue(string key,out byte[] value)=>data.TryGetValue(key,out value!);
    }
    private sealed class FixedClient(Client client):ICurrentClientService{public Task<Client?>GetAsync(CancellationToken ct=default)=>Task.FromResult<Client?>(client);}
    private sealed class NoEmail:IEmailService{public Task QueueAsync(string a,string b,string c,string d,string e,CancellationToken ct=default)=>Task.CompletedTask;}
    private sealed class CaptureEmail:IEmailService{public List<(string Subject,string Text)> Messages{get;}=[];public Task QueueAsync(string a,string subject,string c,string text,string e,CancellationToken ct=default){Messages.Add((subject,text));return Task.CompletedTask;}}
    private sealed class NoStorage:ISecureFileStorage
    {
        public Task<SecureStoredFile> StoreAsync(int id,IFormFile file,CancellationToken ct=default)=>throw new NotSupportedException();
        public Task<Stream> OpenReadAsync(string path,CancellationToken ct=default)=>throw new NotSupportedException();
    }
    private sealed class NoVerification:ILocalVerificationClient
    {
        public Task<string>AnalyseDocumentAsync(Stream a,string b,string c,bool d,bool e,CancellationToken ct)=>throw new NotSupportedException();
        public Task<string>VerifyFaceAsync(Stream a,IReadOnlyList<byte[]>b,Guid c,IReadOnlyList<string>d,IReadOnlyList<long>e,IReadOnlyList<int>f,CancellationToken ct)=>throw new NotSupportedException();
    }
    private sealed class TestDbContext(DbContextOptions<ApplicationDbContext> options):ApplicationDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder b){base.OnModelCreating(b);b.Entity<CalendarEvent>().Property(x=>x.RowVersion).IsConcurrencyToken().ValueGeneratedNever();
            b.Entity<Beneficiary>().Property(x=>x.RowVersion).IsConcurrencyToken().ValueGeneratedNever();b.Entity<FacialVerificationSession>().Property(x=>x.RowVersion).IsConcurrencyToken().ValueGeneratedNever();}
    }
}
