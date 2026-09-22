using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.DTOs.CourtFiling;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services.Notifications;

namespace SimplexLawFirm.Services
{
    /// <summary>Business logic for UC-07: Manage Court Filing Queue.</summary>
    public class CourtFilingQueueService : ICourtFilingQueueService
    {
        private readonly ApplicationDbContext _db;
        private readonly IAuditService _audit;
        private readonly INotificationService _notifications;

        public CourtFilingQueueService(
            ApplicationDbContext db,
            IAuditService audit,
            INotificationService notifications)
        {
            _db = db;
            _audit = audit;
            _notifications = notifications;
        }

        // Steps 1-6: submit document and run all checks immediately.
        public async Task<CourtFilingDetailDto> SubmitAsync(int attorneyId, SubmitCourtFilingDto dto)
        {
            var matter = await _db.Cases
                .FirstOrDefaultAsync(c => c.Id == dto.CaseId)
                ?? throw new InvalidOperationException("Matter not found.");

            if (matter.Status != CaseStatus.Active)
                throw new InvalidOperationException("Only active matters can enter the court filing queue.");

            var submittingAttorney = await _db.Users
                .FirstOrDefaultAsync(u => u.Id == attorneyId && u.IsActive)
                ?? throw new InvalidOperationException("Submitting attorney not found.");

            var attorneyOfRecordId = matter.LawyerId;
            var expectedStep = await GetNextProcedureStepAsync(dto.CaseId, matter.CaseType);

            var failures = new List<string>();

            // Step 4: expected document type.
            if (expectedStep is null)
            {
                failures.Add(nameof(CourtFilingFailureReason.NoProceduralSequence));
            }
            else if (!string.Equals(expectedStep.DocumentType, dto.DocumentType, StringComparison.OrdinalIgnoreCase))
            {
                failures.Add($"{CourtFilingFailureReason.WrongDocumentType}: expected '{expectedStep.DocumentType}', received '{dto.DocumentType}'.");
            }

            // Step 5: case number extracted/received from document metadata.
            if (!string.Equals(dto.SubmittedCaseNumber.Trim(), matter.CaseNumber.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                failures.Add($"{CourtFilingFailureReason.CaseNumberMismatch}: document contains '{dto.SubmittedCaseNumber}', matter is '{matter.CaseNumber}'.");
            }

            // Step 6: correct attorney of record.
            if (!attorneyOfRecordId.HasValue || attorneyOfRecordId.Value != attorneyId)
            {
                failures.Add($"{CourtFilingFailureReason.AttorneyNotOfRecord}: submitting attorney is not the attorney of record.");
            }

            // Step 6: signature check.
            if (!dto.SignatureDetected || !string.Equals(dto.SignedByAttorneyName.Trim(), submittingAttorney.FullName.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                failures.Add($"{CourtFilingFailureReason.SignatureMissing}: the document is not signed by the attorney of record.");
            }

            var submission = new CourtFilingSubmission
            {
                CaseId = dto.CaseId,
                SubmittedByAttorneyId = attorneyId,
                DocumentId = dto.DocumentId,
                DocumentType = dto.DocumentType.Trim(),
                SubmittedCaseNumber = dto.SubmittedCaseNumber.Trim(),
                SignedByAttorneyName = dto.SignedByAttorneyName.Trim(),
                SignatureDetected = dto.SignatureDetected,
                AttorneyOfRecordId = attorneyOfRecordId,
                ExpectedDocumentType = expectedStep?.DocumentType,
                Status = failures.Count == 0 ? CourtFilingStatus.Validated : CourtFilingStatus.ReturnedToAttorney,
                ValidationFailuresJson = JsonSerializer.Serialize(failures),
                ReturnReason = failures.Count == 0 ? null : string.Join(" ", failures),
                SubmittedAtUtc = DateTime.UtcNow,
                ValidatedAtUtc = failures.Count == 0 ? DateTime.UtcNow : null,
                ReturnedAtUtc = failures.Count == 0 ? null : DateTime.UtcNow,
                ValidatedByUserId = failures.Count == 0 ? attorneyId : null,
                CourtReference = dto.CourtReference,
                FilingNotes = dto.FilingNotes
            };

            _db.Set<CourtFilingSubmission>().Add(submission);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(
                "CourtFilingSubmission",
                submission.Id.ToString(),
                failures.Count == 0 ? "Validated" : "ReturnedToAttorney",
                $"UC07 submission for case {matter.CaseNumber}. Document type: {dto.DocumentType}. " +
                (failures.Count == 0 ? "All checks passed." : $"Failures: {string.Join(" | ", failures)}"),
                attorneyId);

            await NotifyRelevantPartiesAsync(
                matter,
                failures.Count == 0 ? "Court Filing Validated" : "Court Filing Returned",
                failures.Count == 0
                    ? $"{dto.DocumentType} passed validation and is ready to file."
                    : $"{dto.DocumentType} was returned for correction: {string.Join(" ", failures)}");

            return await MapAsync(submission.Id);
        }

        // Optional explicit validation endpoint for a second reviewer.
        public async Task<CourtFilingDetailDto> ValidateAsync(int reviewerId, ValidateCourtFilingDto dto)
        {
            var submission = await _db.Set<CourtFilingSubmission>()
                .Include(x => x.Case)
                .FirstOrDefaultAsync(x => x.Id == dto.SubmissionId)
                ?? throw new InvalidOperationException("Court filing submission not found.");

            if (submission.Status != CourtFilingStatus.Validated)
                throw new InvalidOperationException("Only validated submissions can be filed.");

            submission.ValidatedByUserId = reviewerId;
            submission.ValidatedAtUtc ??= DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return await MapAsync(submission.Id);
        }

        // Step 7: manually return a submission with an explanation.
        public async Task<CourtFilingDetailDto> ReturnToAttorneyAsync(int reviewerId, ReturnCourtFilingDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Reason))
                throw new InvalidOperationException("A return reason is required.");

            var submission = await _db.Set<CourtFilingSubmission>()
                .Include(x => x.Case)
                .FirstOrDefaultAsync(x => x.Id == dto.SubmissionId)
                ?? throw new InvalidOperationException("Court filing submission not found.");

            submission.Status = CourtFilingStatus.ReturnedToAttorney;
            submission.ReturnReason = dto.Reason.Trim();
            submission.ReturnedAtUtc = DateTime.UtcNow;
            submission.ValidationFailuresJson = JsonSerializer.Serialize(new[] { dto.Reason.Trim() });
            await _db.SaveChangesAsync();

            await _audit.LogAsync(
                "CourtFilingSubmission",
                submission.Id.ToString(),
                "ReturnedToAttorney",
                $"Reviewer returned court filing: {dto.Reason.Trim()}",
                reviewerId);

            if (submission.SubmittedByAttorneyId > 0)
                await _notifications.SendAsync(
                    submission.SubmittedByAttorneyId,
                    "Court Filing Requires Correction",
                    $"The filing for case {submission.Case?.CaseNumber} was returned: {dto.Reason.Trim()}");

            return await MapAsync(submission.Id);
        }

