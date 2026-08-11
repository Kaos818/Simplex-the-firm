using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using SimplexLawFirm.Data;
using SimplexLawFirm.Services.Storage;
using SimplexLawFirm.Services.Verification;
using SimplexLawFirm.Services.Billing;
using SimplexLawFirm.Services.CurrentUser;
using SimplexLawFirm.Services.Email;
using SimplexLawFirm.Services.Notifications;
using SimplexLawFirm.Services;
using SimplexLawFirm.Infrastructure.Authorization;
using SimplexLawFirm.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add<MatterSafeguardAcknowledgementFilter>());

// Add DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not configured.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase) &&
        !connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase))
        options.UseSqlite(connectionString);
    else options.UseSqlServer(connectionString);
});

// IMPORTANT: Add Session services BEFORE building the app
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtection-Keys")))
    .SetApplicationName("SimplexLawFirm");
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Home/Login";
        options.AccessDeniedPath = "/Home/AccessDenied";
        options.Cookie.Name = "Simplex.Authorization";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

// Add HttpContextAccessor for session access
builder.Services.AddHttpContextAccessor();

// Add Razor runtime compilation
builder.Services.Configure<VerificationOptions>(builder.Configuration.GetSection("Verification"));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.AddScoped<ISecureFileStorage, LocalSecureFileStorage>();
builder.Services.AddHttpClient<ILocalVerificationClient, LocalVerificationClient>();
builder.Services.AddScoped<IReferenceFaceExtractor, ReferenceFaceExtractor>();
builder.Services.AddScoped<ICurrentClientService, CurrentClientService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IEmailService, EmailOutboxService>();
builder.Services.AddScoped<IAppointmentBillingService, AppointmentBillingService>();
builder.Services.AddScoped<IPracticeIntelligenceService, PracticeIntelligenceService>();
builder.Services.AddScoped<IMatterCostEstimateService, MatterCostEstimateService>();
builder.Services.AddScoped<IReimbursementService, ReimbursementService>();
builder.Services.AddScoped<ILegalCostRecoveryService, LegalCostRecoveryService>();
builder.Services.AddScoped<ICaseGovernanceService, CaseGovernanceService>();
builder.Services.AddScoped<IOperationalUseCaseService, OperationalUseCaseService>();
builder.Services.Configure<LegalResearchOptions>(builder.Configuration.GetSection("LegalResearch"));
builder.Services.AddScoped<IReimbursementProofStorage, ReimbursementProofStorage>();
builder.Services.Configure<PrecedentEmbeddingOptions>(builder.Configuration.GetSection("PrecedentEmbedding"));
builder.Services.AddSingleton<LocalSemanticEmbeddingService>();
builder.Services.AddHttpClient<IEmbeddingService, ConfiguredEmbeddingService>();
builder.Services.AddScoped<IPrecedentLibraryService, PrecedentLibraryService>();
builder.Services.Configure<VulnerableClientOptions>(builder.Configuration.GetSection("VulnerableClient"));
builder.Services.AddScoped<IVulnerableClientService, VulnerableClientService>();
builder.Services.AddSingleton<IEstimatePdfService, EstimatePdfService>();
builder.Services.AddScoped<IComplaintFileStorage, ComplaintFileStorage>();
builder.Services.AddScoped<ExternalEvidenceStorage>();
builder.Services.AddHostedService<PracticeGovernanceWorker>();
builder.Services.AddHostedService<EmailOutboxWorker>();
builder.Services.AddHostedService<AppointmentBillingWorker>();
builder.Services.AddHostedService<LatePenaltyWorker>();
builder.Services.AddHostedService<PrecedentIndexWorker>();
builder.Services.AddHostedService<VulnerableClientGovernanceWorker>();
builder.Services.AddHostedService<CaseGovernanceWorker>();
builder.Services.AddHostedService<AttorneySafetyWorker>();

if (!builder.Environment.IsDevelopment())
{
    var requiredSettings = new List<string>
    {
        "ConnectionStrings:DefaultConnection", "Email:FromAddress", "Email:PublicBaseUrl",
        "Verification:BaseUrl", "Verification:ApiKey"
    };
    // Azure Communication Services authenticates with its connection string rather than SMTP credentials.
    // Retain SMTP validation for installations that have not opted into the Azure transport.
    if (string.IsNullOrWhiteSpace(builder.Configuration["Email:AzureCommunicationConnectionString"]))
        requiredSettings.AddRange(["Email:Host", "Email:Username", "Email:Password"]);
    var missing = requiredSettings.Where(key => string.IsNullOrWhiteSpace(builder.Configuration[key])).ToArray();
    if (missing.Length > 0)
        throw new InvalidOperationException($"Required production configuration is missing: {string.Join(", ", missing)}.");
    if (!Uri.TryCreate(builder.Configuration["Email:PublicBaseUrl"], UriKind.Absolute, out var publicUrl) ||
        publicUrl.Scheme != Uri.UriSchemeHttps)
        throw new InvalidOperationException("Email:PublicBaseUrl must be an absolute HTTPS URL in production.");
    if (!Uri.TryCreate(builder.Configuration["Verification:BaseUrl"], UriKind.Absolute, out var verificationUrl) ||
        !verificationUrl.IsLoopback)
        throw new InvalidOperationException("Verification:BaseUrl must point to a loopback address.");
}

var app = builder.Build();


// 🔥 FORCE DEVELOPMENT ERROR VIEW (QUICK FIX)
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}


// Configure the HTTP request pipeline.
else
{
    // ❌ DISABLED TEMPORARILY FOR DEBUGGING
    // app.UseExceptionHandler("/Home/Error");
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseMiddleware<RememberMeMiddleware>();
app.UseMiddleware<SimplexLawFirm.Infrastructure.Authorization.SupportPersonEnforcementMiddleware>();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


// Seed the database with initial data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        // The schema strategy follows the provider, not the environment. The migration
        // history is authored against SQL Server, so a SQLite database - whether a
        // developer's local file or a deployed demo instance - builds its schema from
        // the current model instead. Keying this on IsDevelopment() meant a deployed
        // SQLite instance took the migration path and refused to start.
        if (context.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            await context.Database.EnsureCreatedAsync();
            await DevelopmentDatabaseSchema.EnsureBeneficiaryPortalCredentialsAsync(context);
        }
        else
            await context.Database.MigrateAsync();
        DbInitializer.Seed(context, app.Environment.IsDevelopment(), builder.Configuration["Seed:KaoticPortalPassword"], builder.Configuration["Seed:SharedPassword"], app.Environment.ContentRootPath);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
        throw;
    }
}

app.Run();
