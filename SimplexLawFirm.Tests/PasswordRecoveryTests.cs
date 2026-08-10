using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SimplexLawFirm.Controllers;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services.Email;
using Xunit;

namespace SimplexLawFirm.Tests;

public sealed class PasswordRecoveryTests
{
    [Fact]
    public async Task Forgot_password_stores_only_hash_and_queues_one_hour_link()
    {
        await using var f = await Fixture.Create();
        await f.Controller.ForgotPassword("  CLIENT@EXAMPLE.TEST  ");

        var message = Assert.Single(f.Email.Messages);
        var rawToken = TokenFrom(message.Text);
        Assert.Equal(HomeController.HashToken(rawToken), f.User.PasswordResetToken);
        Assert.DoesNotContain(rawToken, f.User.PasswordResetToken!);
        Assert.InRange(f.User.PasswordResetTokenExpiry!.Value, DateTime.UtcNow.AddMinutes(59), DateTime.UtcNow.AddMinutes(61));
        Assert.Contains("https://portal.simplex.test/Home/ResetPassword?token=", message.Text);
    }

    [Fact]
    public async Task Reset_changes_hash_revokes_remember_token_and_is_one_time()
    {
        await using var f = await Fixture.Create();
        f.User.RememberMeToken = "old-session";
        await f.Controller.ForgotPassword(f.User.Email);
        var rawToken = TokenFrom(Assert.Single(f.Email.Messages).Text);

        var result = await f.Controller.ResetPassword(rawToken, "FreshPassword9!", "FreshPassword9!");

        Assert.IsType<RedirectToActionResult>(result);
        Assert.True(BCrypt.Net.BCrypt.Verify("FreshPassword9!", f.User.PasswordHash));
        Assert.Null(f.User.PasswordResetToken);
        Assert.Null(f.User.PasswordResetTokenExpiry);
        Assert.Null(f.User.RememberMeToken);
        Assert.IsType<RedirectToActionResult>(await f.Controller.ResetPassword(rawToken, "AnotherPassword9!", "AnotherPassword9!"));
        Assert.True(BCrypt.Net.BCrypt.Verify("FreshPassword9!", f.User.PasswordHash));
    }

    [Fact]
    public async Task Expired_link_is_rejected_without_changing_password()
    {
        await using var f = await Fixture.Create();
        const string rawToken = "expired-token";
        f.User.PasswordResetToken = HomeController.HashToken(rawToken);
        f.User.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(-1);
        await f.Db.SaveChangesAsync();

        Assert.IsType<RedirectToActionResult>(await f.Controller.ResetPassword(rawToken));
        Assert.IsType<RedirectToActionResult>(await f.Controller.ResetPassword(rawToken, "FreshPassword9!", "FreshPassword9!"));
        Assert.True(BCrypt.Net.BCrypt.Verify("OriginalPassword9!", f.User.PasswordHash));
    }

    [Fact]
    public async Task Weak_or_mismatched_password_is_rejected()
    {
        await using var f = await Fixture.Create();
        await f.Controller.ForgotPassword(f.User.Email);
        var rawToken = TokenFrom(Assert.Single(f.Email.Messages).Text);

        Assert.IsType<ViewResult>(await f.Controller.ResetPassword(rawToken, "short", "short"));
        Assert.IsType<ViewResult>(await f.Controller.ResetPassword(rawToken, "FreshPassword9!", "DifferentPass9!"));
        Assert.True(BCrypt.Net.BCrypt.Verify("OriginalPassword9!", f.User.PasswordHash));
    }

    [Theory]
    [InlineData("missing@example.test")]
    [InlineData("")]
    public async Task Unknown_or_blank_email_has_generic_response_and_sends_nothing(string emailAddress)
    {
        await using var f = await Fixture.Create();
        Assert.IsType<RedirectToActionResult>(await f.Controller.ForgotPassword(emailAddress));
        Assert.Empty(f.Email.Messages);
        Assert.Equal("If an eligible account exists, a password-reset email has been sent.", f.Controller.TempData["Success"]);
    }

    [Fact]
    public async Task Login_is_case_insensitive_and_remember_token_is_hashed()
    {
        await using var f = await Fixture.Create();
        var result = await f.Controller.Login(" CLIENT@EXAMPLE.TEST ", "OriginalPassword9!", true);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Client", redirect.ActionName);
        Assert.Equal(f.User.Id, f.Controller.HttpContext.Session.GetInt32("UserId"));
        Assert.NotNull(f.User.RememberMeToken);
        Assert.DoesNotContain("OriginalPassword9!", f.User.RememberMeToken!);
    }

    private static string TokenFrom(string text)
    {
        var match = Regex.Match(text, @"[?&]token=([^\s]+)");
        Assert.True(match.Success);
        return Uri.UnescapeDataString(match.Groups[1].Value);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        public ApplicationDbContext Db { get; }
        public ApplicationUser User { get; }
        public CapturingEmail Email { get; } = new();
        public HomeController Controller { get; }

        private Fixture(SqliteConnection connection, ApplicationDbContext db, ApplicationUser user)
        {
            this.connection = connection; Db = db; User = user;
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
                { ["Email:PublicBaseUrl"] = "https://portal.simplex.test" }).Build();
            Controller = new HomeController(db, Email, config);
            var http = new DefaultHttpContext { Session = new MemorySession() };
            Controller.ControllerContext = new ControllerContext { HttpContext = http };
            Controller.TempData = new TempDataDictionary(http, new MemoryTempDataProvider());
        }

        public static async Task<Fixture> Create()
        {
            var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
            var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
            await db.Database.EnsureCreatedAsync();
            var user = new ApplicationUser { FullName = "Test Client", Email = "client@example.test",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("OriginalPassword9!"), Role = UserRole.Client,
                IsActive = true, EmailConfirmed = true, AssignedCases = [] };
            db.Users.Add(user); await db.SaveChangesAsync();
            return new Fixture(connection, db, user);
        }

        public async ValueTask DisposeAsync() { await Db.DisposeAsync(); await connection.DisposeAsync(); }
    }

    private sealed class CapturingEmail : IEmailService
    {
        public List<(string To, string Text)> Messages { get; } = [];
        public Task QueueAsync(string to, string subject, string html, string text, string key, CancellationToken ct = default)
        { Messages.Add((to, text)); return Task.CompletedTask; }
    }

    private sealed class MemoryTempDataProvider : ITempDataProvider
    {
        private Dictionary<string, object> data = [];
        public IDictionary<string, object> LoadTempData(HttpContext context) => data;
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) => data = new(values);
    }

    private sealed class MemorySession : ISession
    {
        private readonly Dictionary<string, byte[]> values = [];
        public bool IsAvailable => true; public string Id { get; } = Guid.NewGuid().ToString(); public IEnumerable<string> Keys => values.Keys;
        public void Clear() => values.Clear(); public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken ct = default) => Task.CompletedTask; public void Remove(string key) => values.Remove(key);
        public void Set(string key, byte[] value) => values[key] = value;
        public bool TryGetValue(string key, out byte[] value)
        {
            if (values.TryGetValue(key, out var stored))
            {
                value = stored;
                return true;
            }
            value = Array.Empty<byte>();
            return false;
        }
    }
}
