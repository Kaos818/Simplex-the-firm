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
    public async Task Director_can_create_a_new_accessible_staff_user()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var controller = new UsersController(db) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        controller.TempData = new TempDataDictionary(controller.HttpContext, new MemoryTempDataProvider());
        var result = await controller.Create(new CreateUserViewModel { FullName = "New Lawyer", Email = "new.lawyer@example.test", Role = UserRole.Lawyer, Password = "Strong!Staff2026", IsActive = true });
        Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectToActionResult>(result);
        var user = await db.Users.SingleAsync();
        Assert.True(user.EmailConfirmed);
        Assert.True(user.IsActive);
        Assert.Equal(UserRole.Lawyer, user.Role);
        Assert.True(BCrypt.Net.BCrypt.Verify("Strong!Staff2026", user.PasswordHash));
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
