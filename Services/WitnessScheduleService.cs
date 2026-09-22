using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.DTOs.Witnesses;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services.Notifications;

namespace SimplexLawFirm.Services
{
    /// <summary>
    /// Business logic for UC-09: Detect Witness Schedule Conflicts.
    /// Compares each case's court date against every linked witness's recorded
    /// commitments, raises persistent Critical/Advisory alerts, notifies the
    /// attorney, rechecks after updates, and writes the resolution to the
    /// case timeline.
    /// </summary>
    public class WitnessScheduleService : IWitnessScheduleService
    {
        private readonly ApplicationDbContext _db;
        private readonly IAuditService _audit;
        private readonly INotificationService _notifications;

        // Calendar event types treated as court dates for conflict purposes.
        private static readonly EventType[] CourtEventTypes = { EventType.CourtAppearance, EventType.Hearing };

        public WitnessScheduleService(
            ApplicationDbContext db,
            IAuditService audit,
            INotificationService notifications)
        {
            _db = db;
            _audit = audit;
            _notifications = notifications;
        }

        public async Task<CaseWitness> AddWitnessAsync(int userId, CreateWitnessDto dto)
        {
            var matter = await _db.Cases.FirstOrDefaultAsync(c => c.Id == dto.CaseId)
                ?? throw new InvalidOperationException("Matter not found.");

            var witness = new CaseWitness
            {
                CaseId = matter.Id,
                FullName = dto.FullName.Trim(),
                Email = dto.Email?.Trim(),
                Phone = dto.Phone?.Trim(),
                IsRequired = dto.IsRequired,
                Notes = dto.Notes?.Trim()
            };
            _db.Set<CaseWitness>().Add(witness);
            await _db.SaveChangesAsync();

            await _audit.LogAsync("CaseWitness", witness.Id.ToString(), "Added",
                $"Witness '{witness.FullName}' linked to case {matter.CaseNumber}.", userId);

            await CheckCaseAsync(matter.Id, userId);
            return witness;
        }

        public async Task<WitnessCommitment> AddCommitmentAsync(int userId, CreateCommitmentDto dto)
        {
            var witness = await _db.Set<CaseWitness>().FirstOrDefaultAsync(w => w.Id == dto.CaseWitnessId)
                ?? throw new InvalidOperationException("Witness not found.");

            if (dto.EndDateTime <= dto.StartDateTime)
                throw new InvalidOperationException("The commitment end must be after its start.");

            var commitment = new WitnessCommitment
            {
                CaseWitnessId = witness.Id,
                Title = dto.Title.Trim(),
                Kind = dto.Kind.Trim(),
                StartDateTime = dto.StartDateTime,
                EndDateTime = dto.EndDateTime,
                Details = dto.Details?.Trim()
            };
            _db.Set<WitnessCommitment>().Add(commitment);
            await _db.SaveChangesAsync();

            // Availability changed, so recheck this case immediately (trigger 2).
            await CheckCaseAsync(witness.CaseId, userId);
            return commitment;
        }

        public async Task RemoveCommitmentAsync(int userId, int commitmentId)
        {
            var commitment = await _db.Set<WitnessCommitment>()
                .Include(c => c.CaseWitness)
                .FirstOrDefaultAsync(c => c.Id == commitmentId)
                ?? throw new InvalidOperationException("Commitment not found.");

            commitment.IsActive = false;
            await _db.SaveChangesAsync();

            await CheckCaseAsync(commitment.CaseWitness!.CaseId, userId);
        }