        // Step 9-10: mark a validated document as filed and advance the sequence.
        public async Task<CourtFilingDetailDto> MarkFiledAsync(int userId, int submissionId, string? courtReference)
        {
            var submission = await _db.Set<CourtFilingSubmission>()
                .Include(x => x.Case)
                .FirstOrDefaultAsync(x => x.Id == submissionId)
                ?? throw new InvalidOperationException("Court filing submission not found.");

            if (submission.Status != CourtFilingStatus.Validated)
                throw new InvalidOperationException("Only validated submissions can be marked as filed.");

            submission.Status = CourtFilingStatus.Filed;
            submission.FiledAtUtc = DateTime.UtcNow;
            submission.CourtReference = string.IsNullOrWhiteSpace(courtReference)
                ? submission.CourtReference
                : courtReference.Trim();
            submission.TimelineEntry = $"{submission.DocumentType} filed on {submission.FiledAtUtc:yyyy-MM-dd HH:mm} UTC by {submission.SignedByAttorneyName}.";

            await _db.SaveChangesAsync();

            await _audit.LogAsync(
                "CourtFilingSubmission",
                submission.Id.ToString(),
                "Filed",
                $"{submission.TimelineEntry} Court reference: {submission.CourtReference ?? "N/A"}.",
                userId);

            await NotifyRelevantPartiesAsync(
                submission.Case!,
                "Court Document Filed",
                submission.TimelineEntry);

            return await MapAsync(submission.Id);
        }

