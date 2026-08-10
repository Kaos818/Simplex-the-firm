using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using SimplexLawFirm.Controllers;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using Xunit;

namespace SimplexLawFirm.Tests;

public sealed class SeedAccessibilityTests
{
    [Fact]
    public async Task Existing_development_database_is_upgraded_with_collaboration_tables_before_seeding()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        await db.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys=OFF; DROP TABLE CaseDocumentRequests; DROP TABLE CaseHandoverRequests; PRAGMA foreign_keys=ON;");

        await DevelopmentDatabaseSchema.EnsureBeneficiaryPortalCredentialsAsync(db);

        Assert.Empty(await db.CaseDocumentRequests.ToListAsync());
        Assert.Empty(await db.CaseHandoverRequests.ToListAsync());
        DbInitializer.Seed(db, true, null, "SimplexTest!2026");
        Assert.Single(await db.CaseDocumentRequests.ToListAsync());
        Assert.Single(await db.CaseHandoverRequests.ToListAsync());
    }

    [Fact]
    public async Task Required_seed_stakeholders_are_active_confirmed_and_share_the_documented_password()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        DbInitializer.Seed(db, true, null, "SimplexTest!2026");
        var expected = new Dictionary<string, UserRole>
        {
            ["director@simplex.com"] = UserRole.Director,
            ["naledi.khumalo@simplex.com"] = UserRole.Lawyer,
            ["sipho.nkosi@simplex.com"] = UserRole.Lawyer,
            ["david.pillay@simplex.com"] = UserRole.Lawyer,
            ["nomsa.zulu@simplex.com"] = UserRole.Paralegal,
            ["accountant@simplex.com"] = UserRole.Accountant,
            ["thabo.mthembu@example.com"] = UserRole.Client
        };
        foreach (var item in expected)
        {
            var user = await db.Users.SingleAsync(x => x.Email == item.Key);
            Assert.Equal(item.Value, user.Role);
            Assert.True(user.IsActive);
            Assert.True(user.EmailConfirmed);
            Assert.True(BCrypt.Net.BCrypt.Verify("SimplexTest!2026", user.PasswordHash));
        }
    }

    [Fact]
    public async Task Showcase_seed_is_linked_and_idempotent()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        DbInitializer.Seed(db, false, null, "SimplexTest!2026");

        Assert.Equal(9, await db.Cases.CountAsync(x => x.CaseNumber.StartsWith("DEMO-") && x.Status == CaseStatus.Closed));
        Assert.Equal(3, await db.CaseForecasts.CountAsync(x => x.Status == ForecastStatus.Scored));
        Assert.Single(await db.ServiceComplaints.Where(x => x.ReferenceNumber == "SLC-DEMO-2026-001").ToListAsync());
        var handover = await db.CaseHandovers.Include(x => x.Items).Include(x => x.Case).SingleAsync();
        Assert.Equal(5, handover.Items.Select(x => x.Type).Distinct().Count());
        Assert.Equal(handover.OutgoingAttorneyId, handover.Case.LawyerId);
        Assert.Contains(handover.Items, x => x.IsMandatory && !x.IsResolved);
        Assert.Single(await db.PrecedentConflictFlags.Where(x => x.Status == PrecedentFlagStatus.Pending).ToListAsync());
        Assert.Single(await db.LitigationStrategyDecisions.Where(x => x.Status == StrategyDecisionStatus.PendingDirectorAuthorisation).ToListAsync());
        Assert.Equal(24, await db.Cases.CountAsync(x => x.CaseNumber.StartsWith("EST-HIST-") && x.Status == CaseStatus.Closed));
        Assert.Equal(24, await db.Invoices.CountAsync(x => x.InvoiceNumber.StartsWith("EST-REV-") && x.Status == InvoiceStatus.Paid));
        Assert.True(await db.LawyerProfiles.CountAsync(x => x.IsActive && x.HourlyRate > 0) >= 3);
        Assert.Single(await db.CaseDocumentRequests.Where(x => x.Status == CaseDocumentRequestStatus.Requested).ToListAsync());
        Assert.Single(await db.CaseHandoverRequests.Where(x => x.Status == HandoverRequestStatus.PendingDirector).ToListAsync());
        var reimbursement = await db.ReimbursementClaims.SingleAsync(x => x.ClaimNumber == "RMB-DEMO-001");
        Assert.Equal("naledi.khumalo@simplex.com", (await db.Users.FindAsync(reimbursement.AttorneyId))!.Email);
        Assert.NotNull(reimbursement.ProofSha256Hash);

        DbInitializer.Seed(db, false, null, "SimplexTest!2026");
        Assert.Equal(9, await db.Cases.CountAsync(x => x.CaseNumber.StartsWith("DEMO-") && x.Status == CaseStatus.Closed));
        Assert.Single(await db.ServiceComplaints.Where(x => x.ReferenceNumber == "SLC-DEMO-2026-001").ToListAsync());
        Assert.Single(await db.ReimbursementClaims.Where(x => x.ClaimNumber == "RMB-DEMO-001").ToListAsync());
        Assert.Equal(24, await db.Invoices.CountAsync(x => x.InvoiceNumber.StartsWith("EST-REV-")));
        Assert.Single(await db.CaseDocumentRequests.ToListAsync());
        Assert.Single(await db.CaseHandoverRequests.ToListAsync());
        Assert.Equal(5, await db.HandoverItems.Where(x => x.CaseHandoverId == handover.Id).CountAsync());
    }

    [Fact]
    public async Task Director_can_create_a_new_accessible_staff_user()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var controller = new UsersController(db) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        controller.TempData = new TempDataDictionary(controller.HttpContext, new MemoryTempDataProvider());
        var result = await controller.Create(new CreateUserViewModel { FullName = "New Lawyer", Email = "new.lawyer@example.test", Role = UserRole.Lawyer, Password = "Strong!Staff2026", IsActive = true, AreaOfProfession = "Commercial law" });
        Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectToActionResult>(result);
        var user = await db.Users.SingleAsync();
        Assert.True(user.EmailConfirmed);
        Assert.True(user.IsActive);
        Assert.Equal(UserRole.Lawyer, user.Role);
        Assert.True(BCrypt.Net.BCrypt.Verify("Strong!Staff2026", user.PasswordHash));
        var profile = await db.LawyerProfiles.Include(x => x.Specializations).SingleAsync(x => x.UserId == user.Id);
        Assert.Contains(profile.Specializations, x => x.Name == "Commercial law");
    }

    [Fact]
    public async Task Legacy_admin_identity_is_upgraded_to_the_documented_director_login()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        db.Users.Add(new ApplicationUser { FullName = "Admin User", Email = "admin@simplex.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("old"), Role = UserRole.Admin, IsActive = true, EmailConfirmed = true, AssignedCases = [] });
        await db.SaveChangesAsync();
        DbInitializer.Seed(db, true, null, "SimplexTest!2026");
        var director = await db.Users.SingleAsync(x => x.Email == "director@simplex.com");
        Assert.False(await db.Users.AnyAsync(x => x.Email == "admin@simplex.com"));
        Assert.Equal("director@simplex.com", director.Email);
        Assert.Equal(UserRole.Director, director.Role);
        Assert.True(BCrypt.Net.BCrypt.Verify("SimplexTest!2026", director.PasswordHash));
    }

    private sealed class MemoryTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
