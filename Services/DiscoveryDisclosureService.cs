using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.DTOs.Discovery;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services.Notifications;
using SimplexLawFirm.Services.Email;
using Microsoft.Extensions.DependencyInjection;
using SimplexLawFirm.Services.Storage;

namespace SimplexLawFirm.Services
{
    /// <summary>
    /// Business logic for UC-08: Discovery and Disclosure Exchange.
    /// Bundle assembly, Director privilege sign-off, secure time-limited external
    /// exchange, reciprocal opposing-party upload, and the compliance check.
    /// </summary>
    public class DiscoveryDisclosureService : IDiscoveryDisclosureService
    {
        private readonly ApplicationDbContext _db;
        private readonly IAuditService _audit;
        private readonly INotificationService _notifications;
        private readonly IWebHostEnvironment _env;
        private readonly IDocumentStorage? _documents;
        private readonly IEmailService? _email;

        private const int MaxOpposingFiles = 10;
        private const long MaxOpposingFileBytes = 50L * 1024 * 1024; // 50 MB
        private const int DefaultLinkValidityHours = 72;

        [ActivatorUtilitiesConstructor]
        public DiscoveryDisclosureService(
            ApplicationDbContext db,
            IAuditService audit,
            INotificationService notifications,
            IWebHostEnvironment env,
            IDocumentStorage documents)
        {
            _db = db;
            _audit = audit;
            _notifications = notifications;
            _env = env;
            _documents = documents;
        }

        public DiscoveryDisclosureService(ApplicationDbContext db, INotificationService notifications, IEmailService email)
        {
            _db = db;
            _audit = new AuditService(db);
            _notifications = notifications;
            _email = email;
            _env = null!;
        }

        public async Task<DiscoveryBundle> CreateAsync(int caseId, int attorneyId, string opposingName, string opposingEmail, IEnumerable<int> documentIds, IDictionary<int, string> privilegeReasons)
        {
            var documents = await _db.Documents.Where(d => d.CaseId == caseId && documentIds.Contains(d.Id)).ToListAsync();
            if (documents.Count == 0) throw new InvalidOperationException("Select at least one matter document.");
            var bundle = new DiscoveryBundle { CaseId = caseId, AttorneyId = attorneyId, OpposingAttorneyName = opposingName, OpposingAttorneyEmail = opposingEmail, ExchangeToken = NewToken(), Status = privilegeReasons.Count == 0 ? DiscoveryBundleStatus.ReadyForExchange : DiscoveryBundleStatus.AwaitingPrivilegeReview };
            foreach (var document in documents)
            {
                var privileged = privilegeReasons.TryGetValue(document.Id, out var reason);
                bundle.Items.Add(new DiscoveryBundleItem { DocumentId = document.Id, ProposedPrivileged = privileged, PrivilegeReason = reason, PrivilegeDecision = privileged ? PrivilegeDecision.Pending : PrivilegeDecision.NotFlagged });
            }
            _db.DiscoveryBundles.Add(bundle); await _db.SaveChangesAsync(); return bundle;
        }

        public async Task DecideAsync(int itemId, int directorId, bool withhold, string reason)
        {
            var item = await _db.DiscoveryBundleItems.Include(x => x.DiscoveryBundle).FirstAsync(x => x.Id == itemId);
            item.DirectorId = directorId; item.DirectorReason = reason; item.DecidedAtUtc = DateTime.UtcNow; item.PrivilegeDecision = withhold ? PrivilegeDecision.WithholdApproved : PrivilegeDecision.OverruledDisclose;
            // Exclude the decision currently in the change tracker: querying before
            // SaveChanges would otherwise still see its old Pending database value.
            if (!await _db.DiscoveryBundleItems.AnyAsync(x => x.DiscoveryBundleId == item.DiscoveryBundleId && x.Id != item.Id && x.PrivilegeDecision == PrivilegeDecision.Pending)) item.DiscoveryBundle.Status = DiscoveryBundleStatus.ReadyForExchange;
            await _db.SaveChangesAsync();
        }