        // ── The detection engine (flow steps 2-8) ──────────────────────────
        public async Task<List<WitnessConflictDto>> CheckCaseAsync(int caseId, int? actorUserId)
        {
            var matter = await _db.Cases.FirstOrDefaultAsync(c => c.Id == caseId)
                ?? throw new InvalidOperationException("Matter not found.");

            var witnesses = await _db.Set<CaseWitness>()
                .Include(w => w.Commitments)
                .Where(w => w.CaseId == caseId && w.IsActive)
                .ToListAsync();

            var courtEvents = await _db.CalendarEvents
                .Where(e => e.CaseId == caseId
                    && e.Status != EventStatus.Cancelled
                    && CourtEventTypes.Contains(e.Type))
                .ToListAsync();

            var activeConflicts = await _db.Set<WitnessScheduleConflict>()
                .Where(c => c.CaseId == caseId && c.Status == ConflictStatus.Active)
                .ToListAsync();

            var detectedKeys = new HashSet<string>();
            var newConflicts = new List<WitnessScheduleConflict>();

            foreach (var courtEvent in courtEvents)
            {
                foreach (var witness in witnesses)
                {
                    // Step 4-5: compare the court window against each commitment.
                    var clash = witness.Commitments
                        .Where(c => c.IsActive)
                        .FirstOrDefault(c => c.StartDateTime < courtEvent.EndDateTime
                                          && c.EndDateTime > courtEvent.StartDateTime);

                    if (clash is null) continue;

                    var key = $"{witness.Id}:{courtEvent.Id}:{clash.Id}";
                    detectedKeys.Add(key);

                    if (activeConflicts.Any(c => c.CaseWitnessId == witness.Id
                        && c.CourtEventId == courtEvent.Id
                        && c.WitnessCommitmentId == clash.Id))
                        continue; // already tracked

                    var severity = witness.IsRequired ? ConflictSeverity.Critical : ConflictSeverity.Advisory;
                    var conflict = new WitnessScheduleConflict
                    {
                        CaseId = caseId,
                        CaseWitnessId = witness.Id,
                        CourtEventId = courtEvent.Id,
                        WitnessCommitmentId = clash.Id,
                        Severity = severity,
                        Status = ConflictStatus.Active,
                        CourtDateStart = courtEvent.StartDateTime,
                        CourtDateEnd = courtEvent.EndDateTime,
                        ConflictingEventSummary = $"{clash.Title} ({clash.Kind}) {clash.StartDateTime:dd MMM yyyy HH:mm}-{clash.EndDateTime:HH:mm}",
                        DetectedAtUtc = DateTime.UtcNow
                    };
                    _db.Set<WitnessScheduleConflict>().Add(conflict);
                    newConflicts.Add(conflict);
                }
            }

            // Steps 11-12: conflicts whose clash no longer exists clear automatically.
            var cleared = activeConflicts
                .Where(c => !detectedKeys.Contains($"{c.CaseWitnessId}:{c.CourtEventId}:{c.WitnessCommitmentId}"))
                .ToList();
            foreach (var stale in cleared)
            {
                stale.Status = ConflictStatus.Resolved;
                stale.ResolvedAtUtc = DateTime.UtcNow;
                stale.ResolutionNotes ??= "Schedule updated; the conflict no longer exists.";
            }

            await _db.SaveChangesAsync();

            // Steps 7-8: flag Critical and notify the attorney of new conflicts.
            if (newConflicts.Count > 0 && matter.LawyerId.HasValue)
            {
                var critical = newConflicts.Count(c => c.Severity == ConflictSeverity.Critical);
                var names = string.Join(", ", newConflicts.Select(c =>
                    witnesses.First(w => w.Id == c.CaseWitnessId).FullName));

                await _notifications.SendAsync(matter.LawyerId.Value,
                    critical > 0 ? "CRITICAL: Witness Schedule Conflict" : "Witness Schedule Conflict",
                    $"Case {matter.CaseNumber}: {newConflicts.Count} conflict(s) detected ({critical} critical) affecting {names}.");
            }

            foreach (var conflict in newConflicts)
            {
                var witness = witnesses.First(w => w.Id == conflict.CaseWitnessId);
                await _audit.LogAsync("WitnessScheduleConflict", conflict.Id.ToString(), "Detected",
                    $"Case {matter.CaseNumber}: witness '{witness.FullName}' conflicts with court date " +
                    $"{conflict.CourtDateStart:dd MMM yyyy HH:mm} due to {conflict.ConflictingEventSummary}. " +
                    $"Severity: {conflict.Severity}.", actorUserId);
                await TimelineAsync(caseId,
                    $"Witness conflict detected: {witness.FullName} unavailable at court date {conflict.CourtDateStart:dd MMM yyyy HH:mm} ({conflict.ConflictingEventSummary}).");
            }

            foreach (var stale in cleared)
            {
                await _audit.LogAsync("WitnessScheduleConflict", stale.Id.ToString(), "AutoResolved",
                    "Conflict cleared after schedule update.", actorUserId);
                await TimelineAsync(caseId, "A witness schedule conflict was cleared after an availability update.");
            }

            return await GetForCaseAsync(caseId);
        }

