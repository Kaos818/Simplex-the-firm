using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Controllers;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using SimplexLawFirm.Models.Beneficiaries;
using SimplexLawFirm.Services.Security;
using SimplexLawFirm.Services.Storage;
using SimplexLawFirm.Services.Verification;
using Xunit;

namespace SimplexLawFirm.Tests;

public sealed class BeneficiaryInvitationTests
{
    [Fact]
    public void Generated_portal_passwords_are_unique_and_strong()
    {
        var passwords = Enumerable.Range(0, 100).Select(_ => BeneficiaryController.GeneratePortalPassword()).ToArray();
        Assert.Equal(passwords.Length, passwords.Distinct().Count());
        Assert.All(passwords, password =>
        {
            Assert.Equal(16, password.Length);
            Assert.Contains(password, char.IsUpper);
            Assert.Contains(password, char.IsLower);
            Assert.Contains(password, char.IsDigit);
            Assert.Contains(password, character => !char.IsLetterOrDigit(character));
        });
    }

    [Fact]
    public async Task Valid_invitation_establishes_limited_session_and_is_single_use()
    {
        await using var fixture = await Fixture.Create();
        var first = await fixture.Controller.Welcome(fixture.RawToken, default);
        Assert.IsType<ViewResult>(first);
        Assert.Equal(fixture.Beneficiary.Id, fixture.Session.GetInt32("BeneficiaryPortalId"));
        Assert.NotNull(fixture.Invitation.UsedAtUtc);
        Assert.Equal(BeneficiaryStatus.AwaitingDocuments, fixture.Beneficiary.Status);
        var second = Assert.IsType<ViewResult>(await fixture.Controller.Welcome(fixture.RawToken, default));
        Assert.Equal("InvalidInvitation", second.ViewName);
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public async Task Expired_revoked_or_used_invitation_is_rejected(bool expired, bool revoked, bool used)
    {
        await using var fixture = await Fixture.Create();
        if (expired) fixture.Invitation.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);
        if (revoked) fixture.Invitation.RevokedAtUtc = DateTime.UtcNow;
        if (used) fixture.Invitation.UsedAtUtc = DateTime.UtcNow;
        await fixture.Db.SaveChangesAsync();
        var result = Assert.IsType<ViewResult>(await fixture.Controller.Welcome(fixture.RawToken, default));
        Assert.Equal("InvalidInvitation", result.ViewName);
        Assert.Null(fixture.Session.GetInt32("BeneficiaryPortalId"));
    }

    [Fact]
    public async Task Raw_invitation_token_is_never_persisted()
    {
        await using var fixture = await Fixture.Create();
        Assert.NotEqual(fixture.RawToken, fixture.Invitation.TokenHash);
        Assert.Equal(SecureToken.Hash(fixture.RawToken), fixture.Invitation.TokenHash);
    }

    [Fact]
    public async Task Password_login_establishes_only_the_beneficiary_portal_session()
    {
        await using var fixture = await Fixture.Create();
        fixture.Beneficiary.PortalAccessEnabled = true;
        fixture.Beneficiary.PortalPasswordHash = BCrypt.Net.BCrypt.HashPassword("test-password");
        await fixture.Db.SaveChangesAsync();

        var result = Assert.IsType<RedirectToActionResult>(await fixture.Controller.Login(fixture.Beneficiary.Email, "test-password", default));

        Assert.Equal("Assets", result.ActionName);
        Assert.Equal(fixture.Beneficiary.Id, fixture.Session.GetInt32("BeneficiaryPortalId"));
        Assert.Null(fixture.Session.GetInt32("UserId"));
    }

    [Fact]
    public async Task Password_login_rejects_incorrect_credentials()
    {
        await using var fixture = await Fixture.Create();
        fixture.Beneficiary.PortalAccessEnabled = true;
        fixture.Beneficiary.PortalPasswordHash = BCrypt.Net.BCrypt.HashPassword("test-password");
        await fixture.Db.SaveChangesAsync();

        var result = Assert.IsType<ViewResult>(await fixture.Controller.Login(fixture.Beneficiary.Email, "incorrect-password", default));

        Assert.Null(fixture.Session.GetInt32("BeneficiaryPortalId"));
        Assert.False(fixture.Controller.ModelState.IsValid);
    }

    [Fact]
    public async Task Manual_verification_is_blocked_until_documents_are_complete()
    {
        await using var fixture = await Fixture.Create();
        fixture.Session.SetInt32("BeneficiaryPortalId", fixture.Beneficiary.Id);
        fixture.Beneficiary.Status = BeneficiaryStatus.AwaitingDocuments;
        await fixture.Db.SaveChangesAsync();

        var result = await fixture.Controller.RequestManualVerification("Camera unavailable", default);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(await fixture.Db.FacialVerificationSessions.ToListAsync());
    }

    [Fact]
    public async Task Manual_verification_creates_audited_heightened_review_after_documents()
    {
        await using var fixture = await Fixture.Create();
        fixture.Session.SetInt32("BeneficiaryPortalId", fixture.Beneficiary.Id);
        fixture.Beneficiary.Status = BeneficiaryStatus.AwaitingFacialVerification;
        await fixture.Db.SaveChangesAsync();

        var result = await fixture.Controller.RequestManualVerification("Camera unavailable", default);

        Assert.IsType<RedirectToActionResult>(result);
        var session = await fixture.Db.FacialVerificationSessions.SingleAsync();
        Assert.Equal(FacialVerificationStatus.ManualReviewRequired, session.Status);
        Assert.False(session.ConsentGranted);
        Assert.Equal("MANUAL_VERIFICATION_REQUESTED", session.ResultReasonCode);
        Assert.Contains(await fixture.Db.AuditEntries.ToListAsync(), x => x.Action == "Manual facial verification requested");
    }