        public async Task FinaliseAsync(int bundleId, int attorneyId, string exchangeUrl)
        {
            var bundle = await _db.DiscoveryBundles.FirstAsync(x => x.Id == bundleId && x.AttorneyId == attorneyId);
            bundle.Status = DiscoveryBundleStatus.Exchanged; bundle.ExchangedAtUtc = DateTime.UtcNow; bundle.ExchangeExpiresAtUtc = DateTime.UtcNow.AddHours(DefaultLinkValidityHours);
            if (_email is not null) await _email.QueueAsync(bundle.OpposingAttorneyEmail, "Discovery disclosure exchange", exchangeUrl, exchangeUrl, $"discovery:{bundle.Id}");
            await _db.SaveChangesAsync();
        }

        public async Task MarkAccessedAsync(string token) { var bundle = await _db.DiscoveryBundles.FirstAsync(x => x.ExchangeToken == token); bundle.AccessedAtUtc ??= DateTime.UtcNow; await _db.SaveChangesAsync(); }
        public async Task RecordOpposingBundleAsync(int bundleId, IEnumerable<(string FileName, string RelativePath, string ContentType, long SizeBytes)> files) { var bundle = await _db.DiscoveryBundles.FirstAsync(x => x.Id == bundleId); foreach (var file in files) _db.OpposingDisclosureDocuments.Add(new OpposingDisclosureDocument { DiscoveryBundleId = bundleId, FileName = file.FileName, RelativePath = file.RelativePath, ContentType = file.ContentType, SizeBytes = file.SizeBytes }); bundle.OpposingBundleReceivedAtUtc = DateTime.UtcNow; bundle.Status = DiscoveryBundleStatus.Received; await _db.SaveChangesAsync(); }
        public async Task SetComplianceAsync(int bundleId, int attorneyId, bool compliant, string notes) { var bundle = await _db.DiscoveryBundles.FirstAsync(x => x.Id == bundleId && x.AttorneyId == attorneyId); bundle.Status = compliant ? DiscoveryBundleStatus.Compliant : DiscoveryBundleStatus.GapsIdentified; bundle.ComplianceNotes = notes; await _db.SaveChangesAsync(); }
        public async Task EscalateGapsAsync(int bundleId, int attorneyId) { var bundle = await _db.DiscoveryBundles.FirstAsync(x => x.Id == bundleId && x.AttorneyId == attorneyId); if (bundle.Status != DiscoveryBundleStatus.GapsIdentified) throw new InvalidOperationException("Only a disclosure gap can be escalated."); bundle.Status = DiscoveryBundleStatus.EscalatedFormalRequest; if (_email is not null) await _email.QueueAsync(bundle.OpposingAttorneyEmail, "Formal request for missing disclosure", bundle.ComplianceNotes ?? "Missing disclosure", bundle.ComplianceNotes ?? "Missing disclosure", $"discovery-gap:{bundle.Id}"); await _db.SaveChangesAsync(); }