        // ── Attorney resolves after making alternative arrangements (step 10, 13) ──
        public async Task<WitnessConflictDto> ResolveConflictAsync(int userId, ResolveConflictDto dto)
        {
            var conflict = await _db.Set<WitnessScheduleConflict>()
                .Include(c => c.Case)
                .Include(c => c.CaseWitness)
                .FirstOrDefaultAsync(c => c.Id == dto.ConflictId)
                ?? throw new InvalidOperationException("Conflict not found.");

            conflict.Status = ConflictStatus.Resolved;
            conflict.ResolvedAtUtc = DateTime.UtcNow;
            conflict.ResolvedByUserId = userId;
            conflict.ResolutionNotes = dto.ResolutionNotes.Trim();
            await _db.SaveChangesAsync();

            await _audit.LogAsync("WitnessScheduleConflict", conflict.Id.ToString(), "Resolved",
                $"Case {conflict.Case?.CaseNumber}: {dto.ResolutionNotes}", userId);
            await TimelineAsync(conflict.CaseId,
                $"Witness conflict resolved for {conflict.CaseWitness?.FullName}: {dto.ResolutionNotes}");

            return (await GetForCaseAsync(conflict.CaseId)).First(c => c.ConflictId == conflict.Id);
        }

        public async Task<List<WitnessConflictDto>> GetActiveConflictsAsync(int? caseId = null) =>
            (await GetForCaseInternal(caseId)).Where(c => c.Status == "Active").ToList();

        public async Task<List<WitnessConflictDto>> GetForCaseAsync(int caseId) =>
            await GetForCaseInternal(caseId);

        public async Task<List<CaseWitness>> GetWitnessesForCaseAsync(int caseId) =>
            await _db.Set<CaseWitness>()
                .Include(w => w.Commitments)
                .Where(w => w.CaseId == caseId && w.IsActive)
                .OrderBy(w => w.FullName)
                .ToListAsync();

        private async Task<List<WitnessConflictDto>> GetForCaseInternal(int? caseId)
        {
            var query = _db.Set<WitnessScheduleConflict>()
                .Include(c => c.Case)
                .Include(c => c.CaseWitness)
                .AsQueryable();
            if (caseId.HasValue) query = query.Where(c => c.CaseId == caseId.Value);

            return await query
                .OrderByDescending(c => c.Status == ConflictStatus.Active)
                .ThenByDescending(c => c.Severity)
                .ThenBy(c => c.CourtDateStart)
                .Select(c => new WitnessConflictDto
                {
                    ConflictId = c.Id,
                    CaseId = c.CaseId,
                    CaseNumber = c.Case!.CaseNumber,
                    WitnessName = c.CaseWitness!.FullName,
                    WitnessIsRequired = c.CaseWitness.IsRequired,
                    Severity = c.Severity.ToString(),
                    Status = c.Status.ToString(),
                    CourtDateStart = c.CourtDateStart,
                    ConflictingEventSummary = c.ConflictingEventSummary,
                    DetectedAtUtc = c.DetectedAtUtc,
                    ResolutionNotes = c.ResolutionNotes
                })
                .ToListAsync();
        }

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
    }
}
