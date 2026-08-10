using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using SimplexLawFirm.Services.Security;
using SimplexLawFirm.Services.Storage;
using SimplexLawFirm.Controllers;
using SimplexLawFirm.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace SimplexLawFirm.Tests;

public class SecurityAndStorageTests
{
    [Fact]
    public void Forbidden_session_routes_have_a_registered_cookie_scheme_in_startup()
    {
        var startup = File.ReadAllText(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Program.cs")));
        Assert.Contains("AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)", startup);
        Assert.Contains("app.UseAuthentication();", startup);
        Assert.True(startup.IndexOf("app.UseAuthentication();", StringComparison.Ordinal) < startup.IndexOf("app.UseAuthorization();", StringComparison.Ordinal));
    }
    [Theory]
    [InlineData(typeof(RetainerController))]
    [InlineData(typeof(BillingController))]
    [InlineData(typeof(CaseController))]
    [InlineData(typeof(ClientController))]
    public void Legacy_business_controllers_require_session_and_automatic_csrf_validation(Type controllerType)
    {
        Assert.NotNull(controllerType.GetCustomAttributes(typeof(RequireSessionUserAttribute), true).SingleOrDefault());
        Assert.NotNull(controllerType.GetCustomAttributes(typeof(AutoValidateAntiforgeryTokenAttribute), true).SingleOrDefault());
    }

    [Fact]
    public void User_management_is_director_only_and_csrf_protected()
    {
        var role = Assert.Single(typeof(UsersController).GetCustomAttributes(typeof(RequireSessionRoleAttribute), true));
        Assert.NotNull(role);
        Assert.NotNull(typeof(UsersController).GetCustomAttributes(typeof(AutoValidateAntiforgeryTokenAttribute), true).SingleOrDefault());
    }
    [Fact]
    public void Secure_tokens_are_random_and_only_hashes_need_persisting()
    {
        var first = SecureToken.Create(); var second = SecureToken.Create();
        Assert.NotEqual(first.Raw, second.Raw); Assert.Equal(64, first.Hash.Length); Assert.Equal(first.Hash, SecureToken.Hash(first.Raw));
    }
    [Fact]
    public async Task Executable_renamed_as_pdf_is_rejected()
    {
        var service = CreateStorage();
        await Assert.ThrowsAsync<InvalidDataException>(() => service.StoreAsync(1, Form("malware.pdf", "MZ executable"u8.ToArray())));
    }
    [Fact]
    public async Task Invalid_jpeg_is_rejected()
    {
        await Assert.ThrowsAsync<InvalidDataException>(() => CreateStorage().StoreAsync(1, Form("fake.jpg", [0xff, 0xd8, 0x00, 0x01])));
    }
    [Fact]
    public async Task Oversized_file_is_rejected()
    {
        await Assert.ThrowsAsync<InvalidDataException>(() => CreateStorage().StoreAsync(1, Form("large.pdf", new byte[LocalSecureFileStorage.MaximumBytes + 1])));
    }
    [Fact]
    public async Task Path_traversal_is_rejected()
    {
        await Assert.ThrowsAsync<InvalidDataException>(() => CreateStorage().OpenReadAsync("../outside.bin"));
    }
    [Fact]
    public async Task Reimbursement_proof_rejects_disguised_executable()
    {
        await Assert.ThrowsAsync<InvalidDataException>(() =>
            CreateReimbursementStorage().StoreAsync(1, Form("receipt.pdf", "MZ executable"u8.ToArray())));
    }
    [Fact]
    public async Task Reimbursement_proof_path_traversal_is_rejected()
    {
        await Assert.ThrowsAsync<InvalidDataException>(() =>
            CreateReimbursementStorage().OpenReadAsync("../outside.bin"));
    }
    [Fact]
    public async Task Case_documents_are_stored_outside_the_public_web_root()
    {
        var root = Path.Combine(Path.GetTempPath(), "simplex-case-document-tests", Guid.NewGuid().ToString("N"));
        var storage = new SecureDocumentStorage(new TestEnvironment(root));
        var stored = await storage.StoreAsync(42, Form("evidence.txt", "case evidence"u8.ToArray()));
        Assert.False(Path.IsPathRooted(stored.RelativePath));
        Assert.True(File.Exists(Path.Combine(root, "App_Data", "SecureCaseDocuments", stored.RelativePath.Replace('/', Path.DirectorySeparatorChar))));
        Assert.False(Directory.Exists(Path.Combine(root, "wwwroot", "uploads")));
    }
    [Fact]
    public async Task Case_document_storage_rejects_path_traversal() =>
        await Assert.ThrowsAsync<InvalidDataException>(() => new SecureDocumentStorage(new TestEnvironment(Path.GetTempPath())).OpenReadAsync("../outside.bin"));
    [Fact]
    public void Publish_configuration_excludes_secrets_uploads_and_workspace_artifacts()
    {
        var project = File.ReadAllText(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../SimplexLawFirm.csproj")));
        Assert.Contains("appsettings.Development.json\" CopyToPublishDirectory=\"Never", project);
        Assert.Contains("wwwroot\\uploads\\**\\*", project);
        Assert.Contains(".artifacts\\**\\*", project);
    }
    private static LocalSecureFileStorage CreateStorage() => new(new TestEnvironment(Path.Combine(Path.GetTempPath(), "simplex-tests", Guid.NewGuid().ToString("N"))));
    private static ReimbursementProofStorage CreateReimbursementStorage() => new(new TestEnvironment(Path.Combine(Path.GetTempPath(), "simplex-reimbursement-tests", Guid.NewGuid().ToString("N"))));
    private static FormFile Form(string name, byte[] data) => new(new MemoryStream(data), 0, data.Length, "file", name) { Headers = new HeaderDictionary(), ContentType = "application/octet-stream" };
    private sealed class TestEnvironment(string root) : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = Path.Combine(root, "wwwroot");
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ApplicationName { get; set; } = "Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = root;
        public string EnvironmentName { get; set; } = "Development";
    }
}