    [Fact]
    public async Task Development_schema_repair_adds_portal_credential_columns_to_legacy_sqlite_database()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var db = new TestDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
        await db.Database.ExecuteSqlRawAsync("CREATE TABLE Beneficiaries (Id INTEGER PRIMARY KEY)");

        await DevelopmentDatabaseSchema.EnsureBeneficiaryPortalCredentialsAsync(db);

        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info(\"Beneficiaries\")";
        await using var reader = await command.ExecuteReaderAsync();
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (await reader.ReadAsync()) columns.Add(reader.GetString(reader.GetOrdinal("name")));
        Assert.Contains("PortalAccessEnabled", columns);
        Assert.Contains("PortalPasswordHash", columns);
        Assert.Contains("PortalPasswordSetAtUtc", columns);
        await db.DisposeAsync();
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        public ApplicationDbContext Db { get; }
        public BeneficiaryPortalController Controller { get; }
        public MemorySession Session { get; }
        public Beneficiary Beneficiary { get; }
        public BeneficiaryInvitation Invitation { get; }
        public string RawToken { get; }
        private Fixture(SqliteConnection connection, ApplicationDbContext db, BeneficiaryPortalController controller, MemorySession session,
            Beneficiary beneficiary, BeneficiaryInvitation invitation, string rawToken)
        { this.connection=connection; Db=db; Controller=controller; Session=session; Beneficiary=beneficiary; Invitation=invitation; RawToken=rawToken; }
        public static async Task<Fixture> Create()
        {
            var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
            var db = new TestDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options); await db.Database.EnsureCreatedAsync();
            var creator = new ApplicationUser { FullName="Director", Email="director@simplex.test", PasswordHash="x", Role=UserRole.Director, AssignedCases=[] };
            var client = new Client { FirstName="Owner", LastName="Client", Email="owner@simplex.test", Phone="1", Cases=[] };
            db.AddRange(creator, client); await db.SaveChangesAsync();
            var beneficiary = new Beneficiary { BenefactorClientId=client.Id, FirstName="Ben", LastName="Person", Email="ben@simplex.test", Status=BeneficiaryStatus.InvitationSent };
            db.Add(beneficiary); await db.SaveChangesAsync();
            var token = SecureToken.Create();
            var invitation = new BeneficiaryInvitation { BeneficiaryId=beneficiary.Id, CreatedByUserId=creator.Id, TokenHash=token.Hash,
                CreatedAtUtc=DateTime.UtcNow, ExpiresAtUtc=DateTime.UtcNow.AddHours(72) };
            db.Add(invitation); await db.SaveChangesAsync();
            var session = new MemorySession(); var http = new DefaultHttpContext(); http.Features.Set<ISessionFeature>(new SessionFeature { Session=session });
            var controller = new BeneficiaryPortalController(db, new NoStorage(), new NoVerification()) { ControllerContext=new ControllerContext { HttpContext=http } };
            return new Fixture(connection, db, controller, session, beneficiary, invitation, token.Raw);
        }
        public async ValueTask DisposeAsync() { await Db.DisposeAsync(); await connection.DisposeAsync(); }
    }
    private sealed class TestDbContext(DbContextOptions<ApplicationDbContext> options) : ApplicationDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder b) { base.OnModelCreating(b); b.Entity<Beneficiary>().Property(x=>x.RowVersion).ValueGeneratedNever(); b.Entity<FacialVerificationSession>().Property(x=>x.RowVersion).ValueGeneratedNever(); b.Entity<CalendarEvent>().Property(x=>x.RowVersion).ValueGeneratedNever(); }
    }
    private sealed class NoStorage : ISecureFileStorage
    {
        public Task<SecureStoredFile> StoreAsync(int beneficiaryId, IFormFile file, CancellationToken cancellationToken=default)=>throw new NotSupportedException();
        public Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken=default)=>throw new NotSupportedException();
    }
    private sealed class NoVerification : ILocalVerificationClient
    {
        public Task<string> AnalyseDocumentAsync(Stream file,string fileName,string requirementCode,bool certified,bool expiryCheck,CancellationToken cancellationToken)=>throw new NotSupportedException();
        public Task<string> VerifyFaceAsync(Stream referenceImage,IReadOnlyList<byte[]> frames,Guid sessionId,IReadOnlyList<string> serverChallenges,IReadOnlyList<long> timestamps,IReadOnlyList<int> stageIndexes,CancellationToken cancellationToken)=>throw new NotSupportedException();
    }
    private sealed class SessionFeature : ISessionFeature { public ISession Session { get; set; } = null!; }
    public sealed class MemorySession : ISession
    {
        private readonly Dictionary<string,byte[]> values=[]; public bool IsAvailable=>true; public string Id=>"test"; public IEnumerable<string> Keys=>values.Keys;
        public void Clear()=>values.Clear(); public Task CommitAsync(CancellationToken cancellationToken=default)=>Task.CompletedTask; public Task LoadAsync(CancellationToken cancellationToken=default)=>Task.CompletedTask;
        public void Remove(string key)=>values.Remove(key); public void Set(string key,byte[] value)=>values[key]=value; public bool TryGetValue(string key,out byte[] value)=>values.TryGetValue(key,out value!);
    }
}
