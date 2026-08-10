using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services;
using Xunit;

namespace SimplexLawFirm.Tests;

public sealed class PrecedentLibraryTests
{
    [Fact]
    public async Task Confidential_article_is_excluded_before_embedding()
    {
        await using var f = await Fixture.CreateAsync();
        var article = await f.Article("Protected advice", "This text must never leave the source.", confidential: true);
        var job = await f.Service.QueueArticleAsync(article.Id);
        await f.Service.ProcessBacklogAsync();
        Assert.Equal(PrecedentJobStatus.Excluded, job.Status);
        Assert.Equal(0, f.Embedding.Calls);
        Assert.Empty(await f.Db.PrecedentItems.ToListAsync());
        Assert.Contains("Confidential", job.ExclusionReason);
    }

    [Fact]
    public async Task Published_article_is_chunked_embedded_classified_and_filed()
    {
        await using var f = await Fixture.CreateAsync();
        var text = string.Join(' ', Enumerable.Repeat("A commercial contract shareholder dispute was decided by the court.", 80));
        var article = await f.Article("New commercial principle", text);
        var job = await f.Service.QueueArticleAsync(article.Id);
        await f.Service.ProcessJobAsync(job.Id);
        var item = await f.Db.PrecedentItems.Include(x => x.LegalSubject).SingleAsync();
        Assert.Equal("Commercial Law", item.LegalSubject.Name);
        Assert.True(await f.Db.PrecedentPassages.CountAsync() > 1);
        Assert.Equal(await f.Db.PrecedentPassages.CountAsync(), f.Embedding.Calls);
        Assert.Equal(PrecedentJobStatus.Indexed, (await f.Db.PrecedentIndexJobs.FindAsync(job.Id))!.Status);
    }

    [Fact]
    public async Task Embedding_outage_preserves_job_and_retry_indexes_it()
    {
        await using var f = await Fixture.CreateAsync();
        var article = await f.Article("Labour update", "A revised labour dismissal procedure now applies at the CCMA.");
        var job = await f.Service.QueueArticleAsync(article.Id);
        f.Embedding.Unavailable = true;
        await f.Service.ProcessJobAsync(job.Id);
        var queued = await f.Db.PrecedentIndexJobs.FindAsync(job.Id);
        Assert.Equal(PrecedentJobStatus.Queued, queued!.Status);
        Assert.NotNull(queued.LastError);
        Assert.Empty(await f.Db.PrecedentItems.ToListAsync());
        f.Embedding.Unavailable = false;
        queued.NextAttemptAtUtc = DateTime.UtcNow.AddSeconds(-1);
        await f.Db.SaveChangesAsync();
        await f.Service.ProcessBacklogAsync();
        Assert.Equal(PrecedentJobStatus.Indexed, queued.Status);
        Assert.Single(await f.Db.PrecedentItems.ToListAsync());
    }

