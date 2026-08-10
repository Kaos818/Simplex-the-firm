using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using SimplexLawFirm.ViewModels;

namespace SimplexLawFirm.Services;

public interface IEmbeddingService
{
    Task<float[]> CreateAsync(string text, CancellationToken ct = default);
}

public sealed class PrecedentEmbeddingOptions
{
    public string Mode { get; set; } = "Local";
    public string? Endpoint { get; set; }
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "default";
    public int TimeoutSeconds { get; set; } = 30;
}

public sealed class ConfiguredEmbeddingService(
    LocalSemanticEmbeddingService local,
    HttpClient http,
    IOptions<PrecedentEmbeddingOptions> options) : IEmbeddingService
{
    public async Task<float[]> CreateAsync(string text, CancellationToken ct = default)
    {
        var settings = options.Value;
        if (string.Equals(settings.Mode, "Local", StringComparison.OrdinalIgnoreCase))
            return await local.CreateAsync(text, ct);
        if (!string.Equals(settings.Mode, "Remote", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("PrecedentEmbedding:Mode must be Local or Remote.");
        if (!Uri.TryCreate(settings.Endpoint, UriKind.Absolute, out var endpoint) ||
            endpoint.Scheme is not ("https" or "http"))
            throw new InvalidOperationException("A valid PrecedentEmbedding:Endpoint is required in Remote mode.");
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(new { model = settings.Model, input = text })
        };
        if (!string.IsNullOrWhiteSpace(settings.ApiKey))
            request.Headers.Authorization = new("Bearer", settings.ApiKey);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(settings.TimeoutSeconds, 5, 120)));
        HttpResponseMessage response;
        try { response = await http.SendAsync(request, timeout.Token); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        { throw new HttpRequestException("Embedding service request timed out."); }
        using (response)
        {
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Embedding service returned HTTP {(int)response.StatusCode}.");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(timeout.Token));
        JsonElement embedding;
        var root = json.RootElement;
        if (root.TryGetProperty("embedding", out embedding)) { }
        else if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array &&
                 data.GetArrayLength() > 0 && data[0].TryGetProperty("embedding", out embedding)) { }
        else throw new InvalidDataException("Embedding service response did not contain an embedding vector.");
        var vector = embedding.EnumerateArray().Select(x => x.GetSingle()).ToArray();
        if (vector.Length == 0 || vector.Any(x => float.IsNaN(x) || float.IsInfinity(x)))
            throw new InvalidDataException("Embedding service returned an invalid vector.");
        return vector;
        }
    }
}

public sealed class LocalSemanticEmbeddingService : IEmbeddingService
{
    public Task<float[]> CreateAsync(string text, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var vector = new float[96];
        foreach (var token in Tokenize(text))
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            var index = BitConverter.ToUInt16(hash, 0) % vector.Length;
            vector[index] += (hash[2] & 1) == 0 ? 1 : -1;
        }
        var magnitude = Math.Sqrt(vector.Sum(x => x * x));
        if (magnitude > 0)
            for (var i = 0; i < vector.Length; i++) vector[i] = (float)(vector[i] / magnitude);
        return Task.FromResult(vector);
    }

    private static IEnumerable<string> Tokenize(string text) =>
        text.ToLowerInvariant().Split([' ', '\r', '\n', '\t', ',', '.', ';', ':', '(', ')', '[', ']', '"', '\''],
            StringSplitOptions.RemoveEmptyEntries).Where(x => x.Length > 2);
}