        // ── Attorney: compile the bundle (flow step 1) ─────────────────────
        public async Task<DiscoveryBundleDetailDto> CreateBundleAsync(int attorneyId, CreateDiscoveryBundleDto dto)
        {
            var matter = await _db.Cases.FirstOrDefaultAsync(c => c.Id == dto.CaseId)
                ?? throw new InvalidOperationException("Matter not found.");

            if (matter.LawyerId != attorneyId)
                throw new InvalidOperationException("Only the assigned attorney can compile a discovery bundle for this matter.");

            if (matter.Status != CaseStatus.Active)
                throw new InvalidOperationException("Discovery bundles can only be compiled on active matters.");

            var documents = await _db.Documents
                .Where(d => d.CaseId == dto.CaseId && dto.DocumentIds.Contains(d.Id))
                .ToListAsync();

            if (documents.Count == 0)
                throw new InvalidOperationException("Select at least one matter document to disclose.");

            foreach (var privilegedId in dto.PrivilegedDocumentIds.Distinct())
            {
                if (!dto.DocumentIds.Contains(privilegedId))
                    throw new InvalidOperationException("A document flagged as privileged must also be selected for the bundle.");

                if (!dto.PrivilegeReasons.TryGetValue(privilegedId, out var reason) || string.IsNullOrWhiteSpace(reason))
                    throw new InvalidOperationException("A stated reason is required for every document proposed to be withheld under privilege.");
            }

            var bundle = new DiscoveryBundle
            {
                CaseId = matter.Id,
                AttorneyId = attorneyId,
                OpposingAttorneyName = dto.OpposingAttorneyName.Trim(),
                OpposingAttorneyEmail = dto.OpposingAttorneyEmail.Trim(),
                ExchangeToken = NewToken(),
                Status = DiscoveryBundleStatus.Draft
            };

            foreach (var document in documents)
            {
                var flagged = dto.PrivilegedDocumentIds.Contains(document.Id);
                bundle.Items.Add(new DiscoveryBundleItem
                {
                    DocumentId = document.Id,
                    ProposedPrivileged = flagged,
                    PrivilegeReason = flagged ? dto.PrivilegeReasons[document.Id].Trim() : null,
                    PrivilegeDecision = flagged ? PrivilegeDecision.Pending : PrivilegeDecision.NotFlagged
                });
            }

            // Flow step 2: nothing flagged finalises automatically.
            if (bundle.Items.All(i => i.PrivilegeDecision != PrivilegeDecision.Pending))
                bundle.Status = DiscoveryBundleStatus.ReadyForExchange;
            else
                bundle.Status = DiscoveryBundleStatus.PendingPrivilegeReview;

            _db.Set<DiscoveryBundle>().Add(bundle);
            await _db.SaveChangesAsync();

            await _audit.LogAsync("DiscoveryBundle", bundle.Id.ToString(), "Compiled",
                $"UC08 bundle compiled for case {matter.CaseNumber}. {documents.Count} documents, " +
                $"{bundle.Items.Count(i => i.PrivilegeDecision == PrivilegeDecision.Pending)} flagged for privilege review.",
                attorneyId);

            if (bundle.Status == DiscoveryBundleStatus.PendingPrivilegeReview)
            {
                var directors = await _db.Users
                    .Where(u => u.IsActive && u.Role == UserRole.Director)
                    .Select(u => u.Id).ToListAsync();
                foreach (var directorId in directors)
                    await _notifications.SendAsync(directorId, "Privilege Review Required",
                        $"Case {matter.CaseNumber}: {bundle.Items.Count(i => i.PrivilegeDecision == PrivilegeDecision.Pending)} document(s) await your privilege decision.");
            }

            return await MapAsync(bundle.Id);
        }

        // ── Director: approve or overrule each flagged document (flow step 2) ──
        public async Task<DiscoveryBundleDetailDto> DecidePrivilegeAsync(int directorId, PrivilegeDecisionDto dto)
        {
            var item = await _db.Set<DiscoveryBundleItem>()
                .Include(i => i.DiscoveryBundle).ThenInclude(b => b!.Case)
                .Include(i => i.Document)
                .FirstOrDefaultAsync(i => i.Id == dto.ItemId)
                ?? throw new InvalidOperationException("Bundle item not found.");

            if (item.PrivilegeDecision != PrivilegeDecision.Pending)
                throw new InvalidOperationException("This document has already been decided.");

            item.PrivilegeDecision = dto.Withhold ? PrivilegeDecision.WithholdApproved : PrivilegeDecision.OverruledDisclose;
            item.DirectorId = directorId;
            item.DirectorReason = dto.Reason.Trim();
            item.DecidedAtUtc = DateTime.UtcNow;

            var bundle = item.DiscoveryBundle!;
            await _db.SaveChangesAsync();

            // Bundle finalises once every flagged item has a decision.
            var stillPending = await _db.Set<DiscoveryBundleItem>()
                .AnyAsync(i => i.DiscoveryBundleId == bundle.Id && i.PrivilegeDecision == PrivilegeDecision.Pending);

            if (!stillPending && bundle.Status == DiscoveryBundleStatus.PendingPrivilegeReview)
            {
                bundle.Status = DiscoveryBundleStatus.ReadyForExchange;
                await _db.SaveChangesAsync();

                await _notifications.SendAsync(bundle.AttorneyId, "Discovery Bundle Finalised",
                    $"Case {bundle.Case?.CaseNumber}: privilege review complete. The bundle is ready for secure exchange.");
            }

            await _audit.LogAsync("DiscoveryBundleItem", item.Id.ToString(),
                dto.Withhold ? "PrivilegeWithholdApproved" : "PrivilegeOverruled",
                $"Document '{item.Document?.FileName}' in case {bundle.Case?.CaseNumber}. Director reason: {dto.Reason}",
                directorId);

            return await MapAsync(bundle.Id);
        }

