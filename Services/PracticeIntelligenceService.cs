using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using SimplexLawFirm.Services.Notifications;
using SimplexLawFirm.ViewModels;

namespace SimplexLawFirm.Services;

public interface IPracticeIntelligenceService
{
    Task<CaseForecast> CreateForecastAsync(int caseId, int attorneyId, CancellationToken ct = default);
    Task LockForecastAsync(int forecastId, decimal assessmentPercent, bool agrees, string? notes, CancellationToken ct = default);
    Task ScoreForecastAsync(int caseId, ForecastResult outcome, CancellationToken ct = default);
    Task<CaseHandover> ApproveReassignmentAsync(int caseId, int receivingAttorneyId, int directorId, string reason, CancellationToken ct = default);
    Task RefreshHandoverAsync(CaseHandover handover, CancellationToken ct = default);
    Task<bool> MarkHandoverReadyAsync(int handoverId, CancellationToken ct = default);
    Task<ServiceComplaint> LodgeComplaintAsync(int clientId, LodgeComplaintViewModel input, CancellationToken ct = default);
}

public sealed class PracticeIntelligenceService(ApplicationDbContext db, INotificationService notifications) : IPracticeIntelligenceService
{
    public const int MinimumComparables = 3;

    public async Task<CaseForecast> CreateForecastAsync(int caseId, int attorneyId, CancellationToken ct = default)
    {
        var matter = await db.Cases.SingleAsync(x => x.Id == caseId, ct);
        if (matter.Status is CaseStatus.Closed or CaseStatus.Archived || matter.LawyerId != attorneyId)
            throw new InvalidOperationException("Only the assigned attorney may forecast an open matter.");
        if (await db.CaseForecasts.AnyAsync(x => x.CaseId == caseId && x.Status != ForecastStatus.Refused, ct))
            throw new InvalidOperationException("This matter already has a committed forecast.");

        var comparable = await db.Cases.Where(x => x.Id != caseId && x.CaseType == matter.CaseType &&
            x.Status == CaseStatus.Closed && x.RecordedOutcome != null).OrderByDescending(x => x.UpdatedAt).Take(20).ToListAsync(ct);
        var forecast = new CaseForecast { CaseId = caseId, AttorneyId = attorneyId, ComparableCount = comparable.Count };
        if (comparable.Count < MinimumComparables)
        {
            forecast.Status = ForecastStatus.Refused;
            forecast.RefusalReason = $"At least {MinimumComparables} comparable closed matters are required; only {comparable.Count} were found.";
            db.CaseForecasts.Add(forecast);
            await db.SaveChangesAsync(ct);
            await FulfilClientRequestsAsync(forecast, ct);
            return forecast;
        }

        var firmRate = comparable.Average(x => OutcomeValue(x.RecordedOutcome!.Value));
        var attorneyCases = comparable.Where(x => x.LawyerId == attorneyId).ToList();
        var attorneyRate = attorneyCases.Count == 0 ? firmRate : attorneyCases.Average(x => OutcomeValue(x.RecordedOutcome!.Value));
        var probability = Math.Clamp((firmRate * .40m) + (attorneyRate * .25m) + (matter.EvidenceStrength * .35m), 0, 1);
        var factors = new[] {
            new ForecastFactor("Comparable matter outcomes", .40m, $"{firmRate:P0} historical success"),
            new ForecastFactor("Assigned attorney record", .25m, $"{attorneyRate:P0} historical success"),
            new ForecastFactor("Evidence profile", .35m, $"{matter.EvidenceStrength:P0} strength")
        };
        forecast.Status = ForecastStatus.Draft;
        forecast.Probability = probability;
        forecast.ProbabilityBand = probability >= .70m ? "Strong prospects" : probability >= .45m ? "Balanced prospects" : "Limited prospects";
        forecast.ConfidenceLevel = comparable.Count >= 10 ? "High" : comparable.Count >= 6 ? "Moderate" : "Developing";
        forecast.FactorsJson = JsonSerializer.Serialize(factors);
        forecast.ComparableCasesJson = JsonSerializer.Serialize(comparable.Select(x => new ComparableMatter(x.CaseNumber, x.Title, x.RecordedOutcome!.Value)));
        db.CaseForecasts.Add(forecast);
        await db.SaveChangesAsync(ct);
        return forecast;
    }