        public async Task<CourtFilingDetailDto?> GetByIdAsync(int id)
        {
            return await _db.Set<CourtFilingSubmission>().AnyAsync(x => x.Id == id)
                ? await MapAsync(id)
                : null;
        }

        public async Task<List<CourtFilingDetailDto>> GetQueueAsync(int? caseId = null)
        {
            var ids = await _db.Set<CourtFilingSubmission>()
                .Where(x => (!caseId.HasValue || x.CaseId == caseId.Value)
                    && x.Status != CourtFilingStatus.Filed
                    && x.Status != CourtFilingStatus.Withdrawn)
                .OrderBy(x => x.SubmittedAtUtc)
                .Select(x => x.Id)
                .ToListAsync();

            var result = new List<CourtFilingDetailDto>();
            foreach (var id in ids) result.Add(await MapAsync(id));
            return result;
        }

        public async Task<List<CourtFilingDetailDto>> GetForCaseAsync(int caseId)
        {
            var ids = await _db.Set<CourtFilingSubmission>()
                .Where(x => x.CaseId == caseId)
                .OrderByDescending(x => x.SubmittedAtUtc)
                .Select(x => x.Id)
                .ToListAsync();

            var result = new List<CourtFilingDetailDto>();
            foreach (var id in ids) result.Add(await MapAsync(id));
            return result;
        }

        private async Task<CourtProcedureStep?> GetNextProcedureStepAsync(int caseId, string caseType)
        {
            var filedTypes = await _db.Set<CourtFilingSubmission>()
                .Where(x => x.CaseId == caseId && x.Status == CourtFilingStatus.Filed)
                .Select(x => x.DocumentType)
                .ToListAsync();

            return await _db.Set<CourtProcedureStep>()
                .Where(x => x.IsActive && x.IsRequired && x.CaseType == caseType && !filedTypes.Contains(x.DocumentType))
                .OrderBy(x => x.SequenceNumber)
                .FirstOrDefaultAsync();
        }

        private async Task NotifyRelevantPartiesAsync(Case matter, string title, string message)
        {
            if (matter.LawyerId.HasValue)
                await _notifications.SendAsync(matter.LawyerId.Value, title, message);

            var directors = await _db.Users
                .Where(u => u.Role == UserRole.Director && u.IsActive)
                .Select(u => u.Id)
                .ToListAsync();

            foreach (var directorId in directors)
                await _notifications.SendAsync(directorId, title, message);
        }

        private async Task<CourtFilingDetailDto> MapAsync(int id)
        {
            var x = await _db.Set<CourtFilingSubmission>()
                .Include(s => s.Case)
                .FirstAsync(s => s.Id == id);

            var failures = string.IsNullOrWhiteSpace(x.ValidationFailuresJson)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(x.ValidationFailuresJson) ?? new List<string>();

            return new CourtFilingDetailDto
            {
                Id = x.Id,
                CaseId = x.CaseId,
                CaseNumber = x.Case?.CaseNumber ?? string.Empty,
                CaseTitle = x.Case?.Title ?? string.Empty,
                DocumentType = x.DocumentType,
                SubmittedCaseNumber = x.SubmittedCaseNumber,
                SignedByAttorneyName = x.SignedByAttorneyName,
                SignatureDetected = x.SignatureDetected,
                ExpectedDocumentType = x.ExpectedDocumentType,
                Status = x.Status.ToString(),
                ValidationFailures = failures,
                ReturnReason = x.ReturnReason,
                CourtReference = x.CourtReference,
                SubmittedAtUtc = x.SubmittedAtUtc,
                FiledAtUtc = x.FiledAtUtc,
                TimelineEntry = x.TimelineEntry
            };
        }
    }
}