        // ── Attorney: generate the secure, time-limited link and send it (step 3) ──
        public async Task<DiscoveryBundleDetailDto> FinaliseAndExchangeAsync(int attorneyId, int bundleId, int linkValidityHours = DefaultLinkValidityHours)
        {
            var bundle = await _db.Set<DiscoveryBundle>()
                .Include(b => b.Case)
                .Include(b => b.Items)
                .FirstOrDefaultAsync(b => b.Id == bundleId)
                ?? throw new InvalidOperationException("Discovery bundle not found.");

            if (bundle.AttorneyId != attorneyId)
                throw new InvalidOperationException("Only the compiling attorney can send the exchange link.");

            if (bundle.Status != DiscoveryBundleStatus.ReadyForExchange)
                throw new InvalidOperationException("The bundle is not ready for exchange. Resolve privilege review first.");

            bundle.ExchangeToken = NewToken();
            bundle.ExchangeExpiresAtUtc = DateTime.UtcNow.AddHours(Math.Clamp(linkValidityHours, 1, 24 * 14));
            bundle.ExchangedAtUtc = DateTime.UtcNow;
            bundle.Status = DiscoveryBundleStatus.Exchanged;
            await _db.SaveChangesAsync();

            var link = $"/DiscoveryDisclosure/Portal?token={bundle.ExchangeToken}";

            _db.Set<EmailOutboxMessage>().Add(new EmailOutboxMessage
            {
                ToAddress = bundle.OpposingAttorneyEmail,
                Subject = $"Secure disclosure exchange - case {bundle.Case?.CaseNumber}",
                TextBody = $"Dear {bundle.OpposingAttorneyName},\n\nA disclosure bundle for case {bundle.Case?.CaseNumber} " +
                           $"is available through our secure exchange portal:\n\n{link}\n\nThe link expires on " +
                           $"{bundle.ExchangeExpiresAtUtc:dd MMM yyyy HH:mm} UTC. You may download the disclosed material " +
                           "and submit your reciprocal disclosure through the same portal.\n\nSimplex Law Firm",
                HtmlBody = null!,
                Status = EmailOutboxStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow,
                NextAttemptAtUtc = DateTime.UtcNow,
                DeduplicationKey = $"discovery-exchange-{bundle.Id}-{bundle.ExchangedAtUtc.Value.Ticks}"
            });
            await _db.SaveChangesAsync();

            await _audit.LogAsync("DiscoveryBundle", bundle.Id.ToString(), "Exchanged",
                $"Secure exchange link issued to {bundle.OpposingAttorneyEmail}, expires {bundle.ExchangeExpiresAtUtc:u}.",
                attorneyId);

            await TimelineAsync(bundle.CaseId, $"Discovery bundle exchanged with {bundle.OpposingAttorneyName} via secure link (expires {bundle.ExchangeExpiresAtUtc:dd MMM yyyy}).");

            return await MapAsync(bundle.Id);
        }

        // ── External portal: token validation, download logging, reciprocal upload (steps 4-5) ──
        public async Task<DiscoveryBundle?> GetPortalBundleAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;
            return await _db.Set<DiscoveryBundle>()
                .Include(b => b.Case)
                .Include(b => b.Items).ThenInclude(i => i.Document)
                .FirstOrDefaultAsync(b => b.ExchangeToken == token);
        }

        public async Task<bool> TryRecordPortalAccessAsync(string token)
        {
            var bundle = await _db.Set<DiscoveryBundle>().FirstOrDefaultAsync(b => b.ExchangeToken == token);
            if (bundle is null || !PortalOpen(bundle)) return false;

            var firstAccess = bundle.AccessedAtUtc is null;
            bundle.AccessedAtUtc ??= DateTime.UtcNow;
            await _db.SaveChangesAsync();

            if (firstAccess)
            {
                await _audit.LogAsync("DiscoveryBundle", bundle.Id.ToString(), "PortalAccessed",
                    $"Opposing party opened the exchange portal.", actorUserId: null);
                await TimelineAsync(bundle.CaseId, "Opposing party accessed the secure disclosure portal.");
            }
            return true;
        }