public interface IPrecedentLibraryService
{
    Task<PrecedentIndexJob> QueueCaseNoteAsync(int noteId, CancellationToken ct = default);
    Task<PrecedentIndexJob> QueueCaseOutcomeAsync(int caseId, CancellationToken ct = default);
    Task<PrecedentIndexJob> QueueArticleAsync(int articleId, CancellationToken ct = default);
    Task WithdrawMatterAsync(int caseId, string reason, CancellationToken ct = default);
    Task<int> ProcessBacklogAsync(int maximum = 20, CancellationToken ct = default);
    Task ProcessJobAsync(int jobId, CancellationToken ct = default);
    Task ReviewFlagAsync(int flagId, PrecedentFlagStatus decision, int reviewerId, string? note, CancellationToken ct = default);
    Task CommissionAsync(int subjectId, int administratorId, string brief, DateTime? dueAtUtc, CancellationToken ct = default);
    Task CompleteCommissionAsync(int commissionId, int administratorId, CancellationToken ct = default);
    Task<PrecedentDashboardViewModel> DashboardAsync(CancellationToken ct = default);
}

public sealed class PrecedentLibraryService(ApplicationDbContext db, IEmbeddingService embeddings) : IPrecedentLibraryService
{
    public async Task<PrecedentIndexJob> QueueCaseNoteAsync(int noteId, CancellationToken ct = default)
    {
        var note = await db.CaseNotes.SingleAsync(x => x.Id == noteId, ct);
        var matter = await db.Cases.SingleAsync(x => x.Id == note.CaseId, ct);
        return await QueueAsync(PrecedentSourceType.CaseNote, note.Id, $"Case note · {matter.CaseNumber}", note.Content,
            matter.CaseType, null, matter.Status == CaseStatus.Archived, note.IsPrivileged, note.IsConfidential, ct);
    }

    public async Task<PrecedentIndexJob> QueueCaseOutcomeAsync(int caseId, CancellationToken ct = default)
    {
        var matter = await db.Cases.SingleAsync(x => x.Id == caseId, ct);
        var text = string.IsNullOrWhiteSpace(matter.OutcomeSummary)
            ? $"{matter.Title}. Matter outcome: {matter.RecordedOutcome}. {matter.Description}"
            : matter.OutcomeSummary;
        return await QueueAsync(PrecedentSourceType.CaseOutcome, matter.Id, $"Case outcome · {matter.CaseNumber}", text,
            matter.CaseType, null, matter.Status == CaseStatus.Archived, matter.OutcomeIsPrivileged, matter.OutcomeIsConfidential, ct);
    }

    public async Task<PrecedentIndexJob> QueueArticleAsync(int articleId, CancellationToken ct = default)
    {
        var article = await db.KnowledgeArticles.SingleAsync(x => x.Id == articleId, ct);
        return await QueueAsync(PrecedentSourceType.KnowledgeArticle, article.Id, article.Title, article.Content, null,
            article.SuggestedSubjectId, article.Status != KnowledgeArticleStatus.Published,
            article.IsPrivileged, article.IsConfidential, ct);
    }