    public async Task LockForecastAsync(int forecastId, decimal assessmentPercent, bool agrees, string? notes, CancellationToken ct = default)
    {
        var forecast = await db.CaseForecasts.SingleAsync(x => x.Id == forecastId, ct);
        if (forecast.Status != ForecastStatus.Draft || forecast.AttorneyAssessment != null)
            throw new InvalidOperationException("A committed assessment cannot be changed.");
        if (assessmentPercent is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(assessmentPercent), "The professional assessment must be between 0% and 100%.");
        forecast.AttorneyAssessment = assessmentPercent / 100m;
        forecast.AttorneyAgrees = agrees;
        forecast.AttorneyNotes = notes;
        forecast.Status = ForecastStatus.Locked;
        forecast.LockedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await FulfilClientRequestsAsync(forecast, ct);
    }

    public async Task ScoreForecastAsync(int caseId, ForecastResult outcome, CancellationToken ct = default)
    {
        var matter = await db.Cases.SingleAsync(x => x.Id == caseId, ct);
        if (matter.Status is CaseStatus.Closed or CaseStatus.Archived)
            throw new InvalidOperationException("Only an open matter can be scored.");
        if (!await db.CaseForecasts.AnyAsync(x => x.CaseId == caseId && x.Status == ForecastStatus.Locked, ct))
            throw new InvalidOperationException("A locked forecast is required before recording an outcome.");
        matter.RecordedOutcome = outcome;
        matter.Status = CaseStatus.Closed;
        var actual = OutcomeValue(outcome);
        foreach (var forecast in await db.CaseForecasts.Where(x => x.CaseId == caseId && x.Status == ForecastStatus.Locked).ToListAsync(ct))
        {
            forecast.ActualOutcome = outcome;
            forecast.AccuracyScore = 1 - Math.Abs((forecast.Probability ?? 0) - actual);
            forecast.Status = ForecastStatus.Scored;
            forecast.ScoredAtUtc = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);
        await UpdateCalibrationAsync(null, ct);
        foreach (var id in await db.CaseForecasts.Where(x => x.CaseId == caseId).Select(x => x.AttorneyId).Distinct().ToListAsync(ct))
            await UpdateCalibrationAsync(id, ct);
        var attorneyId = await db.CaseForecasts.Where(x => x.CaseId == caseId).Select(x => x.AttorneyId).FirstOrDefaultAsync(ct);
        var recent = await db.CaseForecasts.Where(x => x.AttorneyId == attorneyId && x.Status == ForecastStatus.Scored && x.AttorneyAssessment != null && x.ActualOutcome != null)
            .OrderByDescending(x => x.ScoredAtUtc).Take(5).ToListAsync(ct);
        if (recent.Count >= 3 && recent.Count(x => x.AttorneyAssessment! - OutcomeValue(x.ActualOutcome!.Value) > .20m) >= 3)
            foreach (var directorId in await db.Users.Where(x => x.Role == UserRole.Admin && x.IsActive).Select(x => x.Id).ToListAsync(ct))
                await notifications.QueueAsync(directorId, "ForecastCalibration", "Persistent forecast optimism", $"Attorney {attorneyId} has exceeded the optimism tolerance in at least three recent forecasts.", "/Practice", $"optimism-{attorneyId}-{DateTime.UtcNow:yyyyMM}", ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<CaseHandover> ApproveReassignmentAsync(int caseId, int receivingAttorneyId, int directorId, string reason, CancellationToken ct = default)
    {
        var matter = await db.Cases.SingleAsync(x => x.Id == caseId, ct);
        if (matter.Status is CaseStatus.Closed or CaseStatus.Archived) throw new InvalidOperationException("Only an active matter can be reassigned.");
        if (!matter.LawyerId.HasValue || matter.LawyerId == receivingAttorneyId) throw new InvalidOperationException("Select a different receiving attorney.");
        if (await db.CaseHandovers.AnyAsync(x => x.CaseId == caseId && x.Status != HandoverStatus.Accepted, ct))
            throw new InvalidOperationException("This matter already has an active handover.");
        var receiver = await db.Users.SingleAsync(x => x.Id == receivingAttorneyId && x.Role == UserRole.Lawyer && x.IsActive, ct);
        var director = await db.Users.SingleAsync(x => x.Id == directorId && x.Role == UserRole.Admin && x.IsActive, ct);
        var reassignment = new CaseReassignment { CaseId = caseId, OutgoingAttorneyId = matter.LawyerId.Value, ReceivingAttorneyId = receiver.Id, ApprovedByUserId = director.Id, Reason = reason, Status = ReassignmentStatus.Approved };
        db.CaseReassignments.Add(reassignment);
        await db.SaveChangesAsync(ct);
        var handover = new CaseHandover { CaseId = caseId, OutgoingAttorneyId = matter.LawyerId.Value, ReceivingAttorneyId = receiver.Id, DueAtUtc = DateTime.UtcNow.AddDays(2), CaseReassignmentId = reassignment.Id };
        db.CaseHandovers.Add(handover);
        reassignment.Status = ReassignmentStatus.HandoverPreparing;
        await db.SaveChangesAsync(ct);
        await RefreshHandoverAsync(handover, ct);
        return handover;
    }

    public async Task RefreshHandoverAsync(CaseHandover handover, CancellationToken ct = default)
    {
        await db.Entry(handover).Collection(x => x.Items).LoadAsync(ct);
        var live = new List<(string Type, string Text, bool Mandatory, bool Open, string Fingerprint)>();
        var deadlines = await db.CalendarEvents.Where(x => x.CaseId == handover.CaseId && x.Type == EventType.Deadline && x.Status != EventStatus.Completed && x.StartDateTime >= DateTime.Now).OrderBy(x => x.StartDateTime).ToListAsync(ct);
        live.Add(("Deadlines", $"{deadlines.Count} open deadline(s) require a recorded position", true, deadlines.Count > 0, string.Join('|', deadlines.Select(x => $"{x.Id}:{x.UpdatedAt:O}"))));
        var unbilled = await db.TimeEntries.CountAsync(x => x.CaseId == handover.CaseId && !x.IsBilled, ct);
        live.Add(("Unbilled time", $"{unbilled} unbilled time entr{(unbilled == 1 ? "y" : "ies")} must be billed or explained", true, unbilled > 0, unbilled.ToString()));
        var unsigned = await db.Retainers.CountAsync(x => x.CaseId == handover.CaseId && x.Status == RetainerStatus.AwaitingSignature, ct)
            + await db.Documents.CountAsync(x => x.CaseId == handover.CaseId && x.RequiresSignature && x.SignedAtUtc == null, ct);
        live.Add(("Signatures", $"{unsigned} document(s) awaiting signature", true, unsigned > 0, unsigned.ToString()));
        var appointments = await db.CalendarEvents.Where(x => x.CaseId == handover.CaseId && x.Type == EventType.Appointment && x.Status != EventStatus.Completed && x.StartDateTime >= DateTime.Now).OrderBy(x => x.StartDateTime).ToListAsync(ct);
        live.Add(("Appointments", $"{appointments.Count} scheduled appointment(s) must be briefed", false, appointments.Count > 0, string.Join('|', appointments.Select(x => $"{x.Id}:{x.UpdatedAt:O}"))));
        var unanswered = await db.ClientCorrespondence.CountAsync(x => x.CaseId == handover.CaseId && x.AnsweredAtUtc == null, ct);
        live.Add(("Client correspondence", $"{unanswered} client message(s) remain unanswered", true, unanswered > 0, unanswered.ToString()));

        foreach (var item in live)
        {
            var existing = handover.Items.SingleOrDefault(x => x.Type == item.Type);
            if (existing == null) handover.Items.Add(new HandoverItem { Type = item.Type, Description = item.Text, IsMandatory = item.Mandatory, IsResolved = !item.Open, SourceFingerprint = item.Fingerprint });
            else
            {
                existing.Description = item.Text;
                if (!item.Open) existing.IsResolved = true;
                else if (existing.SourceFingerprint != item.Fingerprint) { existing.IsResolved = false; existing.ResolutionNote = null; }
                existing.SourceFingerprint = item.Fingerprint;
            }
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> MarkHandoverReadyAsync(int handoverId, CancellationToken ct = default)
    {
        var handover = await db.CaseHandovers.Include(x => x.Items).SingleAsync(x => x.Id == handoverId, ct);
        if (handover.Status is not (HandoverStatus.Preparing or HandoverStatus.Overdue))
            throw new InvalidOperationException("Only a preparing or overdue handover can be marked ready.");
        await RefreshHandoverAsync(handover, ct);
        if (handover.Items.Any(x => x.IsMandatory && !x.IsResolved)) return false;
        handover.Status = HandoverStatus.Ready; handover.ReadyAtUtc = DateTime.UtcNow;
        await notifications.QueueAsync(handover.ReceivingAttorneyId, "Handover", "Handover ready", $"The handover for {handover.CaseId} is ready for acceptance.", $"/Practice/Handover/{handover.Id}", $"handover-ready-{handover.Id}", ct);
        foreach (var directorId in await db.Users.Where(x => x.Role == UserRole.Admin && x.IsActive).Select(x => x.Id).ToListAsync(ct))
            await notifications.QueueAsync(directorId, "Handover", "Handover ready", $"The handover for {handover.CaseId} is ready.", $"/Practice/Handover/{handover.Id}", $"handover-ready-director-{handover.Id}-{directorId}", ct);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<ServiceComplaint> LodgeComplaintAsync(int clientId, LodgeComplaintViewModel input, CancellationToken ct = default)
    {
        var matter = await db.Cases.Include(x => x.Client).SingleOrDefaultAsync(x => x.Id == input.CaseId && x.ClientId == clientId &&
            (x.Status == CaseStatus.Active || x.Status == CaseStatus.Closed), ct)
            ?? throw new InvalidOperationException("Choose one of your active or closed matters.");
        var duplicate = await db.ServiceComplaints.AnyAsync(x => x.ClientId == clientId && x.CaseId == input.CaseId && x.Category == input.Category && x.Status != ComplaintStatus.Resolved, ct);
        if (duplicate && !input.ConfirmPossibleDuplicate) throw new InvalidOperationException("A similar complaint is already open. Confirm before lodging another.");
        var directors = await db.Users.Where(x => x.Role == UserRole.Admin && x.IsActive).OrderBy(x => x.Id).ToListAsync(ct);
        if (directors.Count == 0) throw new InvalidOperationException("No independent senior reviewer is currently available.");
        var staff = await db.Users.Where(x => x.Role != UserRole.Client && x.IsActive).ToListAsync(ct);
        var namedStaff = staff.Where(x => input.Description.Contains(x.FullName, StringComparison.OrdinalIgnoreCase)).ToList();
        var namedDirector = directors.FirstOrDefault(x => namedStaff.Any(n => n.Id == x.Id));
        var reviewer = directors.FirstOrDefault(x => x.Id != namedDirector?.Id) ?? throw new InvalidOperationException("The named Director cannot review this complaint and no alternate reviewer is available.");
        var restricted = new HashSet<int>(namedStaff.Select(x => x.Id)); if (matter.LawyerId.HasValue) restricted.Add(matter.LawyerId.Value);
        var days = input.Category is ComplaintCategory.Conduct or ComplaintCategory.Billing ? 5 : 10;
        var complaint = new ServiceComplaint {
            ReferenceNumber = $"SLC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            CaseId = matter.Id, ClientId = clientId, Category = input.Category, Description = input.Description,
            RoutedToUserId = reviewer.Id, RestrictedUserIds = string.Join(',', restricted), ResponseDueAtUtc = DateTime.UtcNow.AddDays(days),
            DuplicateWarningAcknowledged = duplicate
        };
        db.ServiceComplaints.Add(complaint);
        await db.SaveChangesAsync(ct);
        foreach (var staffId in restricted)
            db.StaffServiceRecordEntries.Add(new StaffServiceRecordEntry { StaffUserId = staffId, ServiceComplaintId = complaint.Id, CaseId = matter.Id, Category = input.Category });
        await notifications.QueueAsync(reviewer.Id, "Complaint", "New confidential complaint", $"A {input.Category} complaint requires independent review.", $"/Practice/Complaints/{complaint.Id}", $"complaint-{complaint.ReferenceNumber}", ct);
        await db.SaveChangesAsync(ct);
        return complaint;
    }

    private static decimal OutcomeValue(ForecastResult outcome) => outcome switch { ForecastResult.Successful => 1m, ForecastResult.PartlySuccessful => .5m, _ => 0m };

    private async Task UpdateCalibrationAsync(int? attorneyId, CancellationToken ct)
    {
        var forecasts = await db.CaseForecasts.Where(x => x.Status == ForecastStatus.Scored && x.Probability != null && x.ActualOutcome != null && (attorneyId == null || x.AttorneyId == attorneyId)).ToListAsync(ct);
        var calibration = await db.ForecastCalibrations.SingleOrDefaultAsync(x => x.AttorneyId == attorneyId, ct);
        if (calibration == null) { calibration = new ForecastCalibration { AttorneyId = attorneyId }; db.ForecastCalibrations.Add(calibration); }
        calibration.ForecastCount = forecasts.Count;
        calibration.MeanAccuracy = forecasts.Count == 0 ? 0 : forecasts.Average(x => x.AccuracyScore ?? 0);
        calibration.MeanBias = forecasts.Count == 0 ? 0 : forecasts.Average(x => (x.Probability ?? 0) - OutcomeValue(x.ActualOutcome!.Value));
        calibration.OptimisticForecastCount = forecasts.Count(x => (x.Probability ?? 0) - OutcomeValue(x.ActualOutcome!.Value) > .20m);
        calibration.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private async Task FulfilClientRequestsAsync(CaseForecast forecast, CancellationToken ct)
    {
        var requests = await db.ClientForecastRequests.Include(x => x.Client).Where(x => x.CaseId == forecast.CaseId && x.Status == ForecastRequestStatus.Pending).ToListAsync(ct);
        foreach (var request in requests)
        {
            request.Status = ForecastRequestStatus.Fulfilled;
            request.FulfilledByForecastId = forecast.Id;
            request.FulfilledAtUtc = DateTime.UtcNow;
            var userId = await db.Users.Where(x => x.Role == UserRole.Client && x.Email.ToUpper() == request.Client.Email.ToUpper()).Select(x => (int?)x.Id).SingleOrDefaultAsync(ct);
            if (userId.HasValue)
                await notifications.QueueAsync(userId.Value, "ForecastAssessment", forecast.Status == ForecastStatus.Refused ? "Prospects assessment unavailable" : "Prospects assessment prepared", forecast.Status == ForecastStatus.Refused ? forecast.RefusalReason! : "The attorney has prepared an assessment of prospects for your matter.", "/Practice/Prospects", $"client-forecast-request-{request.Id}", ct);
        }
        await db.SaveChangesAsync(ct);
    }
}

public sealed class PracticeGovernanceWorker(IServiceScopeFactory scopeFactory, ILogger<PracticeGovernanceWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();
                var email = scope.ServiceProvider.GetRequiredService<SimplexLawFirm.Services.Email.IEmailService>();
                var directors = await db.Users.Where(x => x.Role == UserRole.Admin && x.IsActive).Select(x => x.Id).ToListAsync(stoppingToken);
                var lateComplaints = await db.ServiceComplaints.Where(x => x.Status == ComplaintStatus.Submitted && x.ResponseDueAtUtc < DateTime.UtcNow).ToListAsync(stoppingToken);
                foreach (var complaint in lateComplaints)
                {
                    complaint.Status = ComplaintStatus.Escalated;
                    await notifications.QueueAsync(complaint.RoutedToUserId, "ComplaintEscalation", "Complaint response overdue", $"{complaint.ReferenceNumber} has passed its response deadline.", $"/Practice/Complaints/{complaint.Id}", $"complaint-overdue-{complaint.Id}", stoppingToken);
                    var clientEmail = await db.Clients.Where(x => x.Id == complaint.ClientId).Select(x => x.Email).SingleAsync(stoppingToken);
                    var clientUserId = await db.Users.Where(x => x.Role == UserRole.Client && x.Email.ToUpper() == clientEmail.ToUpper()).Select(x => (int?)x.Id).SingleOrDefaultAsync(stoppingToken);
                    if (clientUserId.HasValue)
                        await notifications.QueueAsync(clientUserId.Value, "ComplaintDelay", "Your complaint response is delayed", $"{complaint.ReferenceNumber} is overdue and has been escalated.", $"/Practice/ComplaintReceipt/{complaint.Id}", $"complaint-delay-client-{complaint.Id}", stoppingToken);
                    await email.QueueAsync(clientEmail, $"Complaint response delayed: {complaint.ReferenceNumber}", $"<p>Your complaint response is delayed and has been escalated.</p><p><strong>{complaint.ReferenceNumber}</strong></p>", $"Your complaint {complaint.ReferenceNumber} is delayed and has been escalated.", $"complaint-delay-email-{complaint.Id}", stoppingToken);
                }
                var lateHandovers = await db.CaseHandovers.Where(x => x.Status == HandoverStatus.Preparing && x.DueAtUtc < DateTime.UtcNow).ToListAsync(stoppingToken);
                foreach (var handover in lateHandovers)
                {
                    handover.Status = HandoverStatus.Overdue;
                    foreach (var directorId in directors)
                        await notifications.QueueAsync(directorId, "HandoverEscalation", "Handover overdue", $"Handover {handover.Id} was not prepared in time.", $"/Practice/Handover/{handover.Id}", $"handover-overdue-{handover.Id}-{directorId}", stoppingToken);
                }
                await db.SaveChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Practice governance sweep failed."); }
            try { await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