        public async Task<(bool Success, string? Error)> SubmitOpposingBundleAsync(
            string token, IEnumerable<(string FileName, string ContentType, long SizeBytes, Stream Content)> files)
        {
            var bundle = await _db.Set<DiscoveryBundle>()
                .Include(b => b.Case)
                .FirstOrDefaultAsync(b => b.ExchangeToken == token);
            if (bundle is null || !PortalOpen(bundle))
                return (false, "This exchange link is invalid or has expired.");

            var uploads = files.ToList();
            if (uploads.Count == 0) return (false, "Attach at least one file.");
            if (uploads.Count > MaxOpposingFiles) return (false, $"A maximum of {MaxOpposingFiles} files is allowed.");
            if (uploads.Any(f => f.SizeBytes > MaxOpposingFileBytes)) return (false, "Each file may be up to 50 MB.");

            var folder = Path.Combine(_env.ContentRootPath, "App_Data", "opposing-disclosure", bundle.Id.ToString());
            Directory.CreateDirectory(folder);

            foreach (var file in uploads)
            {
                var safeName = Path.GetFileName(file.FileName);
                var storedName = $"{Guid.NewGuid():N}_{safeName}";
                var fullPath = Path.Combine(folder, storedName);
                await using (var fs = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write))
                    await file.Content.CopyToAsync(fs);

                _db.Set<OpposingDisclosureDocument>().Add(new OpposingDisclosureDocument
                {
                    DiscoveryBundleId = bundle.Id,
                    FileName = safeName,
                    RelativePath = fullPath,
                    ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                    SizeBytes = file.SizeBytes,
                    UploadedAtUtc = DateTime.UtcNow
                });
            }

            bundle.OpposingBundleReceivedAtUtc = DateTime.UtcNow;
            bundle.Status = DiscoveryBundleStatus.OpposingBundleReceived;
            await _db.SaveChangesAsync();

            // Step 5: notify our attorney that the reciprocal disclosure arrived.
            await _notifications.SendAsync(bundle.AttorneyId, "Opposing Disclosure Received",
                $"Case {bundle.Case?.CaseNumber}: {uploads.Count} document(s) received from {bundle.OpposingAttorneyName}. Run the compliance check.");

            await _audit.LogAsync("DiscoveryBundle", bundle.Id.ToString(), "OpposingBundleReceived",
                $"{uploads.Count} file(s) uploaded through the secure portal.", actorUserId: null);
            await TimelineAsync(bundle.CaseId, $"Opposing disclosure bundle received ({uploads.Count} files) via secure portal.");