    private async Task<PrecedentIndexJob> QueueAsync(PrecedentSourceType type, int id, string title, string text,
        string? matterType, int? subjectId, bool archived, bool privileged, bool confidential, CancellationToken ct)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text.Trim())));
        var existing = await db.PrecedentIndexJobs.SingleOrDefaultAsync(x =>
            x.SourceType == type && x.SourceId == id && x.ContentHash == hash, ct);
        var exclusion = archived ? "Archived or unpublished content is not eligible for the precedent library."
            : privileged ? "Privileged content is withheld from the precedent index."
            : confidential ? "Confidential content is withheld from the precedent index."
            : string.IsNullOrWhiteSpace(text) ? "Empty content cannot be indexed." : null;
        if (existing != null)
        {
            existing.IsArchived = archived; existing.IsPrivileged = privileged; existing.IsConfidential = confidential;
            if (exclusion != null)
            {
                existing.Status = PrecedentJobStatus.Excluded; existing.ExclusionReason = exclusion;
                existing.CompletedAtUtc = DateTime.UtcNow; existing.NextAttemptAtUtc = null;
                await WithdrawSourceAsync(type, id, exclusion, ct);
            }
            else if (existing.Status == PrecedentJobStatus.Excluded)
            {
                existing.Status = PrecedentJobStatus.Queued; existing.ExclusionReason = null;
                existing.CompletedAtUtc = null; existing.NextAttemptAtUtc = null;
            }
            await db.SaveChangesAsync(ct);
            return existing;
        }
        var job = new PrecedentIndexJob
        {
            SourceType = type, SourceId = id, ContentHash = hash, Title = title, SourceText = text,
            MatterType = matterType, SuggestedSubjectId = subjectId, IsArchived = archived,
            IsPrivileged = privileged, IsConfidential = confidential
        };
        if (exclusion != null)
        {
            job.Status = PrecedentJobStatus.Excluded;
            job.ExclusionReason = exclusion;
            job.CompletedAtUtc = DateTime.UtcNow;
        }
        db.PrecedentIndexJobs.Add(job);
        if (job.Status == PrecedentJobStatus.Excluded)
            await WithdrawSourceAsync(type, id, job.ExclusionReason!, ct);
        await db.SaveChangesAsync(ct);
        return job;
    }

    public async Task WithdrawMatterAsync(int caseId, string reason, CancellationToken ct = default)
    {
        var noteIds = await db.CaseNotes.Where(x => x.CaseId == caseId).Select(x => x.Id).ToListAsync(ct);
        var items = await db.PrecedentItems.Where(x => x.IsCurrent &&
            ((x.SourceType == PrecedentSourceType.CaseOutcome && x.SourceId == caseId) ||
             (x.SourceType == PrecedentSourceType.CaseNote && noteIds.Contains(x.SourceId)))).ToListAsync(ct);
        foreach (var item in items) Retire(item, reason);
        await db.SaveChangesAsync(ct);
    }

    private async Task WithdrawSourceAsync(PrecedentSourceType type, int id, string reason, CancellationToken ct)
    {
        foreach (var item in await db.PrecedentItems.Where(x => x.SourceType == type && x.SourceId == id && x.IsCurrent).ToListAsync(ct))
            Retire(item, reason);
    }

    private static void Retire(PrecedentItem item, string reason)
    {
        item.IsCurrent = false; item.RetiredAtUtc = DateTime.UtcNow; item.CuratorNote = reason;
    }

    public async Task<int> ProcessBacklogAsync(int maximum = 20, CancellationToken ct = default)
    {
        var stale = DateTime.UtcNow.AddMinutes(-10);
        var stuck = await db.PrecedentIndexJobs.Where(x => x.Status == PrecedentJobStatus.Processing &&
            x.ProcessingStartedAtUtc < stale).ToListAsync(ct);
        foreach (var job in stuck)
        {
            job.Status = PrecedentJobStatus.Queued;
            job.LastError = "Recovered after an interrupted indexing attempt.";
            // Keep this unequivocally due across providers with different timestamp precision.
            job.NextAttemptAtUtc = stale;
        }
        if (stuck.Count > 0) await db.SaveChangesAsync(ct);
        var ids = await db.PrecedentIndexJobs.Where(x => x.Status == PrecedentJobStatus.Queued &&
                (x.NextAttemptAtUtc == null || x.NextAttemptAtUtc <= DateTime.UtcNow))
            .OrderBy(x => x.QueuedAtUtc).Select(x => x.Id).Take(maximum).ToListAsync(ct);
        foreach (var id in ids) await ProcessJobAsync(id, ct);
        return ids.Count;
    }

    public async Task ProcessJobAsync(int jobId, CancellationToken ct = default)
    {
        var job = await db.PrecedentIndexJobs.SingleAsync(x => x.Id == jobId, ct);
        if (job.Status is PrecedentJobStatus.Indexed or PrecedentJobStatus.Excluded) return;
        job.Status = PrecedentJobStatus.Processing;
        job.ProcessingStartedAtUtc = DateTime.UtcNow;
        job.AttemptCount++;
        await db.SaveChangesAsync(ct);
        try
        {
            var subject = await ClassifyAsync(job, ct);
            var chunks = Chunk(job.SourceText).ToArray();
            var vectors = new List<float[]>(chunks.Length);
            foreach (var chunk in chunks) vectors.Add(await embeddings.CreateAsync(chunk, ct));
            var item = await db.PrecedentItems.SingleOrDefaultAsync(x =>
                x.SourceType == job.SourceType && x.SourceId == job.SourceId && x.ContentHash == job.ContentHash, ct);
            if (item == null)
            {
                item = new PrecedentItem
                {
                    SourceType = job.SourceType, SourceId = job.SourceId, ContentHash = job.ContentHash,
                    Title = job.Title, SourceText = job.SourceText, LegalSubjectId = subject.Id,
                    SourceDateUtc = job.QueuedAtUtc
                };
                db.PrecedentItems.Add(item);
                for (var i = 0; i < chunks.Length; i++)
                    db.PrecedentPassages.Add(new PrecedentPassage
                        { PrecedentItem = item, PassageNumber = i + 1, Text = chunks[i], EmbeddingJson = JsonSerializer.Serialize(vectors[i]) });
                await db.SaveChangesAsync(ct);
                foreach (var previous in await db.PrecedentItems.Where(x => x.Id != item.Id && x.SourceType == item.SourceType &&
                    x.SourceId == item.SourceId && x.IsCurrent).ToListAsync(ct))
                    Retire(previous, "Replaced by a newer saved version of the same source.");
                await db.SaveChangesAsync(ct);
            }
            await DetectConflictsAsync(item, vectors, ct);
            job.Status = PrecedentJobStatus.Indexed;
            job.CompletedAtUtc = DateTime.UtcNow;
            job.LastError = null;
            job.NextAttemptAtUtc = null;
            job.ProcessingStartedAtUtc = null;
            await db.SaveChangesAsync(ct);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            job.Status = PrecedentJobStatus.Queued;
            job.LastError = ex.Message[..Math.Min(ex.Message.Length, 1000)];
            job.NextAttemptAtUtc = DateTime.UtcNow.AddMinutes(Math.Min(60, Math.Pow(2, Math.Min(job.AttemptCount, 5))));
            job.ProcessingStartedAtUtc = null;
            var monthKey = DateTime.UtcNow.ToString("yyyyMM");
            foreach (var admin in await db.Users.Where(x => x.Role == UserRole.Admin && x.IsActive).Select(x => x.Id).ToListAsync())
                if (!await db.SystemNotifications.AnyAsync(x => x.DeduplicationKey == $"precedent-backlog-{admin}-{monthKey}"))
                    db.SystemNotifications.Add(new SystemNotification { UserId = admin, Type = "PrecedentBacklog",
                        Title = "Precedent indexing backlog", Message = "The embedding service is unavailable or returned invalid data. Content remains queued for retry.",
                        ActionUrl = "/Precedent", DeduplicationKey = $"precedent-backlog-{admin}-{monthKey}" });
            await db.SaveChangesAsync(CancellationToken.None);
        }
    }

    private async Task<LegalSubject> ClassifyAsync(PrecedentIndexJob job, CancellationToken ct)
    {
        if (job.SuggestedSubjectId.HasValue)
            return await db.LegalSubjects.SingleAsync(x => x.Id == job.SuggestedSubjectId && x.IsActive, ct);
        var subjects = await db.LegalSubjects.Where(x => x.IsActive).ToListAsync(ct);
        var haystack = $"{job.MatterType} {job.Title} {job.SourceText}".ToLowerInvariant();
        return subjects.OrderByDescending(s => s.Keywords.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Count(k => haystack.Contains(k.Trim().ToLowerInvariant())))
            .ThenBy(s => s.Name == "General Practice" ? 1 : 0).First();
    }

    private async Task DetectConflictsAsync(PrecedentItem item, List<float[]> newVectors, CancellationToken ct)
    {
        var existing = await db.PrecedentItems.Include(x => x.LegalSubject)
            .Where(x => x.Id != item.Id && x.LegalSubjectId == item.LegalSubjectId && x.IsCurrent)
            .ToListAsync(ct);
        var cues = new[] { "supersede", "overrule", "replaced", "no longer", "contrary", "inconsistent", "distinguish", "revised" };
        var hasCue = cues.Any(x => item.SourceText.Contains(x, StringComparison.OrdinalIgnoreCase));
        foreach (var old in existing)
        {
            if (await db.PrecedentConflictFlags.AnyAsync(x =>
                x.NewPrecedentItemId == item.Id && x.ExistingPrecedentItemId == old.Id, ct)) continue;
            var oldVectors = await db.PrecedentPassages.Where(x => x.PrecedentItemId == old.Id)
                .Select(x => x.EmbeddingJson).ToListAsync(ct);
            var similarity = oldVectors.Select(x => JsonSerializer.Deserialize<float[]>(x))
                .Where(x => x != null).SelectMany(o => newVectors.Select(n => Cosine(o!, n))).DefaultIfEmpty(0).Max();
            if (similarity < .30 || (!hasCue && similarity < .62)) continue;
            db.PrecedentConflictFlags.Add(new PrecedentConflictFlag
            {
                NewPrecedentItemId = item.Id, ExistingPrecedentItemId = old.Id,
                Similarity = (decimal)similarity,
                Reason = hasCue
                    ? "New work contains supersession or contradiction language and substantially overlaps this precedent."
                    : "New work is highly similar to this older precedent and may have overtaken it."
            });
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task ReviewFlagAsync(int flagId, PrecedentFlagStatus decision, int reviewerId, string? note, CancellationToken ct = default)
    {
        if (decision == PrecedentFlagStatus.Pending) throw new InvalidOperationException("Select a final curation decision.");
        if (string.IsNullOrWhiteSpace(note))
            throw new InvalidOperationException("A reason or amendment note is required for every curation decision.");
        var flag = await db.PrecedentConflictFlags.Include(x => x.ExistingPrecedentItem).SingleAsync(x => x.Id == flagId, ct);
        if (flag.Status != PrecedentFlagStatus.Pending) throw new InvalidOperationException("This flag has already been reviewed.");
        flag.Status = decision; flag.ReviewedByUserId = reviewerId; flag.ReviewedAtUtc = DateTime.UtcNow; flag.ReviewNote = note;
        if (decision == PrecedentFlagStatus.Retired)
        {
            flag.ExistingPrecedentItem.IsCurrent = false;
            flag.ExistingPrecedentItem.RetiredAtUtc = DateTime.UtcNow;
        }
        if (decision == PrecedentFlagStatus.Amended)
            flag.ExistingPrecedentItem.CuratorNote = note;
        db.AuditEntries.Add(new AuditEntry { ActorUserId = reviewerId, EntityType = nameof(PrecedentConflictFlag),
            EntityId = flag.Id.ToString(), Action = $"Precedent {decision}",
            SafeMetadataJson = JsonSerializer.Serialize(new { flag.ExistingPrecedentItemId, flag.NewPrecedentItemId }) });
        await db.SaveChangesAsync(ct);
    }

    public async Task CommissionAsync(int subjectId, int administratorId, string brief, DateTime? dueAtUtc, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(brief)) throw new InvalidOperationException("A commissioning brief is required.");
        if (await db.CoverageCommissions.AnyAsync(x => x.LegalSubjectId == subjectId &&
            (x.Status == CoverageCommissionStatus.Commissioned || x.Status == CoverageCommissionStatus.InProgress), ct))
            throw new InvalidOperationException("This subject already has an open commission.");
        db.CoverageCommissions.Add(new CoverageCommission
            { LegalSubjectId = subjectId, CommissionedByUserId = administratorId, Brief = brief.Trim(), DueAtUtc = dueAtUtc });
        db.AuditEntries.Add(new AuditEntry { ActorUserId = administratorId, EntityType = nameof(CoverageCommission),
            EntityId = subjectId.ToString(), Action = "Coverage material commissioned" });
        await db.SaveChangesAsync(ct);
    }

    public async Task CompleteCommissionAsync(int commissionId, int administratorId, CancellationToken ct = default)
    {
        var commission = await db.CoverageCommissions.SingleAsync(x => x.Id == commissionId, ct);
        if (commission.Status is CoverageCommissionStatus.Completed or CoverageCommissionStatus.Cancelled)
            throw new InvalidOperationException("This commission is already closed.");
        commission.Status = CoverageCommissionStatus.Completed; commission.CompletedAtUtc = DateTime.UtcNow;
        db.AuditEntries.Add(new AuditEntry { ActorUserId = administratorId, EntityType = nameof(CoverageCommission),
            EntityId = commission.Id.ToString(), Action = "Coverage commission completed" });
        await db.SaveChangesAsync(ct);
    }

    public async Task<PrecedentDashboardViewModel> DashboardAsync(CancellationToken ct = default)
    {
        var subjects = await db.LegalSubjects.Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync(ct);
        var items = await db.PrecedentItems.AsNoTracking().ToListAsync(ct);
        var commissions = await db.CoverageCommissions.Include(x => x.LegalSubject)
            .Where(x => x.Status == CoverageCommissionStatus.Commissioned || x.Status == CoverageCommissionStatus.InProgress).ToListAsync(ct);
        return new PrecedentDashboardViewModel
        {
            Coverage = subjects.Select(s => new SubjectCoverageRow(s, items.Count(x => x.LegalSubjectId == s.Id && x.IsCurrent),
                items.Count(x => x.LegalSubjectId == s.Id), commissions.FirstOrDefault(x => x.LegalSubjectId == s.Id))).ToList(),
            Flags = await db.PrecedentConflictFlags.Include(x => x.NewPrecedentItem).Include(x => x.ExistingPrecedentItem)
                .Where(x => x.Status == PrecedentFlagStatus.Pending).OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct),
            ReviewedFlags = await db.PrecedentConflictFlags.Include(x => x.NewPrecedentItem).Include(x => x.ExistingPrecedentItem)
                .Where(x => x.Status != PrecedentFlagStatus.Pending).OrderByDescending(x => x.ReviewedAtUtc).Take(20).ToListAsync(ct),
            Backlog = await db.PrecedentIndexJobs.Where(x => x.Status == PrecedentJobStatus.Queued || x.Status == PrecedentJobStatus.Failed)
                .OrderBy(x => x.QueuedAtUtc).Take(100).ToListAsync(ct),
            RecentItems = await db.PrecedentItems.Include(x => x.LegalSubject).OrderByDescending(x => x.IndexedAtUtc).Take(12).ToListAsync(ct)
            ,ExcludedCount = await db.PrecedentIndexJobs.CountAsync(x => x.Status == PrecedentJobStatus.Excluded, ct)
            ,IndexedCount = await db.PrecedentIndexJobs.CountAsync(x => x.Status == PrecedentJobStatus.Indexed, ct)
        };
    }

    internal static IEnumerable<string> Chunk(string text, int maximum = 1200, int overlap = 120)
    {
        text = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (text.Length == 0) yield break;
        for (var start = 0; start < text.Length;)
        {
            var length = Math.Min(maximum, text.Length - start);
            if (start + length < text.Length)
            {
                var boundary = text.LastIndexOfAny(['.', ';', ' '], start + length - 1, length);
                if (boundary > start + maximum / 2) length = boundary - start + 1;
            }
            yield return text.Substring(start, length).Trim();
            if (start + length >= text.Length) break;
            start += Math.Max(1, length - overlap);
        }
    }

    private static double Cosine(float[] left, float[] right)
    {
        double dot = 0, a = 0, b = 0;
        for (var i = 0; i < Math.Min(left.Length, right.Length); i++) { dot += left[i] * right[i]; a += left[i] * left[i]; b += right[i] * right[i]; }
        return a == 0 || b == 0 ? 0 : dot / Math.Sqrt(a * b);
    }
}

public sealed class PrecedentIndexWorker(IServiceScopeFactory scopeFactory, ILogger<PrecedentIndexWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<IPrecedentLibraryService>().ProcessBacklogAsync(20, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex) { logger.LogError(ex, "Precedent indexing cycle failed; queued work remains available for retry."); }
            try { await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
