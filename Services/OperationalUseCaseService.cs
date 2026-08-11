using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SimplexLawFirm.Data;
using SimplexLawFirm.Models;
using SimplexLawFirm.Models.Beneficiaries;
using SimplexLawFirm.Services.Notifications;
using SimplexLawFirm.Services.Email;

namespace SimplexLawFirm.Services;

public interface IOperationalUseCaseService
{
    Task<IReadOnlyList<LegalAuthority>> ResearchAsync(int attorneyId, int caseId, string issue, int? caseNoteId = null, CancellationToken ct = default);
    Task<CaseAuthorityReliance> RelyAsync(int attorneyId, int caseId, int authorityId, string reason, bool confirmAdverseTreatment, CancellationToken ct = default);
    Task<IReadOnlyList<ResearchQuery>> GetThreadAsync(int caseId, int attorneyId, CancellationToken ct = default);
    Task<(IReadOnlyList<LegalAuthority> Supports, IReadOnlyList<LegalAuthority> Against)> GuidedResearchAsync(int attorneyId, int caseId, string unsure, string view, string approach, CancellationToken ct = default);
    Task<ResearchDisagreement> RecordDisagreementAsync(int attorneyId, int caseId, string topic, string note, CancellationToken ct = default);
    Task<IReadOnlyList<CaseAuthorityReliance>> BuildMemoAsync(int caseId, int attorneyId, CancellationToken ct = default);
    Task<AttorneyWhereabout> CheckInAsync(int attorneyId, int? eventId, string venue, DateTime expectedReturnUtc, double? latitude = null, double? longitude = null, string? locationOverrideReason = null, CancellationToken ct = default);
    Task CheckOutAsync(int attorneyId, int whereaboutId, CancellationToken ct = default);
    Task RaiseEmergencyAsync(int attorneyId, int whereaboutId, CancellationToken ct = default);
    Task<IReadOnlyList<AttorneyWhereabout>> GetMyTimelineAsync(int attorneyId, CancellationToken ct = default);
    Task EscalateOverdueAsync(CancellationToken ct = default);
    Task RecordSafetyContactAsync(int staffId, int whereaboutId, string outcome, bool attorneyAccountedFor, CancellationToken ct = default);
    Task<IReadOnlyList<ApplicationUser>> UrgentReplacementCandidatesAsync(int calendarEventId, CancellationToken ct = default);
    Task ReallocateUrgentCommitmentAsync(int directorId, int calendarEventId, int replacementAttorneyId, string reason, CancellationToken ct = default);
    Task<BeneficiaryTrustDisbursementRequest> RequestDisbursementAsync(int beneficiaryId, string purpose, decimal amount, string reason, CancellationToken ct = default);
    Task<BeneficiaryTrustDisbursementRequest> DecideDisbursementAsync(int directorId, int requestId, bool approve, string reason, CancellationToken ct = default);
}

public sealed class LegalResearchOptions
{
    public bool ExternalSourcesAvailable { get; set; } = true;
}