            return (true, null);
        }

        public async Task<(Stream? Stream, string? FileName, string? ContentType)> GetDisclosedDocumentStreamAsync(string token, int documentId)
        {
            var bundle = await GetPortalBundleAsync(token);
            if (bundle is null || !PortalOpen(bundle)) return (null, null, null);

            // Only documents the Director overruled (or that were never flagged) are downloadable.
            var item = bundle.Items.FirstOrDefault(i => i.DocumentId == documentId
                && (i.PrivilegeDecision == PrivilegeDecision.OverruledDisclose || i.PrivilegeDecision == PrivilegeDecision.NotFlagged));
            var path = item?.Document?.FilePath;
            if (path is null) return (null, null, null);

            try
            {
                // Matter documents are stored as paths relative to the secure
                // document store.  They must never be treated as web-root or
                // process-relative paths here.
                var stream = _documents is not null
                    ? await _documents.OpenReadAsync(path)
                    : File.OpenRead(path); // legacy constructor used by tests
                return (stream, item!.Document!.FileName,
                    item.Document.FileType ?? "application/octet-stream");
            }
            catch (FileNotFoundException) { return (null, null, null); }
            catch (DirectoryNotFoundException) { return (null, null, null); }
        }

        public async Task<(Stream? Stream, string? FileName)> GetOpposingDocumentStreamAsync(int userId, int opposingDocumentId, string role)
        {
            var doc = await _db.Set<OpposingDisclosureDocument>()
                .Include(d => d.DiscoveryBundle)
                .FirstOrDefaultAsync(d => d.Id == opposingDocumentId);
            if (doc?.DiscoveryBundle is null) return (null, null);

            var isAttorney = doc.DiscoveryBundle.AttorneyId == userId;
            var isDirector = role is "Director" or "Admin";
            if (!isAttorney && !isDirector) return (null, null);
            if (!System.IO.File.Exists(doc.RelativePath)) return (null, null);

            return (File.OpenRead(doc.RelativePath), doc.FileName);
        }

        // ── Attorney: compliance check (steps 6-7) ─────────────────────────
        public async Task<DiscoveryBundleDetailDto> RecordComplianceCheckAsync(int attorneyId, ComplianceCheckDto dto)
        {
            var bundle = await _db.Set<DiscoveryBundle>()
                .Include(b => b.Case)
                .FirstOrDefaultAsync(b => b.Id == dto.BundleId)
                ?? throw new InvalidOperationException("Discovery bundle not found.");

            if (bundle.AttorneyId != attorneyId)
                throw new InvalidOperationException("Only the assigned attorney can record the compliance check.");

            if (bundle.OpposingBundleReceivedAtUtc is null)
                throw new InvalidOperationException("No opposing disclosure has been received yet.");

            bundle.ComplianceNotes = dto.Notes.Trim();
            bundle.Status = dto.Compliant ? DiscoveryBundleStatus.Compliant : DiscoveryBundleStatus.GapsIdentified;
            await _db.SaveChangesAsync();

            await _audit.LogAsync("DiscoveryBundle", bundle.Id.ToString(),
                dto.Compliant ? "Compliant" : "GapsIdentified",
                $"Compliance check on case {bundle.Case?.CaseNumber}. Notes: {dto.Notes}", attorneyId);
            await TimelineAsync(bundle.CaseId, dto.Compliant
                ? "Discovery compliance check passed: opposing disclosure is complete."
                : "Discovery compliance check identified gaps in the opposing disclosure.");

            return await MapAsync(bundle.Id);
        }

        // ── Attorney: escalate gaps with a formal request (step 8) ─────────
        public async Task<DiscoveryBundleDetailDto> EscalateMissingDisclosureAsync(int attorneyId, int bundleId)
        {
            var bundle = await _db.Set<DiscoveryBundle>()
                .Include(b => b.Case)
                .FirstOrDefaultAsync(b => b.Id == bundleId)
                ?? throw new InvalidOperationException("Discovery bundle not found.");

            if (bundle.AttorneyId != attorneyId)
                throw new InvalidOperationException("Only the assigned attorney can escalate the gaps.");

            if (bundle.Status != DiscoveryBundleStatus.GapsIdentified)
                throw new InvalidOperationException("Escalation is only available after gaps have been identified.");

            bundle.Status = DiscoveryBundleStatus.EscalatedFormalRequest;
            await _db.SaveChangesAsync();

            _db.Set<EmailOutboxMessage>().Add(new EmailOutboxMessage
            {
                ToAddress = bundle.OpposingAttorneyEmail,
                Subject = $"Formal request for outstanding disclosure - case {bundle.Case?.CaseNumber}",
                TextBody = $"Dear {bundle.OpposingAttorneyName},\n\nOur compliance review of your disclosure for case " +
                           $"{bundle.Case?.CaseNumber} identified documents believed outstanding:\n\n{bundle.ComplianceNotes}\n\n" +
                           "Please submit the outstanding disclosure through the existing secure portal link, or contact our offices.\n\nSimplex Law Firm",
                HtmlBody = null!,
                Status = EmailOutboxStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow,
                NextAttemptAtUtc = DateTime.UtcNow,
                DeduplicationKey = $"discovery-escalation-{bundle.Id}-{DateTime.UtcNow.Ticks}"
            });
            await _db.SaveChangesAsync();

            await _audit.LogAsync("DiscoveryBundle", bundle.Id.ToString(), "EscalatedFormalRequest",
                $"Formal request for missing disclosure sent to {bundle.OpposingAttorneyEmail}.", attorneyId);
            await TimelineAsync(bundle.CaseId, "Formal request sent to opposing party for outstanding disclosure.");

            return await MapAsync(bundle.Id);
        }

        // ── Queries ────────────────────────────────────────────────────────
        public async Task<List<DiscoveryBundle>> GetIndexForRoleAsync(string role, int userId)
        {
            var query = _db.Set<DiscoveryBundle>().Include(b => b.Case).AsQueryable();
            if (role == "Lawyer")
                query = query.Where(b => b.AttorneyId == userId);
            else if (role is "Director" or "Admin")
                query = query.Where(b => b.Items.Any(i => i.PrivilegeDecision == PrivilegeDecision.Pending)
                    || b.Status != DiscoveryBundleStatus.Draft);
            return await query.OrderByDescending(b => b.Id).ToListAsync();
        }

        public async Task<DiscoveryBundle?> GetDetailsAsync(int id) =>
            await _db.Set<DiscoveryBundle>()
                .Include(b => b.Case)
                .Include(b => b.Items).ThenInclude(i => i.Document)
                .Include(b => b.OpposingDocuments)
                .FirstOrDefaultAsync(b => b.Id == id);

        public async Task<Case?> GetCaseForCreateAsync(int caseId, int attorneyId) =>
            await _db.Cases.FirstOrDefaultAsync(c => c.Id == caseId && c.LawyerId == attorneyId);

        // ── Internals ──────────────────────────────────────────────────────
        private static bool PortalOpen(DiscoveryBundle bundle) =>
            bundle.ExchangedAtUtc is not null
            && bundle.Status is not DiscoveryBundleStatus.Closed
            && (bundle.ExchangeExpiresAtUtc is null || bundle.ExchangeExpiresAtUtc > DateTime.UtcNow);

        private static string NewToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

        private async Task TimelineAsync(int caseId, string text)
        {
            _db.Set<CaseNote>().Add(new CaseNote
            {
                CaseId = caseId,
                Content = text,
                CreatedAt = DateTime.UtcNow,
                IsConfidential = false,
                IsPrivileged = false
            });
            await _db.SaveChangesAsync();
        }

        private async Task<DiscoveryBundleDetailDto> MapAsync(int id)
        {
            var bundle = await _db.Set<DiscoveryBundle>()
                .Include(b => b.Case)
                .Include(b => b.Items).ThenInclude(i => i.Document)
                .Include(b => b.OpposingDocuments)
                .FirstAsync(b => b.Id == id);

            return new DiscoveryBundleDetailDto
            {
                Id = bundle.Id,
                CaseId = bundle.CaseId,
                CaseNumber = bundle.Case?.CaseNumber ?? string.Empty,
                CaseTitle = bundle.Case?.Title ?? string.Empty,
                Status = bundle.Status.ToString(),
                OpposingAttorneyName = bundle.OpposingAttorneyName,
                OpposingAttorneyEmail = bundle.OpposingAttorneyEmail,
                ExchangeExpiresAtUtc = bundle.ExchangeExpiresAtUtc,
                ExchangedAtUtc = bundle.ExchangedAtUtc,
                AccessedAtUtc = bundle.AccessedAtUtc,
                OpposingBundleReceivedAtUtc = bundle.OpposingBundleReceivedAtUtc,
                ComplianceNotes = bundle.ComplianceNotes,
                PendingPrivilegeCount = bundle.Items.Count(i => i.PrivilegeDecision == PrivilegeDecision.Pending),
                DisclosedCount = bundle.Items.Count(i => i.PrivilegeDecision is PrivilegeDecision.NotFlagged or PrivilegeDecision.OverruledDisclose),
                WithheldCount = bundle.Items.Count(i => i.PrivilegeDecision == PrivilegeDecision.WithholdApproved),
                OpposingDocumentCount = bundle.OpposingDocuments.Count,
                Items = bundle.Items.Select(i => new DiscoveryBundleItemDto
                {
                    ItemId = i.Id,
                    DocumentId = i.DocumentId,
                    FileName = i.Document?.FileName ?? string.Empty,
                    ProposedPrivileged = i.ProposedPrivileged,
                    PrivilegeReason = i.PrivilegeReason,
                    PrivilegeDecision = i.PrivilegeDecision.ToString(),
                    DirectorReason = i.DirectorReason,
                    DecidedAtUtc = i.DecidedAtUtc
                }).ToList()
            };
        }
    }
}
