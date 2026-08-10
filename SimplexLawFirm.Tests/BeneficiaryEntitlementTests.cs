using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using SimplexLawFirm.Models.Beneficiaries;
using SimplexLawFirm.Services;
using SimplexLawFirm.Services.Notifications;
using SimplexLawFirm.Services.Email;
using Xunit;

namespace SimplexLawFirm.Tests;

public sealed class BeneficiaryEntitlementTests
{
    [Fact]
    public async Task Request_cannot_exceed_personal_entitlement_even_when_trust_holds_more()
    {
        await using var fixture=await Fixture.Create(100m,10_000m);
        var exception=await Assert.ThrowsAsync<InvalidOperationException>(()=>fixture.Service.RequestDisbursementAsync(fixture.Beneficiary.Id,"Education",101m,"Tuition"));
        Assert.Contains("available beneficiary entitlement",exception.Message);
        Assert.Empty(await fixture.Db.BeneficiaryTrustDisbursementRequests.ToListAsync());
    }

    [Fact]
    public async Task Prior_requests_reduce_only_that_beneficiarys_remaining_entitlement()
    {
        await using var fixture=await Fixture.Create(100m,10_000m);
        await fixture.Service.RequestDisbursementAsync(fixture.Beneficiary.Id,"Education",60m,"First tuition payment");
        await Assert.ThrowsAsync<InvalidOperationException>(()=>fixture.Service.RequestDisbursementAsync(fixture.Beneficiary.Id,"Education",41m,"Exceeds remaining amount"));
        var second=await fixture.Service.RequestDisbursementAsync(fixture.Beneficiary.Id,"Education",40m,"Remaining tuition payment");
        Assert.Equal(100m,(await fixture.Db.BeneficiaryTrustDisbursementRequests.SumAsync(x=>x.Amount)));
        Assert.Equal(100m,second.EntitlementLimitSnapshot);
    }

    [Fact]
    public async Task Director_decision_requires_reason_and_is_final()
    {
        await using var fixture=await Fixture.Create(500m,10_000m);
        var request=await fixture.Service.RequestDisbursementAsync(fixture.Beneficiary.Id,"Education",100m,"Approved tuition payment");
        var director=await fixture.Db.Users.SingleAsync(x=>x.Role==UserRole.Director);
        await Assert.ThrowsAsync<ArgumentException>(()=>fixture.Service.DecideDisbursementAsync(director.Id,request.Id,true,"short"));
        request=await fixture.Service.DecideDisbursementAsync(director.Id,request.Id,true,"Entitlement and supporting evidence verified.");
        Assert.Equal(TrustDisbursementStatus.Approved,request.Status);
        Assert.NotNull(request.DecidedAtUtc);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>fixture.Service.DecideDisbursementAsync(director.Id,request.Id,false,"A second decision must not be accepted."));
    }

    private sealed class Fixture:IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        public ApplicationDbContext Db{get;}
        public Beneficiary Beneficiary{get;}
        public OperationalUseCaseService Service{get;}
        private Fixture(SqliteConnection connection,ApplicationDbContext db,Beneficiary beneficiary){this.connection=connection;Db=db;Beneficiary=beneficiary;Service=new(db,new NotificationService(db),new EmailOutboxService(db),Options.Create(new LegalResearchOptions()));}
        public static async Task<Fixture>Create(decimal limit,decimal trustBalance)
        {
            var connection=new SqliteConnection("Data Source=:memory:");await connection.OpenAsync();
            var db=new TestDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);await db.Database.EnsureCreatedAsync();
            var client=new Client{FirstName="Owner",Email="owner@test",Phone="1",Cases=[]};var director=new ApplicationUser{FullName="Director",Email="director@test",PasswordHash="x",Role=UserRole.Director,IsActive=true,AssignedCases=[]};db.AddRange(client,director);await db.SaveChangesAsync();
            var beneficiary=new Beneficiary{BenefactorClientId=client.Id,FirstName="Ben",LastName="One",Email="ben@test",Phone="1",IdentificationNumber="1",RelationshipToBenefactor="Child",AssetAccessTerms="Education only",PermittedAssetPurposes="Education",EntitlementDescription="Cash education entitlement",EntitlementAmountLimit=limit,Status=BeneficiaryStatus.Approved,PortalAccessEnabled=true};
            db.AddRange(beneficiary,new TrustAccount{ClientId=client.Id,Balance=trustBalance,TotalDeposited=trustBalance,LastUpdated=DateTime.UtcNow,Transactions=[]});await db.SaveChangesAsync();
            db.FacialVerificationSessions.Add(new(){Id=Guid.NewGuid(),BeneficiaryId=beneficiary.Id,ChallengeJson="[]",Status=FacialVerificationStatus.Verified,ConsentGranted=true,ConsentNoticeVersion="1",ExpiresAtUtc=DateTime.UtcNow.AddMinutes(5)});await db.SaveChangesAsync();
            return new(connection,db,beneficiary);
        }
        public async ValueTask DisposeAsync(){await Db.DisposeAsync();await connection.DisposeAsync();}
    }
    private sealed class TestDbContext(DbContextOptions<ApplicationDbContext> options):ApplicationDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder builder){base.OnModelCreating(builder);builder.Entity<CalendarEvent>().Property(x=>x.RowVersion).ValueGeneratedNever();builder.Entity<Beneficiary>().Property(x=>x.RowVersion).ValueGeneratedNever();builder.Entity<FacialVerificationSession>().Property(x=>x.RowVersion).ValueGeneratedNever();}
    }
}