    [Fact]
    public async Task New_superseding_work_is_flagged_and_admin_can_retire_old_item()
    {
        await using var f = await Fixture.CreateAsync();
        var first = await f.Article("Old eviction rule", "Property eviction requires notice and a hearing before the court.");
        var firstJob = await f.Service.QueueArticleAsync(first.Id); await f.Service.ProcessJobAsync(firstJob.Id);
        var second = await f.Article("Revised eviction rule", "This revised property eviction rule supersedes the old rule and requires notice and a hearing before the court.");
        var secondJob = await f.Service.QueueArticleAsync(second.Id); await f.Service.ProcessJobAsync(secondJob.Id);
        var flag = await f.Db.PrecedentConflictFlags.SingleAsync();
        await f.Service.ReviewFlagAsync(flag.Id, PrecedentFlagStatus.Retired, 77, "Overtaken by revised authority.");
        Assert.Equal(PrecedentFlagStatus.Retired, flag.Status);
        Assert.False((await f.Db.PrecedentItems.FindAsync(flag.ExistingPrecedentItemId))!.IsCurrent);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            f.Service.ReviewFlagAsync(flag.Id, PrecedentFlagStatus.Retained, 77, "Duplicate decision"));
    }

    [Fact]
    public async Task Coverage_includes_empty_subjects_and_commissions_are_governed()
    {
        await using var f = await Fixture.CreateAsync();
        var dashboard = await f.Service.DashboardAsync();
        Assert.Equal(9, dashboard.Coverage.Count);
        Assert.All(dashboard.Coverage, x => Assert.Equal(0, x.CurrentItems));
        await f.Service.CommissionAsync(1, 77, "Prepare a civil litigation precedent pack.", DateTime.UtcNow.AddDays(14));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            f.Service.CommissionAsync(1, 77, "Duplicate", null));
        var refreshed = await f.Service.DashboardAsync();
        Assert.NotNull(refreshed.Coverage.Single(x => x.Subject.Id == 1).OpenCommission);
        var commission = await f.Db.CoverageCommissions.SingleAsync();
        await f.Service.CompleteCommissionAsync(commission.Id, 77);
        Assert.Equal(CoverageCommissionStatus.Completed, commission.Status);
    }

    [Fact]
    public async Task Source_becoming_confidential_or_archived_withdraws_already_indexed_version()
    {
        await using var f = await Fixture.CreateAsync();
        var article = await f.Article("Published advice", "Commercial contract advice for company shareholders.");
        var job = await f.Service.QueueArticleAsync(article.Id);
        await f.Service.ProcessJobAsync(job.Id);
        Assert.True((await f.Db.PrecedentItems.SingleAsync()).IsCurrent);

        article.IsConfidential = true;
        await f.Db.SaveChangesAsync();
        var excluded = await f.Service.QueueArticleAsync(article.Id);
        Assert.Equal(PrecedentJobStatus.Excluded, excluded.Status);
        var item = await f.Db.PrecedentItems.SingleAsync();
        Assert.False(item.IsCurrent);
        Assert.Contains("Confidential", item.CuratorNote);
    }

    [Fact]
    public async Task Removing_protection_requeues_same_content_without_losing_it()
    {
        await using var f = await Fixture.CreateAsync();
        var article = await f.Article("Initially protected", "A family divorce custody principle.", confidential: true);
        var job = await f.Service.QueueArticleAsync(article.Id);
        Assert.Equal(PrecedentJobStatus.Excluded, job.Status);
        article.IsConfidential = false;
        await f.Db.SaveChangesAsync();
        var reactivated = await f.Service.QueueArticleAsync(article.Id);
        Assert.Equal(job.Id, reactivated.Id);
        Assert.Equal(PrecedentJobStatus.Queued, reactivated.Status);
        await f.Service.ProcessBacklogAsync();
        Assert.Single(await f.Db.PrecedentItems.Where(x => x.IsCurrent).ToListAsync());
    }

    [Fact]
    public async Task New_version_of_same_source_retires_old_version()
    {
        await using var f = await Fixture.CreateAsync();
        var article = await f.Article("Versioned guidance", "The labour dismissal procedure requires a hearing.");
        var first = await f.Service.QueueArticleAsync(article.Id); await f.Service.ProcessJobAsync(first.Id);
        article.Content = "The revised labour dismissal procedure requires notice, reasons and a hearing.";
        await f.Db.SaveChangesAsync();
        var second = await f.Service.QueueArticleAsync(article.Id); await f.Service.ProcessJobAsync(second.Id);
        var versions = await f.Db.PrecedentItems.OrderBy(x => x.IndexedAtUtc).ToListAsync();
        Assert.Equal(2, versions.Count);
        Assert.False(versions[0].IsCurrent);
        Assert.Contains("newer saved version", versions[0].CuratorNote);
        Assert.True(versions[1].IsCurrent);
    }

    [Fact]
    public async Task Interrupted_processing_job_is_recovered_and_not_lost()
    {
        await using var f = await Fixture.CreateAsync();
        var article = await f.Article("Interrupted", "Criminal bail guidance for an accused person.");
        var job = await f.Service.QueueArticleAsync(article.Id);
        job.Status = PrecedentJobStatus.Processing;
        job.ProcessingStartedAtUtc = DateTime.UtcNow.AddMinutes(-11);
        await f.Db.SaveChangesAsync();
        await f.Service.ProcessBacklogAsync();
        Assert.True(job.Status == PrecedentJobStatus.Indexed, $"Expected recovered job to index but it was {job.Status}: {job.LastError}");
        Assert.Single(await f.Db.PrecedentItems.ToListAsync());
    }

    [Fact]
    public async Task Remote_embedding_mode_sends_content_and_accepts_standard_vector_response()
    {
        var handler = new StubHandler("""{"data":[{"embedding":[0.25,-0.5,0.75]}]}""");
        var service = new ConfiguredEmbeddingService(new LocalSemanticEmbeddingService(),
            new HttpClient(handler), Options.Create(new PrecedentEmbeddingOptions
            { Mode = "Remote", Endpoint = "https://embeddings.example.test/v1", ApiKey = "secret", Model = "legal-v1" }));
        var vector = await service.CreateAsync("Contract principle");
        Assert.Equal([0.25f, -0.5f, 0.75f], vector);
        Assert.Contains("Contract principle", handler.RequestBody);
        Assert.Equal("Bearer", handler.AuthorizationScheme);
    }

    [Fact]
    public async Task Invalid_remote_embedding_response_is_treated_as_service_failure()
    {
        var service = new ConfiguredEmbeddingService(new LocalSemanticEmbeddingService(),
            new HttpClient(new StubHandler("""{"unexpected":true}""")), Options.Create(new PrecedentEmbeddingOptions
            { Mode = "Remote", Endpoint = "https://embeddings.example.test/v1" }));
        await Assert.ThrowsAsync<InvalidDataException>(() => service.CreateAsync("Text"));
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        public ApplicationDbContext Db { get; }
        public FakeEmbedding Embedding { get; } = new();
        public PrecedentLibraryService Service { get; }
        private Fixture(SqliteConnection connection, ApplicationDbContext db)
        { this.connection = connection; Db = db; Service = new(db, Embedding); }
        public static async Task<Fixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
            await db.Database.EnsureCreatedAsync();
            return new Fixture(connection, db);
        }
        public async Task<KnowledgeArticle> Article(string title, string content, bool confidential = false)
        {
            var article = new KnowledgeArticle { Title = title, Content = content, Status = KnowledgeArticleStatus.Published,
                AuthorUserId = 77, IsConfidential = confidential };
            Db.KnowledgeArticles.Add(article); await Db.SaveChangesAsync(); return article;
        }
        public async ValueTask DisposeAsync() { await Db.DisposeAsync(); await connection.DisposeAsync(); }
    }

    private sealed class FakeEmbedding : IEmbeddingService
    {
        private readonly LocalSemanticEmbeddingService inner = new();
        public bool Unavailable { get; set; }
        public int Calls { get; private set; }
        public Task<float[]> CreateAsync(string text, CancellationToken ct = default)
        {
            Calls++;
            if (Unavailable) throw new HttpRequestException("Embedding service unavailable.");
            return inner.CreateAsync(text, ct);
        }
    }

    private sealed class StubHandler(string response) : HttpMessageHandler
    {
        public string RequestBody { get; private set; } = "";
        public string? AuthorizationScheme { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            AuthorizationScheme = request.Headers.Authorization?.Scheme;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(response) };
        }
    }
}
