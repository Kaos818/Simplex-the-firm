using Microsoft.EntityFrameworkCore;
using SimplexLawFirm.Models;
using SimplexLawFirm.Models.Beneficiaries;

namespace SimplexLawFirm.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public override int SaveChanges()
        {
            GuardForecastSnapshots();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            GuardForecastSnapshots();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void GuardForecastSnapshots()
        {
            var immutable = new[] { nameof(CaseForecast.Probability), nameof(CaseForecast.ProbabilityBand), nameof(CaseForecast.ConfidenceLevel), nameof(CaseForecast.ComparableCount), nameof(CaseForecast.FactorsJson), nameof(CaseForecast.ComparableCasesJson), nameof(CaseForecast.AttorneyId), nameof(CaseForecast.CaseId), nameof(CaseForecast.AttorneyAssessment), nameof(CaseForecast.AttorneyAgrees), nameof(CaseForecast.AttorneyNotes), nameof(CaseForecast.LockedAtUtc) };
            foreach (var entry in ChangeTracker.Entries<CaseForecast>().Where(x => x.State == EntityState.Modified))
            {
                var originalStatus = entry.Property(x => x.Status).OriginalValue;
                if (originalStatus is not (ForecastStatus.Locked or ForecastStatus.Scored)) continue;
                if (immutable.Any(name => entry.Property(name).IsModified))
                    throw new InvalidOperationException("A locked forecast snapshot is immutable.");
            }
            var estimateFields = new[] { nameof(MatterCostEstimate.CostEstimateEnquiryId), nameof(MatterCostEstimate.Version), nameof(MatterCostEstimate.ComparableMatterCount), nameof(MatterCostEstimate.ProfessionalFeesLow), nameof(MatterCostEstimate.ProfessionalFeesHigh), nameof(MatterCostEstimate.DisbursementsLow), nameof(MatterCostEstimate.DisbursementsHigh), nameof(MatterCostEstimate.VatLow), nameof(MatterCostEstimate.VatHigh), nameof(MatterCostEstimate.TotalLow), nameof(MatterCostEstimate.TotalHigh), nameof(MatterCostEstimate.PermittedVarianceTolerance), nameof(MatterCostEstimate.RatesSnapshotJson), nameof(MatterCostEstimate.AssumptionsSnapshotJson), nameof(MatterCostEstimate.ComparableMattersSnapshotJson), nameof(MatterCostEstimate.DeclineReason), nameof(MatterCostEstimate.LockedAtUtc) };
            foreach (var entry in ChangeTracker.Entries<MatterCostEstimate>().Where(x => x.State == EntityState.Modified && x.Property(y => y.Status).OriginalValue is CostEstimateStatus.Locked or CostEstimateStatus.Linked))
                if (estimateFields.Any(name => entry.Property(name).IsModified))
                    throw new InvalidOperationException("A locked matter cost estimate is immutable.");
            var reimbursementFields = new[] {
                nameof(ReimbursementClaim.ClaimNumber), nameof(ReimbursementClaim.CaseId), nameof(ReimbursementClaim.AttorneyId),
                nameof(ReimbursementClaim.ExpenseType), nameof(ReimbursementClaim.ExpenseDate), nameof(ReimbursementClaim.Amount),
                nameof(ReimbursementClaim.Description), nameof(ReimbursementClaim.MatchedActivityType), nameof(ReimbursementClaim.MatchedActivityId),
                nameof(ReimbursementClaim.ValidationFailureReason), nameof(ReimbursementClaim.ProofOriginalFileName),
                nameof(ReimbursementClaim.ProofRelativePath), nameof(ReimbursementClaim.ProofContentType),
                nameof(ReimbursementClaim.ProofSizeBytes), nameof(ReimbursementClaim.ProofSha256Hash),
                nameof(ReimbursementClaim.PolicyLimitSnapshot), nameof(ReimbursementClaim.DelegatedLimitSnapshot),
                nameof(ReimbursementClaim.ExceedsPolicyLimit), nameof(ReimbursementClaim.Classification),
                nameof(ReimbursementClaim.ClassificationReason), nameof(ReimbursementClaim.SubmittedAtUtc)
            };
            foreach (var entry in ChangeTracker.Entries<ReimbursementClaim>()
                .Where(x => x.State == EntityState.Modified
                    && x.Property(y => y.Status).OriginalValue != ReimbursementStatus.DraftProofRequired))
                if (reimbursementFields.Any(name => entry.Property(name).IsModified))
                    throw new InvalidOperationException("Submitted reimbursement evidence and policy snapshots are immutable.");
            if (ChangeTracker.Entries<ReimbursementAuditEntry>().Any(x => x.State is EntityState.Modified or EntityState.Deleted))
                throw new InvalidOperationException("Reimbursement audit entries are append-only.");
            if (ChangeTracker.Entries<AuditEntry>().Any(x => x.State is EntityState.Modified or EntityState.Deleted))
                throw new InvalidOperationException("System audit entries are append-only.");
            if (ChangeTracker.Entries<LegalCostRecoveryAuditEntry>().Any(x => x.State is EntityState.Modified or EntityState.Deleted))
                throw new InvalidOperationException("Legal cost recovery audit entries are append-only.");
            foreach (var entry in ChangeTracker.Entries<LegalCostRecoveryClaim>().Where(x => x.State == EntityState.Modified && x.Property(y => y.Status).OriginalValue != LegalCostRecoveryStatus.PendingDirectorApproval))
                if (new[] { nameof(LegalCostRecoveryClaim.CaseId), nameof(LegalCostRecoveryClaim.AttorneyId), nameof(LegalCostRecoveryClaim.Ground), nameof(LegalCostRecoveryClaim.Justification), nameof(LegalCostRecoveryClaim.ClaimedAmount) }.Any(name => entry.Property(name).IsModified))
                    throw new InvalidOperationException("Decided legal cost recovery evidence is immutable.");
        }

        // User Management
        public DbSet<ApplicationUser> Users { get; set; }

        // Client & Case Management
        public DbSet<Client> Clients { get; set; }
        public DbSet<Case> Cases { get; set; }
        public DbSet<CaseNote> CaseNotes { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentShare> DocumentShares { get; set; }

        // Retainer Management
        public DbSet<Retainer> Retainers { get; set; }
        public DbSet<RetainerTemplate> RetainerTemplates { get; set; }
        public DbSet<ClientRequest> ClientRequests { get; set; }
        public DbSet<RetainerPaymentSchedule> RetainerPaymentSchedules { get; set; }
        public DbSet<RetainerPayment> RetainerPayments { get; set; }
        public DbSet<RetainerActionLog> RetainerActionLogs { get; set; }
        public DbSet<RetainerRenewal> RetainerRenewals { get; set; }

        // Billing & Accounting
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<TimeEntry> TimeEntries { get; set; }
        public DbSet<TrustAccount> TrustAccounts { get; set; }
        public DbSet<TrustTransaction> TrustTransactions { get; set; }

        // Calendar & Tasks
        public DbSet<CalendarEvent> CalendarEvents { get; set; }
        public DbSet<EventAttendee> EventAttendees { get; set; }
        public DbSet<EventReminder> EventReminders { get; set; }
        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<TaskComment> TaskComments { get; set; }
        public DbSet<TaskAttachment> TaskAttachments { get; set; }

        // Lawyer Specialization
        public DbSet<LawyerProfile> LawyerProfiles { get; set; }
        public DbSet<LawyerSpecialization> LawyerSpecializations { get; set; }
        public DbSet<Beneficiary> Beneficiaries { get; set; }
        public DbSet<BeneficiaryInvitation> BeneficiaryInvitations { get; set; }
        public DbSet<BeneficiaryDocumentRequirement> BeneficiaryDocumentRequirements { get; set; }
        public DbSet<BeneficiaryRequirementAssignment> BeneficiaryRequirementAssignments { get; set; }
        public DbSet<BeneficiaryDocument> BeneficiaryDocuments { get; set; }
        public DbSet<FacialVerificationSession> FacialVerificationSessions { get; set; }
        public DbSet<BiometricConsentRecord> BiometricConsentRecords { get; set; }
        public DbSet<SystemNotification> SystemNotifications { get; set; }
        public DbSet<EmailOutboxMessage> EmailOutboxMessages { get; set; }
        public DbSet<AppointmentInvitation> AppointmentInvitations { get; set; }
        public DbSet<AppointmentBillingRecord> AppointmentBillingRecords { get; set; }
        public DbSet<InvoicePenalty> InvoicePenalties { get; set; }
        public DbSet<AuditEntry> AuditEntries { get; set; }
        public DbSet<ExternalEvidenceRequest> ExternalEvidenceRequests { get; set; }
        public DbSet<ExternalEvidenceDocument> ExternalEvidenceDocuments { get; set; }
        public DbSet<CaseForecast> CaseForecasts { get; set; }
        public DbSet<CaseHandover> CaseHandovers { get; set; }
        public DbSet<HandoverItem> HandoverItems { get; set; }
        public DbSet<HandoverQuery> HandoverQueries { get; set; }
        public DbSet<ServiceComplaint> ServiceComplaints { get; set; }
        public DbSet<ComplaintAttachment> ComplaintAttachments { get; set; }
        public DbSet<ComplaintAppointment> ComplaintAppointments { get; set; }
        public DbSet<CaseReassignment> CaseReassignments { get; set; }
        public DbSet<ForecastCalibration> ForecastCalibrations { get; set; }
        public DbSet<ClientCorrespondence> ClientCorrespondence { get; set; }
        public DbSet<ClientForecastRequest> ClientForecastRequests { get; set; }
        public DbSet<StaffServiceRecordEntry> StaffServiceRecordEntries { get; set; }
        public DbSet<CostEstimateEnquiry> CostEstimateEnquiries { get; set; }
        public DbSet<MatterCostEstimate> MatterCostEstimates { get; set; }
        public DbSet<CostEstimateCoverageGap> CostEstimateCoverageGaps { get; set; }
        public DbSet<InvoiceEstimateAuthorisation> InvoiceEstimateAuthorisations { get; set; }
        public DbSet<ExpensePolicy> ExpensePolicies { get; set; }
        public DbSet<MatterExpenseTerm> MatterExpenseTerms { get; set; }
        public DbSet<ReimbursementClaim> ReimbursementClaims { get; set; }
        public DbSet<AttorneyReimbursementPayable> AttorneyReimbursementPayables { get; set; }
        public DbSet<MatterDisbursement> MatterDisbursements { get; set; }
        public DbSet<ReimbursementAuditEntry> ReimbursementAuditEntries { get; set; }
        public DbSet<LegalSubject> LegalSubjects { get; set; }
        public DbSet<KnowledgeArticle> KnowledgeArticles { get; set; }
        public DbSet<PrecedentIndexJob> PrecedentIndexJobs { get; set; }
        public DbSet<PrecedentItem> PrecedentItems { get; set; }
        public DbSet<PrecedentPassage> PrecedentPassages { get; set; }
        public DbSet<PrecedentConflictFlag> PrecedentConflictFlags { get; set; }
        public DbSet<CoverageCommission> CoverageCommissions { get; set; }
        public DbSet<VulnerableClientFlag> VulnerableClientFlags { get; set; }
        public DbSet<VulnerableFlagAcknowledgement> VulnerableFlagAcknowledgements { get; set; }
        public DbSet<AppointmentInterpreterAssignment> AppointmentInterpreterAssignments { get; set; }
        public DbSet<AppointmentSupportPersonAssignment> AppointmentSupportPersonAssignments { get; set; }
        public DbSet<ClientSupportSession> ClientSupportSessions { get; set; }
        public DbSet<LegalCostRecoveryClaim> LegalCostRecoveryClaims { get; set; }
        public DbSet<LegalCostRecoveryTimeEntry> LegalCostRecoveryTimeEntries { get; set; }
        public DbSet<LegalCostRecoveryAuditEntry> LegalCostRecoveryAuditEntries { get; set; }
        public DbSet<LitigationStrategyDecision> LitigationStrategyDecisions { get; set; }
        public DbSet<CaseDocumentRequirement> CaseDocumentRequirements { get; set; }
        public DbSet<CaseDocumentWaiver> CaseDocumentWaivers { get; set; }
        public DbSet<CaseReadinessReview> CaseReadinessReviews { get; set; }
        public DbSet<LegalAuthority> LegalAuthorities { get; set; }
        public DbSet<CaseAuthorityReliance> CaseAuthorityReliances { get; set; }
        public DbSet<ResearchQuery> ResearchQueries { get; set; }
        public DbSet<ResearchDisagreement> ResearchDisagreements { get; set; }
        public DbSet<AttorneyWhereabout> AttorneyWhereabouts { get; set; }
        public DbSet<BeneficiaryTrustDisbursementRequest> BeneficiaryTrustDisbursementRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<CaseAuthorityReliance>().HasIndex(x => new { x.CaseId, x.LegalAuthorityId, x.AttorneyId });
            modelBuilder.Entity<ResearchQuery>(e => {
                e.HasOne(x => x.Case).WithMany().HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Attorney).WithMany().HasForeignKey(x => x.AttorneyId).OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(x => new { x.CaseId, x.CreatedAtUtc });
            });
            modelBuilder.Entity<ResearchDisagreement>(e => {
                e.HasOne(x => x.Case).WithMany().HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Attorney).WithMany().HasForeignKey(x => x.AttorneyId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<AttorneyWhereabout>().HasIndex(x => new { x.Status, x.ExpectedReturnAtUtc });
            modelBuilder.Entity<BeneficiaryTrustDisbursementRequest>().HasIndex(x => x.ReferenceNumber).IsUnique();
            modelBuilder.Entity<BeneficiaryTrustDisbursementRequest>().Property(x => x.Amount).HasPrecision(18, 2);
            modelBuilder.Entity<BeneficiaryTrustDisbursementRequest>().Property(x => x.EntitlementLimitSnapshot).HasPrecision(18, 2);
            modelBuilder.Entity<BeneficiaryTrustDisbursementRequest>().Property(x => x.BalanceSnapshot).HasPrecision(18, 2);
            modelBuilder.Entity<LegalSubject>(e => {
                e.HasIndex(x => x.Name).IsUnique();
                e.HasData(
                    Subject(1, "Civil Litigation", "litigation,court,trial,appeal,interdict,damages"),
                    Subject(2, "Commercial Law", "commercial,contract,company,business,shareholder"),
                    Subject(3, "Family Law", "family,divorce,custody,maintenance,matrimonial"),
                    Subject(4, "Labour Law", "labour,employment,employee,ccma,dismissal"),
                    Subject(5, "Criminal Law", "criminal,bail,prosecution,sentence,accused"),
                    Subject(6, "Property Law", "property,transfer,lease,eviction,conveyancing"),
                    Subject(7, "Estates and Trusts", "estate,will,trust,beneficiary,executor"),
                    Subject(8, "Personal Injury", "injury,accident,raf,medical negligence,compensation"),
                    Subject(9, "General Practice", "general,advice,procedure"));
            });
            modelBuilder.Entity<VulnerableClientFlag>(e => {
                e.HasIndex(x => new { x.ClientId, x.Status });
                e.HasIndex(x => new { x.ClientId, x.Safeguard }).IsUnique().HasFilter("[Status] <> 3");
                e.HasIndex(x => new { x.Status, x.ReviewDueAtUtc });
                e.HasIndex(x => new { x.Status, x.NextReviewAtUtc });
                e.HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.RaisedByAttorney).WithMany().HasForeignKey(x => x.RaisedByAttorneyId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.ReviewedByDirector).WithMany().HasForeignKey(x => x.ReviewedByDirectorId).OnDelete(DeleteBehavior.Restrict);
                if (Database.ProviderName?.Contains("Sqlite") == true) e.Property(x => x.RowVersion).IsConcurrencyToken().ValueGeneratedNever();
                else e.Property(x => x.RowVersion).IsRowVersion();
            });
            modelBuilder.Entity<VulnerableFlagAcknowledgement>(e => {
                e.HasIndex(x => new { x.CaseId, x.StaffUserId, x.VulnerableClientFlagId });
                e.HasOne(x => x.Flag).WithMany().HasForeignKey(x => x.VulnerableClientFlagId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne<Case>().WithMany().HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.StaffUserId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<AppointmentInterpreterAssignment>(e => {
                e.HasIndex(x => x.CalendarEventId).IsUnique();
                e.HasOne(x => x.CalendarEvent).WithMany().HasForeignKey(x => x.CalendarEventId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.AssignedByUserId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<AppointmentSupportPersonAssignment>(e => {
                e.HasIndex(x => x.CalendarEventId).IsUnique();
                e.HasOne(x => x.CalendarEvent).WithMany().HasForeignKey(x => x.CalendarEventId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<ClientSupportSession>(e => {
                e.HasIndex(x => new { x.ClientId, x.ExpiresAtUtc });
                e.HasOne<Client>().WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.AuthorisedByStaffUserId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<PrecedentIndexJob>(e => {
                e.HasIndex(x => new { x.SourceType, x.SourceId, x.ContentHash }).IsUnique();
                e.HasIndex(x => new { x.Status, x.NextAttemptAtUtc });
            });
            modelBuilder.Entity<PrecedentItem>(e => {
                e.HasIndex(x => new { x.SourceType, x.SourceId, x.ContentHash }).IsUnique();
                e.HasIndex(x => new { x.LegalSubjectId, x.IsCurrent });
                e.HasOne(x => x.LegalSubject).WithMany().HasForeignKey(x => x.LegalSubjectId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<PrecedentPassage>(e => {
                e.HasIndex(x => new { x.PrecedentItemId, x.PassageNumber }).IsUnique();
                e.HasOne(x => x.PrecedentItem).WithMany().HasForeignKey(x => x.PrecedentItemId).OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<PrecedentConflictFlag>(e => {
                e.HasIndex(x => new { x.ExistingPrecedentItemId, x.NewPrecedentItemId }).IsUnique();
                e.Property(x => x.Similarity).HasPrecision(6, 5);
                e.HasOne(x => x.NewPrecedentItem).WithMany().HasForeignKey(x => x.NewPrecedentItemId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.ExistingPrecedentItem).WithMany().HasForeignKey(x => x.ExistingPrecedentItemId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<CoverageCommission>(e => {
                e.HasIndex(x => new { x.LegalSubjectId, x.Status });
                e.HasOne(x => x.LegalSubject).WithMany().HasForeignKey(x => x.LegalSubjectId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<CaseForecast>(e => {
                e.HasIndex(x => x.CaseId);
                e.Property(x => x.Probability).HasPrecision(6,5);
                e.Property(x => x.AttorneyAssessment).HasPrecision(6,5);
                e.Property(x => x.AccuracyScore).HasPrecision(6,5);
                e.HasOne(x => x.Case).WithMany().HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Attorney).WithMany().HasForeignKey(x => x.AttorneyId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<ForecastCalibration>(e => {
                e.HasIndex(x => x.AttorneyId).IsUnique();
                e.Property(x => x.MeanAccuracy).HasPrecision(6,5);
                e.Property(x => x.MeanBias).HasPrecision(6,5);
                e.HasOne(x => x.Attorney).WithMany().HasForeignKey(x => x.AttorneyId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<ClientForecastRequest>(e => {
                e.HasIndex(x => new { x.CaseId, x.Status });
                e.HasOne(x => x.Case).WithMany().HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.FulfilledByForecast).WithMany().HasForeignKey(x => x.FulfilledByForecastId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<CaseReassignment>(e => {
                e.HasIndex(x => new { x.CaseId, x.Status });
                e.HasOne(x => x.Case).WithMany().HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.OutgoingAttorney).WithMany().HasForeignKey(x => x.OutgoingAttorneyId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.ReceivingAttorney).WithMany().HasForeignKey(x => x.ReceivingAttorneyId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.ApprovedByUser).WithMany().HasForeignKey(x => x.ApprovedByUserId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<CaseHandover>(e => {
                e.HasIndex(x => new { x.CaseId, x.Status });
                e.HasOne(x => x.Case).WithMany().HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.OutgoingAttorney).WithMany().HasForeignKey(x => x.OutgoingAttorneyId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.ReceivingAttorney).WithMany().HasForeignKey(x => x.ReceivingAttorneyId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.CaseReassignment).WithMany().HasForeignKey(x => x.CaseReassignmentId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.DirectorReviewedByUser).WithMany().HasForeignKey(x => x.DirectorReviewedByUserId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<HandoverItem>().HasOne(x => x.CaseHandover).WithMany(x => x.Items).HasForeignKey(x => x.CaseHandoverId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<HandoverQuery>(e => {
                e.HasOne(x => x.CaseHandover).WithMany(x => x.Queries).HasForeignKey(x => x.CaseHandoverId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.RaisedByUser).WithMany().HasForeignKey(x => x.RaisedByUserId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<ServiceComplaint>(e => {
                e.HasIndex(x => x.ReferenceNumber).IsUnique();
                e.HasIndex(x => new { x.Status, x.ResponseDueAtUtc });
                e.HasOne(x => x.Case).WithMany().HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.RoutedToUser).WithMany().HasForeignKey(x => x.RoutedToUserId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.ResolvedByUser).WithMany().HasForeignKey(x => x.ResolvedByUserId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<ComplaintAppointment>(e => {
                e.HasOne(x => x.ServiceComplaint).WithMany(x => x.Appointments).HasForeignKey(x => x.ServiceComplaintId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.BookedByUser).WithMany().HasForeignKey(x => x.BookedByUserId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<ComplaintAttachment>(e => {
                e.HasOne(x => x.ServiceComplaint).WithMany(x => x.Attachments).HasForeignKey(x => x.ServiceComplaintId).OnDelete(DeleteBehavior.Cascade);
                e.Property(x => x.Sha256Hash).HasMaxLength(64);
            });
            modelBuilder.Entity<ClientCorrespondence>(e => {
                e.HasIndex(x => new { x.CaseId, x.AnsweredAtUtc });
                e.HasOne(x => x.Case).WithMany().HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<StaffServiceRecordEntry>(e => {
                e.HasIndex(x => new { x.StaffUserId, x.RecordedAtUtc });
                e.HasIndex(x => new { x.ServiceComplaintId, x.StaffUserId }).IsUnique();
                e.HasOne(x => x.StaffUser).WithMany().HasForeignKey(x => x.StaffUserId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.ServiceComplaint).WithMany().HasForeignKey(x => x.ServiceComplaintId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<CostEstimateEnquiry>(e => {
                e.HasIndex(x => x.PublicToken).IsUnique();
                e.HasIndex(x => new { x.Email, x.CreatedAtUtc });
                e.Property(x => x.MatterValue).HasPrecision(18, 2);
                e.HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<MatterCostEstimate>(e => {
                e.HasIndex(x => new { x.CostEstimateEnquiryId, x.Version }).IsUnique();
                e.HasIndex(x => x.LinkedCaseId);
                foreach (var name in new[] { nameof(MatterCostEstimate.ProfessionalFeesLow), nameof(MatterCostEstimate.ProfessionalFeesHigh), nameof(MatterCostEstimate.DisbursementsLow), nameof(MatterCostEstimate.DisbursementsHigh), nameof(MatterCostEstimate.VatLow), nameof(MatterCostEstimate.VatHigh), nameof(MatterCostEstimate.TotalLow), nameof(MatterCostEstimate.TotalHigh) }) e.Property(name).HasPrecision(18,2);
                e.Property(x => x.PermittedVarianceTolerance).HasPrecision(6,5);
                e.HasOne(x => x.Enquiry).WithMany(x => x.Estimates).HasForeignKey(x => x.CostEstimateEnquiryId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.LinkedCase).WithMany().HasForeignKey(x => x.LinkedCaseId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<CostEstimateCoverageGap>(e => e.HasIndex(x => new { x.Status, x.RecordedAtUtc }));
            modelBuilder.Entity<InvoiceEstimateAuthorisation>(e => {
                e.HasIndex(x => x.InvoiceId).IsUnique();
                e.Property(x => x.InvoiceTotal).HasPrecision(18,2);
                e.Property(x => x.VariancePercent).HasPrecision(8,5);
                e.HasOne(x => x.Invoice).WithMany(x => x.EstimateAuthorisations).HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.MatterCostEstimate).WithMany().HasForeignKey(x => x.MatterCostEstimateId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.ApprovedByUser).WithMany().HasForeignKey(x => x.ApprovedByUserId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<ExpensePolicy>(e => {
                e.HasIndex(x => x.ExpenseType).IsUnique();
                e.Property(x => x.PerItemLimit).HasPrecision(18, 2);
                e.Property(x => x.DelegatedApprovalLimit).HasPrecision(18, 2);
                e.HasData(
                    Policy(1, ReimbursementExpenseType.Travel, 2_500, 1_500, ExpenseClassification.ClientRecoverable),
                    Policy(2, ReimbursementExpenseType.CourtFiling, 15_000, 5_000, ExpenseClassification.ClientRecoverable),
                    Policy(3, ReimbursementExpenseType.SheriffService, 10_000, 5_000, ExpenseClassification.ClientRecoverable),
                    Policy(4, ReimbursementExpenseType.Courier, 1_500, 1_000, ExpenseClassification.ClientRecoverable),
                    Policy(5, ReimbursementExpenseType.Accommodation, 3_500, 2_500, ExpenseClassification.ClientRecoverable),
                    Policy(6, ReimbursementExpenseType.Meals, 600, 500, ExpenseClassification.FirmOverhead),
                    Policy(7, ReimbursementExpenseType.Parking, 500, 500, ExpenseClassification.ClientRecoverable),
                    Policy(8, ReimbursementExpenseType.Printing, 2_000, 1_000, ExpenseClassification.FirmOverhead),
                    Policy(9, ReimbursementExpenseType.Other, 1_000, 500, ExpenseClassification.FirmOverhead));
            });
            modelBuilder.Entity<MatterExpenseTerm>(e => {
                e.HasIndex(x => new { x.CaseId, x.ExpenseType }).IsUnique();
                e.HasOne(x => x.Case).WithMany().HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<ReimbursementClaim>(e => {
                e.HasIndex(x => x.ClaimNumber).IsUnique();
                e.HasIndex(x => new { x.AttorneyId, x.SubmittedAtUtc });
                e.HasIndex(x => new { x.Status, x.SubmittedAtUtc });
                e.Property(x => x.Amount).HasPrecision(18, 2);
                e.Property(x => x.PolicyLimitSnapshot).HasPrecision(18, 2);
                e.Property(x => x.DelegatedLimitSnapshot).HasPrecision(18, 2);
                e.HasOne(x => x.Case).WithMany().HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Attorney).WithMany().HasForeignKey(x => x.AttorneyId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.DecidedByUser).WithMany().HasForeignKey(x => x.DecidedByUserId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<AttorneyReimbursementPayable>(e => {
                e.HasIndex(x => x.ReimbursementClaimId).IsUnique();
                e.Property(x => x.Amount).HasPrecision(18, 2);
                e.HasOne(x => x.Claim).WithOne(x => x.Payable).HasForeignKey<AttorneyReimbursementPayable>(x => x.ReimbursementClaimId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Attorney).WithMany().HasForeignKey(x => x.AttorneyId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<MatterDisbursement>(e => {
                e.HasIndex(x => x.ReimbursementClaimId).IsUnique();
                e.HasIndex(x => new { x.CaseId, x.InvoiceId });
                e.Property(x => x.Amount).HasPrecision(18, 2);
                e.HasOne(x => x.Claim).WithOne(x => x.Disbursement).HasForeignKey<MatterDisbursement>(x => x.ReimbursementClaimId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Case).WithMany().HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Invoice).WithMany(x => x.Disbursements).HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<ReimbursementAuditEntry>(e => {
                e.HasIndex(x => new { x.ReimbursementClaimId, x.RecordedAtUtc });
                e.HasOne(x => x.Claim).WithMany(x => x.AuditEntries).HasForeignKey(x => x.ReimbursementClaimId).OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<LegalCostRecoveryClaim>(e => {
                e.HasIndex(x => new { x.Status, x.SubmittedAtUtc });
                e.Property(x => x.ClaimedAmount).HasPrecision(18, 2);
                e.Property(x => x.AwardedAmount).HasPrecision(18, 2);
                e.HasOne(x => x.Case).WithMany().HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Attorney).WithMany().HasForeignKey(x => x.AttorneyId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.DecidedByUser).WithMany().HasForeignKey(x => x.DecidedByUserId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<Case>().Property(x => x.CostRecoveryAwardedTotal).HasPrecision(18, 2);
            modelBuilder.Entity<LegalCostRecoveryTimeEntry>(e => {
                e.HasKey(x => new { x.LegalCostRecoveryClaimId, x.TimeEntryId });
                e.HasIndex(x => x.TimeEntryId).IsUnique();
                e.Property(x => x.AmountSnapshot).HasPrecision(18, 2);
                e.HasOne(x => x.Claim).WithMany(x => x.TimeEntries).HasForeignKey(x => x.LegalCostRecoveryClaimId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.TimeEntry).WithMany().HasForeignKey(x => x.TimeEntryId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<LegalCostRecoveryAuditEntry>(e => {
                e.HasIndex(x => new { x.LegalCostRecoveryClaimId, x.RecordedAtUtc });
                e.HasOne(x => x.Claim).WithMany(x => x.AuditEntries).HasForeignKey(x => x.LegalCostRecoveryClaimId).OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<LitigationStrategyDecision>(e => {
                e.HasIndex(x => new { x.CaseId, x.Status });
                e.Property(x => x.CostsIncurredSnapshot).HasPrecision(18,2); e.Property(x => x.ProjectedCostToCompletion).HasPrecision(18,2); e.Property(x => x.MatterValueSnapshot).HasPrecision(18,2); e.Property(x => x.ComparableSettlementLow).HasPrecision(18,2); e.Property(x => x.ComparableSettlementHigh).HasPrecision(18,2); e.Property(x => x.ProspectsSnapshot).HasPrecision(6,5);
                e.HasOne(x => x.Case).WithMany().HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Attorney).WithMany().HasForeignKey(x => x.AttorneyId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Director).WithMany().HasForeignKey(x => x.DirectorId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<CaseDocumentRequirement>(e => { e.HasIndex(x => new { x.CaseType, x.Code }).IsUnique(); });
            modelBuilder.Entity<CaseDocumentWaiver>(e => {
                e.HasIndex(x => new { x.CaseId, x.RequirementId, x.Status });
                e.HasOne(x => x.Case).WithMany().HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Requirement).WithMany().HasForeignKey(x => x.RequirementId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<CaseReadinessReview>(e => e.HasIndex(x => new { x.CaseId, x.ReviewedAtUtc }));
            modelBuilder.Entity<Case>().Property(x => x.MatterValue).HasPrecision(18,2);
            modelBuilder.Entity<Case>().Property(x => x.SettlementAmount).HasPrecision(18,2);

            modelBuilder.Entity<Beneficiary>(entity =>
            {
                entity.HasIndex(x => x.BenefactorClientId);
                entity.HasIndex(x => x.Status);
                entity.HasIndex(x => x.Email);
                entity.Property(x => x.Status).HasConversion<string>();
                entity.Property(x => x.AssetAccessTerms).HasMaxLength(2000);
                entity.Property(x => x.PermittedAssetPurposes).HasMaxLength(1000);
                entity.Property(x => x.EntitlementDescription).HasMaxLength(1000);
                entity.Property(x => x.PortalPasswordHash).HasMaxLength(500);
                if (Database.ProviderName?.Contains("Sqlite") == true) entity.Property(x => x.RowVersion).IsConcurrencyToken().ValueGeneratedNever();
                else entity.Property(x => x.RowVersion).IsRowVersion();
                entity.HasOne(x => x.BenefactorClient).WithMany().HasForeignKey(x => x.BenefactorClientId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.ReviewedByUser).WithMany().HasForeignKey(x => x.ReviewedByUserId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<BeneficiaryInvitation>(entity =>
            {
                entity.HasIndex(x => x.TokenHash).IsUnique();
                entity.HasIndex(x => x.ExpiresAtUtc);
                entity.Property(x => x.TokenHash).HasMaxLength(64);
                entity.HasOne(x => x.Beneficiary).WithMany().HasForeignKey(x => x.BeneficiaryId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<BeneficiaryDocumentRequirement>(entity =>
            {
                entity.HasIndex(x => x.Code).IsUnique();
                entity.Property(x => x.Code).HasMaxLength(50);
            });
            modelBuilder.Entity<BeneficiaryRequirementAssignment>(entity =>
            {
                entity.HasKey(x => new { x.BeneficiaryId, x.RequirementId });
                entity.HasOne(x => x.Beneficiary).WithMany(x => x.RequirementAssignments).HasForeignKey(x => x.BeneficiaryId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(x => x.Requirement).WithMany().HasForeignKey(x => x.RequirementId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<BeneficiaryDocument>(entity =>
            {
                entity.HasIndex(x => x.BeneficiaryId);
                entity.HasIndex(x => x.RequirementId);
                entity.HasIndex(x => x.PreScreenStatus);
                entity.Property(x => x.PreScreenStatus).HasConversion<string>();
                entity.Property(x => x.QualityScore).HasPrecision(6, 5);
                entity.Property(x => x.OcrConfidence).HasPrecision(6, 5);
                entity.HasOne(x => x.Beneficiary).WithMany().HasForeignKey(x => x.BeneficiaryId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.Requirement).WithMany().HasForeignKey(x => x.RequirementId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<FacialVerificationSession>(entity =>
            {
                entity.HasIndex(x => x.BeneficiaryId);
                entity.HasIndex(x => x.Status);
                entity.HasIndex(x => x.ExpiresAtUtc);
                entity.Property(x => x.Status).HasConversion<string>();
                entity.Property(x => x.SimilarityScore).HasPrecision(6, 5);
                entity.Property(x => x.ValidFrameRatio).HasPrecision(6, 5);
                entity.Property(x => x.DuplicateFrameRatio).HasPrecision(6, 5);
                if (Database.ProviderName?.Contains("Sqlite") == true) entity.Property(x => x.RowVersion).IsConcurrencyToken().ValueGeneratedNever();
                else entity.Property(x => x.RowVersion).IsRowVersion();
                entity.HasOne(x => x.Beneficiary).WithMany().HasForeignKey(x => x.BeneficiaryId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<Beneficiary>().Property(x => x.EntitlementAmountLimit).HasPrecision(18, 2);
            modelBuilder.Entity<SystemNotification>(entity =>
            {
                entity.HasIndex(x => x.DeduplicationKey).IsUnique().HasFilter("[DeduplicationKey] IS NOT NULL");
                entity.Property(x => x.Type).HasMaxLength(50);
                entity.Property(x => x.Title).HasMaxLength(200);
                entity.Property(x => x.ActionUrl).HasMaxLength(500);
                entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<EmailOutboxMessage>(entity =>
            {
                entity.HasIndex(x => x.DeduplicationKey).IsUnique();
                entity.HasIndex(x => new { x.Status, x.NextAttemptAtUtc });
                entity.Property(x => x.Status).HasConversion<string>();
                entity.Property(x => x.ToAddress).HasMaxLength(254);
                entity.Property(x => x.Subject).HasMaxLength(300);
                entity.Property(x => x.LastError).HasMaxLength(1000);
            });
            modelBuilder.Entity<AppointmentInvitation>(entity =>
            {
                entity.HasIndex(x => x.TokenHash).IsUnique();
                entity.HasIndex(x => x.ExpiresAtUtc);
                entity.Property(x => x.TokenHash).HasMaxLength(64);
                entity.HasOne(x => x.CalendarEvent).WithMany().HasForeignKey(x => x.CalendarEventId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<AppointmentBillingRecord>(entity =>
            {
                entity.HasIndex(x => x.CalendarEventId).IsUnique();
                entity.HasIndex(x => x.IdempotencyKey).IsUnique();
                entity.Property(x => x.Status).HasConversion<string>();
                entity.Property(x => x.AppointmentFee).HasPrecision(18, 2);
                entity.Property(x => x.CoveredAmount).HasPrecision(18, 2);
                entity.Property(x => x.InvoicedAmount).HasPrecision(18, 2);
                entity.HasOne(x => x.CalendarEvent).WithMany().HasForeignKey(x => x.CalendarEventId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<InvoicePenalty>(entity =>
            {
                entity.HasIndex(x => x.IdempotencyKey).IsUnique();
                entity.Property(x => x.Type).HasConversion<string>();
                entity.Property(x => x.BasisAmount).HasPrecision(18, 2);
                entity.Property(x => x.PenaltyValue).HasPrecision(18, 2);
                entity.Property(x => x.Amount).HasPrecision(18, 2);
                entity.Property(x => x.Reason).IsRequired().HasMaxLength(1000);
                entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.AppliedByAccountantId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.Invoice).WithMany().HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<AuditEntry>(entity =>
            {
                entity.HasIndex(x => new { x.EntityType, x.EntityId });
                entity.HasIndex(x => x.CreatedAtUtc);
            });

            // ========== USER CONFIGURATION ==========
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.FullName).IsRequired().HasMaxLength(200);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(200);
                entity.Property(u => u.PasswordHash).IsRequired();
                entity.Property(u => u.Role).HasConversion<string>();
            });

            // ========== CLIENT CONFIGURATION ==========
            modelBuilder.Entity<Client>(entity =>
            {
                entity.HasIndex(c => c.Email);
                entity.Property(c => c.FirstName).HasMaxLength(100);
                entity.Property(c => c.LastName).HasMaxLength(100);
                entity.Property(c => c.CompanyName).HasMaxLength(200);
                entity.Property(c => c.RegistrationNumber).HasMaxLength(50);
                entity.Property(c => c.SAIDNumber).HasMaxLength(13);
                entity.Property(c => c.Phone).HasMaxLength(20);

                // Computed FullName property (will be handled in model)
                entity.Ignore(c => c.FullName);
            });

            // ========== CASE CONFIGURATION ==========
            modelBuilder.Entity<Case>(entity =>
            {
                entity.HasIndex(c => c.CaseNumber).IsUnique();
                entity.Property(c => c.Title).IsRequired().HasMaxLength(200);
                entity.Property(c => c.CaseNumber).IsRequired().HasMaxLength(50);
                entity.Property(c => c.Status).HasConversion<string>();
                entity.Property(c => c.EvidenceStrength).HasPrecision(6, 5);

                entity.HasOne(c => c.Client)
                    .WithMany(c => c.Cases)
                    .HasForeignKey(c => c.ClientId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.Lawyer)
                    .WithMany(u => u.AssignedCases)
                    .HasForeignKey(c => c.LawyerId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ========== RETAINER CONFIGURATION ==========
           
            modelBuilder.Entity<Retainer>(entity =>
            {
                entity.HasIndex(r => r.SignatureToken);
                entity.HasIndex(r => r.Status);
                entity.HasIndex(r => r.ClientId);

                entity.Property(r => r.Title).IsRequired().HasMaxLength(200);
                entity.Property(r => r.ScopeOfWork).IsRequired();
                entity.Property(r => r.Type).HasConversion<string>();
                entity.Property(r => r.Status).HasConversion<string>();
                entity.Property(r => r.Amount).HasPrecision(18, 2);
                entity.Property(r => r.OverageRate).HasPrecision(18, 2);
                entity.Property(r => r.AmountPaid).HasPrecision(18, 2);
                entity.Property(r => r.AvailableBalance).HasPrecision(18, 2);
                entity.Property(r => r.BillingCycle).HasMaxLength(50);
                entity.Property(r => r.ClientSignatureName).HasMaxLength(200);
                entity.Property(r => r.ClientIPAddress).HasMaxLength(50);
                entity.Property(r => r.PaymentReference).HasMaxLength(100);
                entity.Property(r => r.SignatureToken).HasMaxLength(100);

                // Relationships - FIX: Use Restrict instead of Cascade to prevent multiple cascade paths
                entity.HasOne(r => r.Client)
                    .WithMany()
                    .HasForeignKey(r => r.ClientId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Case)
                    .WithMany()
                    .HasForeignKey(r => r.CaseId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(r => r.Template)
                    .WithMany()
                    .HasForeignKey(r => r.TemplateId)
                    .OnDelete(DeleteBehavior.SetNull);

                // CRITICAL FIX: Change these from Cascade to Restrict
                entity.HasOne(r => r.ApprovedByUser)
                    .WithMany()
                    .HasForeignKey(r => r.ApprovedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);  // Changed from Cascade

                entity.HasOne(r => r.SubmittedByUser)
                    .WithMany()
                    .HasForeignKey(r => r.SubmittedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);  // Changed from Cascade
            });

            // ========== RETAINER TEMPLATE CONFIGURATION ==========
            modelBuilder.Entity<RetainerTemplate>(entity =>
            {
                entity.Property(t => t.Name).IsRequired().HasMaxLength(200);
                entity.Property(t => t.Category).HasMaxLength(100);
                entity.Property(t => t.BillingCycle).HasMaxLength(50);
                entity.Property(t => t.PriceDisplay).HasMaxLength(100);
                entity.Property(t => t.EstimatedDuration).HasMaxLength(100);
                entity.Property(t => t.Type).HasConversion<string>();
                entity.Property(t => t.BasePrice).HasPrecision(18, 2);
                entity.Property(t => t.OverageRate).HasPrecision(18, 2);

                entity.HasIndex(t => t.IsPublic);
                entity.HasIndex(t => t.Category);
            });

            // ========== RETAINER PAYMENT SCHEDULE CONFIGURATION ==========
            // Retainer Payment Schedule Configuration
            modelBuilder.Entity<RetainerPaymentSchedule>(entity =>
            {
                entity.Property(ps => ps.Status).HasConversion<string>();
                entity.Property(ps => ps.AmountDue).HasPrecision(18, 2);
                entity.Property(ps => ps.PaymentReference).HasMaxLength(100);

                entity.HasOne(ps => ps.Retainer)
                    .WithMany(r => r.PaymentSchedules)
                    .HasForeignKey(ps => ps.RetainerId)
                    .OnDelete(DeleteBehavior.Restrict);  // Changed from Cascade to Restrict

                entity.HasIndex(ps => ps.Status);
                entity.HasIndex(ps => ps.DueDate);
            });
            // Retainer Payment Configuration
            modelBuilder.Entity<RetainerPayment>(entity =>
            {
                entity.Property(rp => rp.Amount).HasPrecision(18, 2);
                entity.Property(rp => rp.PaymentMethod).HasConversion<string>();
                entity.Property(rp => rp.TransactionReference).HasMaxLength(100);
                entity.Property(rp => rp.Notes).HasMaxLength(500);

                entity.HasOne(rp => rp.Retainer)
                    .WithMany(r => r.Payments)
                    .HasForeignKey(rp => rp.RetainerId)
                    .OnDelete(DeleteBehavior.Restrict);  // Changed from Cascade to Restrict

                entity.HasOne(rp => rp.PaymentSchedule)
                    .WithMany()
                    .HasForeignKey(rp => rp.PaymentScheduleId)
                    .OnDelete(DeleteBehavior.SetNull);  // Keep as SetNull

                entity.HasIndex(rp => rp.PaymentDate);
                entity.HasIndex(rp => rp.TransactionReference);
            });


            // ========== RETAINER ACTION LOG CONFIGURATION ==========
            modelBuilder.Entity<RetainerActionLog>(entity =>
            {
                entity.HasKey(l => l.Id);
                entity.Property(l => l.Action).IsRequired().HasMaxLength(100);
                entity.Property(l => l.Details).HasMaxLength(1000);

                entity.HasOne(l => l.Retainer)
                    .WithMany(r => r.ActionLogs)
                    .HasForeignKey(l => l.RetainerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(l => l.User)
                    .WithMany()
                    .HasForeignKey(l => l.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(l => l.RetainerId);
                entity.HasIndex(l => l.CreatedAt);
            });

            // ========== RETAINER RENEWAL CONFIGURATION ==========
            modelBuilder.Entity<RetainerRenewal>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Notes).HasMaxLength(500);
                entity.Property(r => r.AmountAdjustment).HasPrecision(18, 2);

                entity.HasOne(r => r.Retainer)
                    .WithMany(r => r.Renewals)
                    .HasForeignKey(r => r.RetainerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.RenewedByUser)
                    .WithMany()
                    .HasForeignKey(r => r.RenewedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(r => r.RetainerId);
            });
            // ========== CLIENT REQUEST CONFIGURATION ==========
            modelBuilder.Entity<ClientRequest>(entity =>
            {
                entity.Property(cr => cr.Status).IsRequired().HasMaxLength(50);
                entity.Property(cr => cr.PreferredContact).HasMaxLength(50);
                entity.Property(cr => cr.Urgency).HasMaxLength(50);

                entity.HasOne(cr => cr.Client)
                    .WithMany()
                    .HasForeignKey(cr => cr.ClientId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(cr => cr.Template)
                    .WithMany(t => t.ClientRequests)
                    .HasForeignKey(cr => cr.TemplateId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(cr => cr.Status);
                entity.HasIndex(cr => cr.CreatedDate);
            });

            // ========== INVOICE CONFIGURATION ==========
            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.HasIndex(i => i.InvoiceNumber).IsUnique();
                entity.Property(i => i.InvoiceNumber).IsRequired().HasMaxLength(50);
                entity.Property(i => i.Status).HasConversion<string>();
                entity.Property(i => i.Amount).HasPrecision(18, 2);
                entity.Property(i => i.TaxAmount).HasPrecision(18, 2);
                entity.Property(i => i.TotalAmount).HasPrecision(18, 2);
                entity.Property(i => i.Description).HasMaxLength(500);

                entity.HasOne(i => i.Client)
                    .WithMany()
                    .HasForeignKey(i => i.ClientId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(i => i.Case)
                    .WithMany()
                    .HasForeignKey(i => i.CaseId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(i => i.Retainer)
                    .WithMany()
                    .HasForeignKey(i => i.RetainerId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(i => i.MatterCostEstimate)
                    .WithMany()
                    .HasForeignKey(i => i.MatterCostEstimateId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ========== PAYMENT CONFIGURATION ==========
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.Property(p => p.Amount).HasPrecision(18, 2);
                entity.Property(p => p.PaymentMethod).HasConversion<string>();
                entity.Property(p => p.TransactionReference).HasMaxLength(100);
                entity.Property(p => p.Notes).HasMaxLength(500);

                entity.HasOne(p => p.Invoice)
                    .WithMany(i => i.Payments)
                    .HasForeignKey(p => p.InvoiceId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Client)
                    .WithMany()
                    .HasForeignKey(p => p.ClientId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(p => p.PaymentDate);
                entity.HasIndex(p => p.TransactionReference);
            });

            // Time Entry Configuration
            modelBuilder.Entity<TimeEntry>(entity =>
            {
                entity.Property(te => te.Description).IsRequired().HasMaxLength(500);
                entity.Property(te => te.Hours).HasPrecision(18, 2);
                entity.Property(te => te.HourlyRate).HasPrecision(18, 2);
                entity.Property(te => te.TotalAmount).HasPrecision(18, 2);

                entity.HasOne(te => te.Lawyer)
                    .WithMany()
                    .HasForeignKey(te => te.LawyerId)
                    .OnDelete(DeleteBehavior.Restrict);  // Changed from No Action

                entity.HasOne(te => te.Case)
                    .WithMany()
                    .HasForeignKey(te => te.CaseId)
                    .OnDelete(DeleteBehavior.Restrict);  // Changed from No Action

                entity.HasOne(te => te.Retainer)
                    .WithMany()
                    .HasForeignKey(te => te.RetainerId)
                    .OnDelete(DeleteBehavior.SetNull);  // Keep as SetNull

                entity.HasOne(te => te.Invoice)
                    .WithMany(i => i.TimeEntries)
                    .HasForeignKey(te => te.InvoiceId)
                    .OnDelete(DeleteBehavior.SetNull);  // Keep as SetNull

                entity.HasIndex(te => te.Date);
                entity.HasIndex(te => te.IsBillable);
            });

            // ========== TRUST ACCOUNT CONFIGURATION ==========
            modelBuilder.Entity<TrustAccount>(entity =>
            {
                entity.Property(ta => ta.Balance).HasPrecision(18, 2);
                entity.Property(ta => ta.TotalDeposited).HasPrecision(18, 2);
                entity.Property(ta => ta.TotalWithdrawn).HasPrecision(18, 2);

                entity.HasOne(ta => ta.Client)
                    .WithMany()
                    .HasForeignKey(ta => ta.ClientId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(ta => ta.ClientId).IsUnique();
            });

            // ========== TRUST TRANSACTION CONFIGURATION ==========
            modelBuilder.Entity<TrustTransaction>(entity =>
            {
                entity.Property(tt => tt.Amount).HasPrecision(18, 2);
                entity.Property(tt => tt.Type).HasConversion<string>();
                entity.Property(tt => tt.Description).HasMaxLength(500);
                entity.Property(tt => tt.Reference).HasMaxLength(100);
                entity.Property(tt => tt.AuthorizedBy).HasMaxLength(200);

                entity.HasOne(tt => tt.TrustAccount)
                    .WithMany(ta => ta.Transactions)
                    .HasForeignKey(tt => tt.TrustAccountId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(tt => tt.TransactionDate);
                entity.HasIndex(tt => tt.Type);
            });

            // ========== CALENDAR EVENT CONFIGURATION ==========
            modelBuilder.Entity<CalendarEvent>(entity =>
            {
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Location).HasMaxLength(200);
                entity.Property(e => e.Type).HasConversion<string>();
                entity.Property(e => e.Status).HasConversion<string>();
                entity.Property(e => e.ClientResponseStatus).HasConversion<string>();
                entity.Property(e => e.LatePenaltyType).HasConversion<string>();
                entity.Property(e => e.AppointmentFee).HasPrecision(18, 2);
                entity.Property(e => e.LatePenaltyValue).HasPrecision(18, 2);
                if (Database.ProviderName?.Contains("Sqlite") == true) entity.Property(e => e.RowVersion).IsConcurrencyToken().ValueGeneratedNever();
                else entity.Property(e => e.RowVersion).IsRowVersion();

                entity.HasOne(e => e.Case)
                    .WithMany()
                    .HasForeignKey(e => e.CaseId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Client).WithMany().HasForeignKey(e => e.ClientId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.GeneratedInvoice).WithMany().HasForeignKey(e => e.GeneratedInvoiceId).OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.StartDateTime);
                entity.HasIndex(e => e.Status);
            });

            // ========== EVENT ATTENDEE CONFIGURATION ==========
            modelBuilder.Entity<EventAttendee>(entity =>
            {
                entity.Property(ea => ea.ResponseStatus).HasMaxLength(50);

                entity.HasOne(ea => ea.Event)
                    .WithMany(e => e.Attendees)
                    .HasForeignKey(ea => ea.EventId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ea => ea.User)
                    .WithMany()
                    .HasForeignKey(ea => ea.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ========== EVENT REMINDER CONFIGURATION ==========
            modelBuilder.Entity<EventReminder>(entity =>
            {
                entity.HasOne(er => er.Event)
                    .WithMany(e => e.Reminders)
                    .HasForeignKey(er => er.EventId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(er => er.ReminderMinutesBefore);
                entity.HasIndex(er => er.IsSent);
            });

            // ========== TASK CONFIGURATION ==========
            modelBuilder.Entity<TaskItem>(entity =>
            {
                entity.Property(t => t.Title).IsRequired().HasMaxLength(200);
                entity.Property(t => t.Description).HasMaxLength(1000);
                entity.Property(t => t.Priority).HasConversion<string>();
                entity.Property(t => t.Status).HasConversion<string>();
                entity.Property(t => t.EstimatedHours).HasPrecision(10, 2);
                entity.Property(t => t.ActualHours).HasPrecision(10, 2);

                entity.HasOne(t => t.AssignedTo)
                    .WithMany()
                    .HasForeignKey(t => t.AssignedToId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.CreatedBy)
                    .WithMany()
                    .HasForeignKey(t => t.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.Case)
                    .WithMany()
                    .HasForeignKey(t => t.CaseId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(t => t.RelatedEvent)
                    .WithMany()
                    .HasForeignKey(t => t.RelatedEventId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(t => t.Status);
                entity.HasIndex(t => t.DueDate);
                entity.HasIndex(t => t.Priority);
            });
            modelBuilder.Entity<LawyerProfile>().Property(x => x.HourlyRate).HasPrecision(18, 2);

            // ========== TASK COMMENT CONFIGURATION ==========
            modelBuilder.Entity<TaskComment>(entity =>
            {
                entity.Property(tc => tc.Comment).IsRequired().HasMaxLength(1000);

                entity.HasOne(tc => tc.Task)
                    .WithMany(t => t.Comments)
                    .HasForeignKey(tc => tc.TaskId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(tc => tc.User)
                    .WithMany()
                    .HasForeignKey(tc => tc.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ========== TASK ATTACHMENT CONFIGURATION ==========
            modelBuilder.Entity<TaskAttachment>(entity =>
            {
                entity.Property(ta => ta.FileName).IsRequired().HasMaxLength(200);
                entity.Property(ta => ta.FilePath).IsRequired().HasMaxLength(500);

                entity.HasOne(ta => ta.Task)
                    .WithMany(t => t.Attachments)
                    .HasForeignKey(ta => ta.TaskId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ========== DOCUMENT SHARE FIX ==========
            modelBuilder.Entity<DocumentShare>(entity =>
            {
                entity.HasOne(ds => ds.Document)
                    .WithMany(d => d.SharedWith)
                    .HasForeignKey(ds => ds.DocumentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ds => ds.SharedWithUser)
                    .WithMany()
                    .HasForeignKey(ds => ds.SharedWithUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ds => ds.SharedByUser)
                    .WithMany()
                    .HasForeignKey(ds => ds.SharedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ========== LAWYER PROFILE CONFIGURATION ==========
           

            // ========== LAWYER SPECIALIZATION CONFIGURATION ==========
           

            // ========== TABLE NAMES ==========
            modelBuilder.Entity<ApplicationUser>().ToTable("Users");
            modelBuilder.Entity<Client>().ToTable("Clients");
            modelBuilder.Entity<Case>().ToTable("Cases");
            modelBuilder.Entity<CaseNote>().ToTable("CaseNotes");
            modelBuilder.Entity<Document>().ToTable("Documents");
            modelBuilder.Entity<DocumentShare>().ToTable("DocumentShares");
            modelBuilder.Entity<Invoice>().ToTable("Invoices");
            modelBuilder.Entity<Payment>().ToTable("Payments");
            modelBuilder.Entity<TimeEntry>().ToTable("TimeEntries");
            modelBuilder.Entity<Retainer>().ToTable("Retainers");
            modelBuilder.Entity<RetainerTemplate>().ToTable("RetainerTemplates");
            modelBuilder.Entity<RetainerPaymentSchedule>().ToTable("RetainerPaymentSchedules");
            modelBuilder.Entity<RetainerPayment>().ToTable("RetainerPayments");
            modelBuilder.Entity<ClientRequest>().ToTable("ClientRequests");
            modelBuilder.Entity<CalendarEvent>().ToTable("CalendarEvents");
            modelBuilder.Entity<EventAttendee>().ToTable("EventAttendees");
            modelBuilder.Entity<EventReminder>().ToTable("EventReminders");
            modelBuilder.Entity<TaskItem>().ToTable("Tasks");
            modelBuilder.Entity<TaskComment>().ToTable("TaskComments");
            modelBuilder.Entity<TaskAttachment>().ToTable("TaskAttachments");
            modelBuilder.Entity<TrustAccount>().ToTable("TrustAccounts");
            modelBuilder.Entity<TrustTransaction>().ToTable("TrustTransactions");
            modelBuilder.Entity<LawyerProfile>().ToTable("LawyerProfiles");
            modelBuilder.Entity<LawyerSpecialization>().ToTable("LawyerSpecializations");
        }

        private static ExpensePolicy Policy(int id, ReimbursementExpenseType type, decimal limit, decimal delegated, ExpenseClassification classification) =>
            new() { Id = id, ExpenseType = type, PerItemLimit = limit, DelegatedApprovalLimit = delegated, DefaultClassification = classification, IsActive = true };
        private static LegalSubject Subject(int id, string name, string keywords) =>
            new() { Id = id, Name = name, Keywords = keywords, IsActive = true };
    }
}