public sealed class OperationalUseCaseService(ApplicationDbContext db, INotificationService notifications, IEmailService email,
    IOptions<LegalResearchOptions> researchOptions) : IOperationalUseCaseService
{
    private async Task<List<LegalAuthority>> RankAuthorities(string text, bool limitedToInternal, CancellationToken ct)
    {
        var terms = text.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Where(x => x.Length > 3).Distinct().ToArray();
        var authorities = await db.LegalAuthorities.Where(x => x.IsInternalFallback == limitedToInternal).ToListAsync(ct);
        return authorities.Select(x => new { Item=x, Score=terms.Count(term => $"{x.Citation} {x.Subject} {x.Summary} {x.SearchText}".Contains(term, StringComparison.OrdinalIgnoreCase)) })
            .Where(x => x.Score > 0).OrderByDescending(x => x.Item.Rank == AuthorityRank.Binding).ThenByDescending(x => x.Score).Select(x => x.Item).Take(20).ToList();
    }

    // Legal-issue extraction: a passage highlighted in case notes is rarely itself a clean search query — it's
    // surrounding narrative. This picks out the sentence(s) most likely to state the actual legal issue (those
    // containing an issue-spotting trigger word), falling back to a truncated version of the passage if none
    // of the trigger words appear, rather than searching on the raw passage verbatim.
    private static readonly string[] IssueTriggerWords =
    [
        "whether", "liable", "liability", "breach", "negligent", "negligence", "unlawful", "duty",
        "damages", "capacity", "jurisdiction", "prescription", "reasonable", "unreasonable", "wrongful",
        "entitled", "entitlement", "obligation", "consent", "custody", "dismissal", "unfair", "invalid",
        "void", "enforceable", "misrepresentation", "fraud", "causation"
    ];

    internal static string ExtractIssue(string passage)
    {
        passage = passage.Trim();
        if (passage.Length == 0) return passage;
        var sentences = passage.Split(['.', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var triggerSentences = sentences.Where(s => IssueTriggerWords.Any(w => s.Contains(w, StringComparison.OrdinalIgnoreCase))).ToList();
        var extracted = triggerSentences.Count > 0 ? string.Join(". ", triggerSentences) : passage;
        const int maxLength = 220;
        return extracted.Length > maxLength ? string.Concat(extracted.AsSpan(0, maxLength).ToString().TrimEnd(), "…") : extracted;
    }

    private async Task<Case> RequireActiveMatterAsync(int attorneyId, int caseId, CancellationToken ct) =>
        await db.Cases.SingleOrDefaultAsync(x => x.Id == caseId && x.LawyerId == attorneyId && x.Status == CaseStatus.Active, ct)
            ?? throw new UnauthorizedAccessException("Only the assigned attorney may research an active matter.");

    public async Task<IReadOnlyList<LegalAuthority>> ResearchAsync(int attorneyId, int caseId, string issue, int? caseNoteId = null, CancellationToken ct = default)
    {
        var matter = await RequireActiveMatterAsync(attorneyId, caseId, ct);
        if (string.IsNullOrWhiteSpace(issue)) throw new InvalidOperationException("A legal issue or source passage is required.");
        var passage = issue.Trim();
        var extractedIssue = ExtractIssue(passage);
        var limitedToInternal = !researchOptions.Value.ExternalSourcesAvailable;
        var ranked = await RankAuthorities(extractedIssue, limitedToInternal, ct);
        db.AuditEntries.Add(new() { ActorUserId=attorneyId, EntityType="LegalResearch", EntityId=matter.Id.ToString(), Action=ranked.Count == 0 ? "Research returned no authority" : "Legal authority research performed", SafeMetadataJson=System.Text.Json.JsonSerializer.Serialize(new { issue=extractedIssue, passage, results=ranked.Count, limitedToInternal }) });
        db.ResearchQueries.Add(new() { CaseId=caseId, AttorneyId=attorneyId, CaseNoteId=caseNoteId, SourcePassage=passage, Issue=extractedIssue, ResultCount=ranked.Count, LimitedToInternal=limitedToInternal });
        await db.SaveChangesAsync(ct); return ranked;
    }

    public async Task<IReadOnlyList<ResearchQuery>> GetThreadAsync(int caseId, int attorneyId, CancellationToken ct = default)
    {
        await RequireActiveMatterAsync(attorneyId, caseId, ct);
        return await db.ResearchQueries.Where(x => x.CaseId == caseId).OrderByDescending(x => x.CreatedAtUtc).Take(30).ToListAsync(ct);
    }

    public async Task<(IReadOnlyList<LegalAuthority> Supports, IReadOnlyList<LegalAuthority> Against)> GuidedResearchAsync(int attorneyId, int caseId, string unsure, string view, string approach, CancellationToken ct = default)
    {
        await RequireActiveMatterAsync(attorneyId, caseId, ct);
        if (string.IsNullOrWhiteSpace(unsure) && string.IsNullOrWhiteSpace(view)) throw new InvalidOperationException("Describe what you're unsure about and your view before running guided research.");
        var limitedToInternal = !researchOptions.Value.ExternalSourcesAvailable;
        var ranked = await RankAuthorities($"{unsure} {view} {approach}", limitedToInternal, ct);
        // Best-effort split: an authority still good law is treated as supporting the attorney's position;
        // one with adverse subsequent treatment is surfaced as the challenge to be ready for.
        var supports = ranked.Where(x => x.Treatment == AuthorityTreatment.GoodLaw).ToList();
        var against = ranked.Where(x => x.Treatment != AuthorityTreatment.GoodLaw).ToList();
        db.AuditEntries.Add(new() { ActorUserId=attorneyId, EntityType="LegalResearch", EntityId=caseId.ToString(), Action="Guided research performed", SafeMetadataJson=System.Text.Json.JsonSerializer.Serialize(new { unsure, view, approach, supports=supports.Count, against=against.Count }) });
        db.ResearchQueries.Add(new() { CaseId=caseId, AttorneyId=attorneyId, Issue=$"Guided: {unsure}", ResultCount=ranked.Count, LimitedToInternal=limitedToInternal });
        await db.SaveChangesAsync(ct);
        return (supports, against);
    }

    public async Task<ResearchDisagreement> RecordDisagreementAsync(int attorneyId, int caseId, string topic, string note, CancellationToken ct = default)
    {
        await RequireActiveMatterAsync(attorneyId, caseId, ct);
        if (string.IsNullOrWhiteSpace(note)) throw new InvalidOperationException("Record why your professional judgement differs before saving a disagreement.");
        var disagreement = new ResearchDisagreement { CaseId=caseId, AttorneyId=attorneyId, Topic=(topic ?? "").Trim(), Note=note.Trim() };
        db.ResearchDisagreements.Add(disagreement);
        db.AuditEntries.Add(new() { ActorUserId=attorneyId, EntityType="ResearchDisagreement", EntityId=caseId.ToString(), Action="Professional disagreement recorded", SafeMetadataJson=System.Text.Json.JsonSerializer.Serialize(new { topic }) });
        await db.SaveChangesAsync(ct); return disagreement;
    }

    public async Task<IReadOnlyList<CaseAuthorityReliance>> BuildMemoAsync(int caseId, int attorneyId, CancellationToken ct = default)
    {
        await RequireActiveMatterAsync(attorneyId, caseId, ct);
        return await db.CaseAuthorityReliances.Include(x => x.LegalAuthority).Include(x => x.Attorney)
            .Where(x => x.CaseId == caseId).OrderBy(x => x.RecordedAtUtc).ToListAsync(ct);
    }

    public async Task<CaseAuthorityReliance> RelyAsync(int attorneyId, int caseId, int authorityId, string reason, bool confirmAdverseTreatment, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new InvalidOperationException("Record why the authority is relevant before relying on it.");
        var matter = await RequireActiveMatterAsync(attorneyId, caseId, ct);
        var authority = await db.LegalAuthorities.FindAsync([authorityId], ct) ?? throw new KeyNotFoundException();
        if (authority.Treatment != AuthorityTreatment.GoodLaw && !confirmAdverseTreatment) throw new InvalidOperationException("Express confirmation is required because this authority has adverse subsequent treatment.");
        var reliance = new CaseAuthorityReliance { CaseId=caseId, LegalAuthorityId=authorityId, AttorneyId=attorneyId, RelevanceReason=reason.Trim(), AdverseTreatmentConfirmed=confirmAdverseTreatment };
        db.CaseAuthorityReliances.Add(reliance); db.AuditEntries.Add(new() { ActorUserId=attorneyId, EntityType="CaseAuthorityReliance", EntityId=caseId.ToString(), Action="Authority attached to matter", SafeMetadataJson=System.Text.Json.JsonSerializer.Serialize(new { authority.Citation, authority.Treatment }) });
        await db.SaveChangesAsync(ct); return reliance;
    }

    public const double GeofenceRadiusMeters = 500;

    private static double DistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadius = 6371000;
        double ToRad(double deg) => deg * Math.PI / 180;
        var dLat = ToRad(lat2 - lat1); var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return earthRadius * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    public async Task<AttorneyWhereabout> CheckInAsync(int attorneyId, int? eventId, string venue, DateTime expectedReturnUtc, double? latitude = null, double? longitude = null, string? locationOverrideReason = null, CancellationToken ct = default)
    {
        if (!await db.Users.AnyAsync(x=>x.Id==attorneyId && x.Role==UserRole.Lawyer && x.IsActive,ct)) throw new UnauthorizedAccessException();
        if (string.IsNullOrWhiteSpace(venue) || expectedReturnUtc <= DateTime.UtcNow) throw new InvalidOperationException("A venue and future expected return time are required.");
        if (await db.AttorneyWhereabouts.AnyAsync(x=>x.AttorneyId==attorneyId && x.CheckedOutAtUtc==null,ct)) throw new InvalidOperationException("Check out of the current engagement first.");
        var item=new AttorneyWhereabout { AttorneyId=attorneyId,CalendarEventId=eventId,Venue=venue.Trim(),CheckedInAtUtc=DateTime.UtcNow,ExpectedReturnAtUtc=expectedReturnUtc,Status=WhereaboutStatus.Offsite,Latitude=latitude,Longitude=longitude };
        if (latitude.HasValue && longitude.HasValue)
        {
            var known = await db.KnownVenues.Where(x => venue.Contains(x.Name) || x.Name.Contains(venue)).ToListAsync(ct);
            if (known.Count > 0)
            {
                var distance = known.Min(v => DistanceMeters(latitude.Value, longitude.Value, v.Latitude, v.Longitude));
                item.DistanceFromVenueMeters = distance;
                item.LocationVerified = distance <= GeofenceRadiusMeters;
                if (!item.LocationVerified.Value)
                {
                    if (string.IsNullOrWhiteSpace(locationOverrideReason)) throw new InvalidOperationException($"Your location is {distance:N0}m from {venue} — outside the {GeofenceRadiusMeters:N0}m confirmation radius. State a reason to check in anyway.");
                    item.LocationOverrideReason = locationOverrideReason.Trim();
                }
            }
        }
        db.Add(item); db.AuditEntries.Add(new(){ActorUserId=attorneyId,EntityType="AttorneyWhereabout",EntityId=attorneyId.ToString(),Action=item.LocationVerified==true?"Attorney checked in offsite — location verified":item.LocationVerified==false?"Attorney checked in offsite — location outside geofence, reason recorded":"Attorney checked in offsite"}); await db.SaveChangesAsync(ct); return item;
    }

    public async Task CheckOutAsync(int attorneyId, int whereaboutId, CancellationToken ct = default)
    {
        var item=await db.AttorneyWhereabouts.SingleOrDefaultAsync(x=>x.Id==whereaboutId && x.AttorneyId==attorneyId && x.CheckedOutAtUtc==null,ct) ?? throw new KeyNotFoundException();
        var wasEmergency = item.Status == WhereaboutStatus.Emergency;
        item.CheckedOutAtUtc=DateTime.UtcNow; item.Status=WhereaboutStatus.Returned; db.AuditEntries.Add(new(){ActorUserId=attorneyId,EntityType="AttorneyWhereabout",EntityId=item.Id.ToString(),Action=wasEmergency?"Attorney checked out — emergency status cleared":"Attorney checked out"}); await db.SaveChangesAsync(ct);
    }

    public async Task RaiseEmergencyAsync(int attorneyId, int whereaboutId, CancellationToken ct = default)
    {
        var item = await db.AttorneyWhereabouts.Include(x => x.Attorney).SingleOrDefaultAsync(x => x.Id == whereaboutId && x.AttorneyId == attorneyId && x.CheckedOutAtUtc == null, ct)
            ?? throw new KeyNotFoundException();
        item.Status = WhereaboutStatus.Emergency;
        item.AlertedAtUtc = DateTime.UtcNow;
        db.AuditEntries.Add(new() { ActorUserId = attorneyId, EntityType = nameof(AttorneyWhereabout), EntityId = item.Id.ToString(), Action = "Emergency raised by attorney" });
        var recipients = await db.Users.Where(x => (x.Role == UserRole.Admin || x.Role == UserRole.Director) && x.IsActive).Select(x => x.Id).ToListAsync(ct);
        foreach (var id in recipients)
            await notifications.QueueAsync(id, "AttorneyEmergency", "EMERGENCY: attorney requires assistance",
                $"{item.Attorney.FullName} raised an emergency alert at {item.Venue}.", "/Operational/Whereabouts", $"whereabout-emergency:{item.Id}:{DateTime.UtcNow.Ticks}", ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<AttorneyWhereabout>> GetMyTimelineAsync(int attorneyId, CancellationToken ct = default) =>
        await db.AttorneyWhereabouts.Where(x => x.AttorneyId == attorneyId).OrderByDescending(x => x.CheckedInAtUtc).Take(30).ToListAsync(ct);

    public async Task EscalateOverdueAsync(CancellationToken ct = default)
    {
        var now=DateTime.UtcNow; var directors=await db.Users.Where(x=>(x.Role==UserRole.Director || x.Role==UserRole.Admin) && x.IsActive).Select(x=>x.Id).ToListAsync(ct);
        foreach(var item in await db.AttorneyWhereabouts.Include(x=>x.Attorney).Where(x=>x.CheckedOutAtUtc==null && x.ExpectedReturnAtUtc<now && x.Status!=WhereaboutStatus.Emergency).ToListAsync(ct))
        {
            if(item.AlertedAtUtc==null){item.AlertedAtUtc=now;item.Status=WhereaboutStatus.AlertRaised;foreach(var id in directors)await notifications.QueueAsync(id,"AttorneySafety","Attorney overdue from off-site engagement",$"{item.Attorney.FullName} is overdue from {item.Venue}.","/Operational/Whereabouts",$"whereabout-alert:{item.Id}:{id}",ct);}
            else if(item.DirectorEscalatedAtUtc==null && item.AlertedAtUtc<=now.AddMinutes(-30)){item.DirectorEscalatedAtUtc=now;item.Status=WhereaboutStatus.DirectorEscalated;foreach(var id in directors)await notifications.QueueAsync(id,"AttorneySafetyEscalation","Director escalation: attorney unaccounted for",$"{item.Attorney.FullName} remains unaccounted for.","/Operational/Whereabouts",$"whereabout-director:{item.Id}:{id}",ct);}
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task RecordSafetyContactAsync(int staffId, int whereaboutId, string outcome, bool attorneyAccountedFor, CancellationToken ct = default)
    {
        if (!await db.Users.AnyAsync(x => x.Id == staffId && (x.Role == UserRole.Admin || x.Role == UserRole.Director) && x.IsActive, ct))
            throw new UnauthorizedAccessException("Only an Administrator or Director may record a safety contact attempt.");
        if (string.IsNullOrWhiteSpace(outcome)) throw new InvalidOperationException("Record the outcome of the contact attempt.");
        var item = await db.AttorneyWhereabouts.SingleOrDefaultAsync(x => x.Id == whereaboutId && x.CheckedOutAtUtc == null, ct)
            ?? throw new InvalidOperationException("This off-site engagement is already closed or unavailable.");
        item.ContactOutcome = $"{DateTime.UtcNow:u} — {outcome.Trim()}";
        if (attorneyAccountedFor) item.Status = WhereaboutStatus.AccountedFor;
        db.AuditEntries.Add(new() { ActorUserId = staffId, EntityType = nameof(AttorneyWhereabout), EntityId = item.Id.ToString(), Action = attorneyAccountedFor ? "Attorney accounted for" : "Safety contact attempt recorded" });
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ApplicationUser>> UrgentReplacementCandidatesAsync(int calendarEventId, CancellationToken ct = default)
    {
        var courtEvent = await db.CalendarEvents.Include(x => x.Case).SingleOrDefaultAsync(x => x.Id == calendarEventId, ct)
            ?? throw new InvalidOperationException("Court commitment not found.");
        if (courtEvent.Case?.LawyerId is not int unavailableId || courtEvent.Type is not (EventType.CourtAppearance or EventType.Hearing)) return [];
        var specialtyIds = await db.LawyerProfiles.Where(x => x.UserId == unavailableId).SelectMany(x => x.Specializations.Select(s => s.Id)).ToListAsync(ct);
        var unavailable = await db.AttorneyWhereabouts.Where(x => x.CheckedOutAtUtc == null).Select(x => x.AttorneyId).ToListAsync(ct);
        var candidates = db.Users.Where(x => x.Role == UserRole.Lawyer && x.IsActive && x.Id != unavailableId && !unavailable.Contains(x.Id));
        if (specialtyIds.Count > 0)
            candidates = candidates.Where(x => db.LawyerProfiles.Any(p => p.UserId == x.Id && p.Specializations.Any(s => specialtyIds.Contains(s.Id))));
        return await candidates.OrderBy(x => x.FullName).ToListAsync(ct);
    }

    public async Task ReallocateUrgentCommitmentAsync(int directorId, int calendarEventId, int replacementAttorneyId, string reason, CancellationToken ct = default)
    {
        if (!await db.Users.AnyAsync(x => x.Id == directorId && (x.Role == UserRole.Admin || x.Role == UserRole.Director) && x.IsActive, ct)) throw new UnauthorizedAccessException();
        if (string.IsNullOrWhiteSpace(reason)) throw new InvalidOperationException("Record the reason for urgent reallocation.");
        var courtEvent = await db.CalendarEvents.Include(x => x.Case).SingleOrDefaultAsync(x => x.Id == calendarEventId, ct) ?? throw new InvalidOperationException("Court commitment not found.");
        if (courtEvent.Type is not (EventType.CourtAppearance or EventType.Hearing) || courtEvent.StartDateTime < DateTime.UtcNow || courtEvent.StartDateTime > DateTime.UtcNow.AddHours(24)) throw new InvalidOperationException("Only a court commitment within the next 24 hours can be reallocated here.");
        if (!(await UrgentReplacementCandidatesAsync(calendarEventId, ct)).Any(x => x.Id == replacementAttorneyId)) throw new InvalidOperationException("Select an available attorney with matching specialisation.");
        courtEvent.AssignedToUserId = replacementAttorneyId;
        db.AuditEntries.Add(new() { ActorUserId = directorId, EntityType = nameof(CalendarEvent), EntityId = courtEvent.Id.ToString(), Action = "Urgent court commitment reallocated", SafeMetadataJson = System.Text.Json.JsonSerializer.Serialize(new { replacementAttorneyId, reason = reason.Trim() }) });
        await db.SaveChangesAsync(ct);
    }

    public async Task<BeneficiaryTrustDisbursementRequest> RequestDisbursementAsync(int beneficiaryId, string purpose, decimal amount, string reason, CancellationToken ct = default)
    {
        var beneficiary=await db.Beneficiaries.SingleOrDefaultAsync(x=>x.Id==beneficiaryId && x.Status==BeneficiaryStatus.Approved && x.PortalAccessEnabled,ct) ?? throw new UnauthorizedAccessException("An approved, verified beneficiary account is required.");
        var trust=await db.TrustAccounts.SingleOrDefaultAsync(x=>x.ClientId==beneficiary.BenefactorClientId,ct) ?? throw new InvalidOperationException("No active trust fund is recorded.");
        if(trust.IsFrozen || trust.IsClosed) throw new InvalidOperationException(trust.IsFrozen?"The trust fund is frozen.":"The trust fund is closed.");
        if(string.IsNullOrWhiteSpace(purpose)||!beneficiary.PermittedAssetPurposes.Contains(purpose,StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("The requested purpose is not permitted by the recorded entitlement.");
        if(beneficiary.EntitlementAmountLimit is null or <= 0) throw new InvalidOperationException("No cash entitlement limit is recorded for this beneficiary.");
        var priorCommitted = await db.BeneficiaryTrustDisbursementRequests.Where(x => x.BeneficiaryId == beneficiaryId && x.Status != TrustDisbursementStatus.Rejected).SumAsync(x => (decimal?)x.Amount, ct) ?? 0;
        var entitlementRemaining = Math.Max(0, beneficiary.EntitlementAmountLimit.Value - priorCommitted);
        var availableToBeneficiary = Math.Min(trust.Balance, entitlementRemaining);
        if(amount<=0 || amount>availableToBeneficiary) throw new InvalidOperationException($"The amount exceeds your available beneficiary entitlement of R {availableToBeneficiary:N2}.");
        if(string.IsNullOrWhiteSpace(reason)) throw new InvalidOperationException("A reason is required.");
        var required=await db.BeneficiaryRequirementAssignments.Where(x=>x.BeneficiaryId==beneficiaryId&&x.IsRequired).Select(x=>x.RequirementId).ToListAsync(ct);
        var supplied=await db.BeneficiaryDocuments.Where(x=>x.BeneficiaryId==beneficiaryId&&x.PreScreenStatus==DocumentPreScreenStatus.Passed).Select(x=>x.RequirementId).Distinct().ToListAsync(ct);
        if(required.Except(supplied).Any()) throw new InvalidOperationException("Required supporting documents are missing.");
        var face=await db.FacialVerificationSessions.Where(x=>x.BeneficiaryId==beneficiaryId).OrderByDescending(x=>x.CreatedAtUtc).FirstOrDefaultAsync(ct);
        var request=new BeneficiaryTrustDisbursementRequest{ReferenceNumber=$"TD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",BeneficiaryId=beneficiaryId,TrustAccountId=trust.Id,Purpose=purpose.Trim(),Reason=reason.Trim(),Amount=amount,BalanceSnapshot=availableToBeneficiary,EntitlementLimitSnapshot=beneficiary.EntitlementAmountLimit.Value,Status=face?.Status==FacialVerificationStatus.Verified?TrustDisbursementStatus.Submitted:TrustDisbursementStatus.ManualIdentityReview,SubmittedAtUtc=DateTime.UtcNow};
        db.Add(request);foreach(var id in await db.Users.Where(x=>x.Role==UserRole.Director&&x.IsActive).Select(x=>x.Id).ToListAsync(ct))await notifications.QueueAsync(id,"TrustDisbursement","Beneficiary trust request submitted",$"{request.ReferenceNumber} requests R {amount:N2}.","/Operational/TrustRequests",$"trust-request:{request.ReferenceNumber}:{id}",ct);
        db.AuditEntries.Add(new(){EntityType="BeneficiaryTrustDisbursementRequest",EntityId=request.ReferenceNumber,Action="Trust disbursement requested"});await db.SaveChangesAsync(ct);return request;
    }

    public async Task<BeneficiaryTrustDisbursementRequest> DecideDisbursementAsync(int directorId, int requestId, bool approve, string reason, CancellationToken ct = default)
    {
        if (!await db.Users.AnyAsync(x => x.Id == directorId && x.Role == UserRole.Director && x.IsActive, ct)) throw new UnauthorizedAccessException("Only the Director may decide trust requests.");
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 10) throw new ArgumentException("A clear decision reason is required.");
        var request = await db.BeneficiaryTrustDisbursementRequests.Include(x => x.Beneficiary).SingleOrDefaultAsync(x => x.Id == requestId, ct) ?? throw new KeyNotFoundException("Trust request not found.");
        if (request.Status is not (TrustDisbursementStatus.Submitted or TrustDisbursementStatus.ManualIdentityReview)) throw new InvalidOperationException("This request has already been decided.");
        if (approve && request.Status == TrustDisbursementStatus.ManualIdentityReview) throw new InvalidOperationException("Manual identity review must be completed before approval.");
        request.Status = approve ? TrustDisbursementStatus.Approved : TrustDisbursementStatus.Rejected;
        request.DecidedByUserId = directorId; request.DecisionReason = reason.Trim(); request.DecidedAtUtc = DateTime.UtcNow;
        db.AuditEntries.Add(new(){ActorUserId=directorId,EntityType="BeneficiaryTrustDisbursementRequest",EntityId=request.ReferenceNumber,Action=approve?"Trust disbursement approved":"Trust disbursement rejected",SafeMetadataJson=System.Text.Json.JsonSerializer.Serialize(new { request.Amount, request.DecisionReason })});
        await email.QueueAsync(request.Beneficiary.Email, $"Trust request {request.ReferenceNumber}: {request.Status}", $"<h2>Trust request decision</h2><p>Your request <strong>{request.ReferenceNumber}</strong> has been <strong>{request.Status}</strong>.</p><p>{System.Net.WebUtility.HtmlEncode(request.DecisionReason)}</p>", $"Your trust request {request.ReferenceNumber} is {request.Status}. Reason: {request.DecisionReason}", $"trust-request-decision:{request.Id}", ct);
        await db.SaveChangesAsync(ct); return request;
    }
}

// Runs independently of any page being loaded — the SRS requires overdue detection to work
// "not dependent on the web application being active", not only when an Admin/Director happens
// to open the Whereabouts dashboard.
public sealed class AttorneySafetyWorker(IServiceScopeFactory scopes, ILogger<AttorneySafetyWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { using var scope = scopes.CreateScope(); await scope.ServiceProvider.GetRequiredService<IOperationalUseCaseService>().EscalateOverdueAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex) { logger.LogError(ex, "Attorney safety escalation cycle failed."); }
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
