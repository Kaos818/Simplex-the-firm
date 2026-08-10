using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Controllers;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using SimplexLawFirm.Models.Beneficiaries;
using SimplexLawFirm.Services.Email;
using SimplexLawFirm.Services.Notifications;
using Xunit;

namespace SimplexLawFirm.Tests;

public sealed class BeneficiaryApprovalTests
{
    [Fact]
    public async Task Approval_fails_when_required_document_is_missing()
    {
        await using var f=await Fixture.Create();
        Assert.IsType<BadRequestObjectResult>(await f.Controller.Approve(f.Beneficiary.Id,null,default));
    }

    [Theory]
    [InlineData(DocumentPreScreenStatus.Pending)]
    [InlineData(DocumentPreScreenStatus.Processing)]
    [InlineData(DocumentPreScreenStatus.ResubmissionRequired)]
    [InlineData(DocumentPreScreenStatus.FailedTechnicalProcessing)]
    public async Task Approval_fails_when_latest_document_is_not_reviewable(DocumentPreScreenStatus status)
    {
        await using var f=await Fixture.Create(); f.AddDocument(status); f.AddFace(FacialVerificationStatus.Verified); await f.Db.SaveChangesAsync();
        Assert.IsType<BadRequestObjectResult>(await f.Controller.Approve(f.Beneficiary.Id,null,default));
    }

    [Fact]
    public async Task Approval_fails_when_face_verification_failed()
    {
        await using var f=await Fixture.Create(); f.AddDocument(DocumentPreScreenStatus.Passed); f.AddFace(FacialVerificationStatus.FaceNotMatched); await f.Db.SaveChangesAsync();
        Assert.IsType<BadRequestObjectResult>(await f.Controller.Approve(f.Beneficiary.Id,null,default));
    }

    [Fact]
    public async Task Manual_face_review_requires_administrator_reason()
    {
        await using var f=await Fixture.Create(); f.AddDocument(DocumentPreScreenStatus.ManualReviewRequired); f.AddFace(FacialVerificationStatus.ManualReviewRequired); await f.Db.SaveChangesAsync();
        Assert.IsType<BadRequestObjectResult>(await f.Controller.Approve(f.Beneficiary.Id," ",default));
        Assert.IsType<RedirectToActionResult>(await f.Controller.Approve(f.Beneficiary.Id,"Reviewed identity evidence in person.",default));
        Assert.Equal(BeneficiaryStatus.Approved,f.Beneficiary.Status);
        Assert.Single(f.Db.AuditEntries);
    }

    private sealed class Fixture:IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        public ApplicationDbContext Db { get; }
        public AdminBeneficiaryController Controller { get; }
        public Beneficiary Beneficiary { get; }
        public BeneficiaryDocumentRequirement Requirement { get; }
        private Fixture(SqliteConnection connection,ApplicationDbContext db,Beneficiary beneficiary,BeneficiaryDocumentRequirement requirement,AdminBeneficiaryController controller)
        {this.connection=connection;Db=db;Beneficiary=beneficiary;Requirement=requirement;Controller=controller;}
        public static async Task<Fixture> Create()
        {
            var connection=new SqliteConnection("Data Source=:memory:");await connection.OpenAsync();
            var db=new TestDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);await db.Database.EnsureCreatedAsync();
            var admin=new ApplicationUser{FullName="Admin",Email="admin@example.test",PasswordHash="x",Role=UserRole.Admin,CreatedAt=DateTime.UtcNow,AssignedCases=[]};
            var client=new Client{FirstName="Owner",Email="owner@example.test",Phone="1",Cases=[]};db.AddRange(admin,client);await db.SaveChangesAsync();
            var beneficiary=new Beneficiary{BenefactorClientId=client.Id,FirstName="Ben",LastName="Person",Email="ben@example.test",Phone="1",IdentificationNumber="1",RelationshipToBenefactor="Child",Status=BeneficiaryStatus.UnderAdminReview};
            var requirement=new BeneficiaryDocumentRequirement{Code="SA_ID",DisplayName="ID",Description="ID",IsRequired=true};
            db.AddRange(beneficiary,requirement);await db.SaveChangesAsync();
            db.BeneficiaryRequirementAssignments.Add(new(){BeneficiaryId=beneficiary.Id,RequirementId=requirement.Id,IsRequired=true});await db.SaveChangesAsync();
            var context=new DefaultHttpContext();var session=new MemorySession();session.SetInt32("UserId",admin.Id);context.Features.Set<ISessionFeature>(new SessionFeature{Session=session});
            var controller=new AdminBeneficiaryController(db,new NoEmail(),new NoNotifications()){ControllerContext=new ControllerContext{HttpContext=context}};
            return new Fixture(connection,db,beneficiary,requirement,controller);
        }
        public void AddDocument(DocumentPreScreenStatus status)=>Db.BeneficiaryDocuments.Add(new(){BeneficiaryId=Beneficiary.Id,RequirementId=Requirement.Id,
            OriginalFileName="id.jpg",StoredFileName="x",RelativeStoragePath="1/x",ContentType="image/jpeg",Sha256Hash=new string('a',64),SizeBytes=1,PreScreenStatus=status});
        public void AddFace(FacialVerificationStatus status)=>Db.FacialVerificationSessions.Add(new(){Id=Guid.NewGuid(),BeneficiaryId=Beneficiary.Id,
            ChallengeJson="[]",Status=status,ConsentGranted=true,ConsentNoticeVersion="1",ExpiresAtUtc=DateTime.UtcNow.AddMinutes(5)});
        public async ValueTask DisposeAsync(){await Db.DisposeAsync();await connection.DisposeAsync();}
    }
    private sealed class TestDbContext(DbContextOptions<ApplicationDbContext> options):ApplicationDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder b){base.OnModelCreating(b);b.Entity<CalendarEvent>().Property(x=>x.RowVersion).IsConcurrencyToken().ValueGeneratedNever();
            b.Entity<Beneficiary>().Property(x=>x.RowVersion).IsConcurrencyToken().ValueGeneratedNever();b.Entity<FacialVerificationSession>().Property(x=>x.RowVersion).IsConcurrencyToken().ValueGeneratedNever();}
    }
    private sealed class SessionFeature:ISessionFeature{public ISession Session{get;set;}=null!;}
    private sealed class NoEmail:IEmailService
    {
        public Task QueueAsync(string to,string subject,string html,string text,string key,CancellationToken ct=default)=>Task.CompletedTask;
    }
    private sealed class NoNotifications:INotificationService
    {
        public Task QueueAsync(int userId,string type,string title,string message,string? actionUrl,string? key,CancellationToken ct=default)=>Task.CompletedTask;
    }
    private sealed class MemorySession:ISession
    {
        private readonly Dictionary<string,byte[]> values=[];public bool IsAvailable=>true;public string Id=>"test";public IEnumerable<string> Keys=>values.Keys;
        public void Clear()=>values.Clear();public Task CommitAsync(CancellationToken cancellationToken=default)=>Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken=default)=>Task.CompletedTask;public void Remove(string key)=>values.Remove(key);
        public void Set(string key,byte[] value)=>values[key]=value;public bool TryGetValue(string key,out byte[] value)=>values.TryGetValue(key,out value!);
    }
}
