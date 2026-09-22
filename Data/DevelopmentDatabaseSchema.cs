using Microsoft.EntityFrameworkCore;
using System.Data;

namespace SimplexLawFirm.Data;

public static class DevelopmentDatabaseSchema
{
    public static async Task EnsureBeneficiaryPortalCredentialsAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        var connection = context.Database.GetDbConnection();
        var wasOpen = connection.State == ConnectionState.Open;
        if (!wasOpen) await connection.OpenAsync(cancellationToken);
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA table_info(\"Beneficiaries\")";
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) columns.Add(reader.GetString(reader.GetOrdinal("name")));
        }

        if (!columns.Contains("PortalAccessEnabled", StringComparer.OrdinalIgnoreCase))
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE \"Beneficiaries\" ADD COLUMN \"PortalAccessEnabled\" INTEGER NOT NULL DEFAULT 0", cancellationToken);
        if (!columns.Contains("PortalPasswordHash", StringComparer.OrdinalIgnoreCase))
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE \"Beneficiaries\" ADD COLUMN \"PortalPasswordHash\" TEXT NULL", cancellationToken);
        if (!columns.Contains("PortalPasswordSetAtUtc", StringComparer.OrdinalIgnoreCase))
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE \"Beneficiaries\" ADD COLUMN \"PortalPasswordSetAtUtc\" TEXT NULL", cancellationToken);
        if (!columns.Contains("BankAccountHolder", StringComparer.OrdinalIgnoreCase))
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE \"Beneficiaries\" ADD COLUMN \"BankAccountHolder\" TEXT NULL", cancellationToken);
        if (!columns.Contains("BankName", StringComparer.OrdinalIgnoreCase))
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE \"Beneficiaries\" ADD COLUMN \"BankName\" TEXT NULL", cancellationToken);
        if (!columns.Contains("BankAccountNumber", StringComparer.OrdinalIgnoreCase))
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE \"Beneficiaries\" ADD COLUMN \"BankAccountNumber\" TEXT NULL", cancellationToken);
        if (!columns.Contains("BankBranchCode", StringComparer.OrdinalIgnoreCase))
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE \"Beneficiaries\" ADD COLUMN \"BankBranchCode\" TEXT NULL", cancellationToken);
        if (!columns.Contains("BankDetailsConfirmedAtUtc", StringComparer.OrdinalIgnoreCase))
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE \"Beneficiaries\" ADD COLUMN \"BankDetailsConfirmedAtUtc\" TEXT NULL", cancellationToken);
        if (!columns.Contains("EntitlementAmountLimit", StringComparer.OrdinalIgnoreCase))
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE \"Beneficiaries\" ADD COLUMN \"EntitlementAmountLimit\" TEXT NULL", cancellationToken);
        var calendarColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA table_info(\"CalendarEvents\")";
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) calendarColumns.Add(reader.GetString(reader.GetOrdinal("name")));
        }
        if (calendarColumns.Count > 0 && !calendarColumns.Contains("LawyerApprovalStatus"))
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE \"CalendarEvents\" ADD COLUMN \"LawyerApprovalStatus\" TEXT NOT NULL DEFAULT 'NotRequired'", cancellationToken);
        await EnsureColumnAsync(connection, "Cases", "MatterValue", "TEXT NOT NULL DEFAULT '0'", cancellationToken);
        await EnsureColumnAsync(connection, "Cases", "CostRecoveryAwardedTotal", "TEXT NOT NULL DEFAULT '0'", cancellationToken);
        await EnsureColumnAsync(connection, "Cases", "SettlementAmount", "TEXT NULL", cancellationToken);
        await EnsureColumnAsync(connection, "Cases", "IsCourtReady", "INTEGER NOT NULL DEFAULT 0", cancellationToken);
        await EnsureColumnAsync(connection, "Cases", "CourtReadyAtUtc", "TEXT NULL", cancellationToken);
        await EnsureColumnAsync(connection, "Cases", "StrategyReviewRequired", "INTEGER NOT NULL DEFAULT 0", cancellationToken);
        await EnsureColumnAsync(connection, "Documents", "RequirementCode", "TEXT NULL", cancellationToken);
        await EnsureColumnAsync(connection, "Documents", "CaseDocumentRequestId", "INTEGER NULL", cancellationToken);
        await EnsureColumnAsync(connection, "ServiceComplaints", "DirectorFeedback", "TEXT NULL", cancellationToken);
        await EnsureColumnAsync(connection, "ServiceComplaints", "RespondedAtUtc", "TEXT NULL", cancellationToken);
        await EnsureColumnAsync(connection, "CaseHandovers", "AcceptanceRiskAcknowledged", "INTEGER NOT NULL DEFAULT 0", cancellationToken);
        await EnsureColumnAsync(connection, "CaseHandovers", "AcceptanceSignatureName", "TEXT NULL", cancellationToken);
        await EnsureColumnAsync(connection, "CaseHandovers", "ReceivingAttorneyQuery", "TEXT NULL", cancellationToken);
        await EnsureColumnAsync(connection, "CaseHandovers", "DirectorQueryResponse", "TEXT NULL", cancellationToken);
        await EnsureColumnAsync(connection, "CaseHandovers", "DirectorQueryRespondedByUserId", "INTEGER NULL", cancellationToken);
        await EnsureColumnAsync(connection, "CaseHandovers", "DirectorQueryRespondedAtUtc", "TEXT NULL", cancellationToken);
        await EnsureColumnAsync(connection, "CaseHandovers", "DirectorQueryVerdictFinal", "INTEGER NOT NULL DEFAULT 0", cancellationToken);
        await EnsureColumnAsync(connection, "ReimbursementClaims", "ApprovedAmount", "TEXT NULL", cancellationToken);
        await EnsureColumnAsync(connection, "CaseHandovers", "ReceivingAttorneyDeclineReason", "TEXT NULL", cancellationToken);
        await EnsureColumnAsync(connection, "CaseHandovers", "ReceivingAttorneyRespondedAtUtc", "TEXT NULL", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS CaseDocumentRequests (Id INTEGER PRIMARY KEY AUTOINCREMENT, CaseId INTEGER NOT NULL, ClientId INTEGER NOT NULL, RequestedByAttorneyId INTEGER NOT NULL, Title TEXT NOT NULL, Instructions TEXT NOT NULL, RequiredDocuments TEXT NOT NULL, DueAtUtc TEXT NOT NULL, Status INTEGER NOT NULL, ReviewFeedback TEXT NULL, CreatedAtUtc TEXT NOT NULL, SubmittedAtUtc TEXT NULL, ReviewedAtUtc TEXT NULL)", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS IX_CaseDocumentRequests_CaseId_Status ON CaseDocumentRequests(CaseId, Status)", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS IX_CaseDocumentRequests_ClientId ON CaseDocumentRequests(ClientId)", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS IX_CaseDocumentRequests_RequestedByAttorneyId ON CaseDocumentRequests(RequestedByAttorneyId)", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS CaseHandoverRequests (Id INTEGER PRIMARY KEY AUTOINCREMENT, CaseId INTEGER NOT NULL, RequestedByAttorneyId INTEGER NOT NULL, Reason TEXT NOT NULL, DirectorResponse TEXT NULL, Status INTEGER NOT NULL, RequestedAtUtc TEXT NOT NULL, ReviewedAtUtc TEXT NULL, ReviewedByDirectorId INTEGER NULL, CaseHandoverId INTEGER NULL)", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS IX_CaseHandoverRequests_CaseId ON CaseHandoverRequests(CaseId)", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS IX_CaseHandoverRequests_Status_RequestedAtUtc ON CaseHandoverRequests(Status, RequestedAtUtc)", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS ExternalEvidenceRequests (Id INTEGER PRIMARY KEY AUTOINCREMENT, CaseId INTEGER NOT NULL, RecipientEmail TEXT NOT NULL, RecipientName TEXT NOT NULL, TokenHash TEXT NOT NULL, CreatedAtUtc TEXT NOT NULL, ExpiresAtUtc TEXT NOT NULL, AccessedAtUtc TEXT NULL, ClosedAtUtc TEXT NULL, RevokedAtUtc TEXT NULL, RequestedByUserId INTEGER NOT NULL)", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS ExternalEvidenceDocuments (Id INTEGER PRIMARY KEY AUTOINCREMENT, RequestId INTEGER NOT NULL, OriginalFileName TEXT NOT NULL, Purpose TEXT NOT NULL, RelativePath TEXT NOT NULL, ContentType TEXT NOT NULL, SizeBytes INTEGER NOT NULL, Sha256Hash TEXT NOT NULL, UploadedAtUtc TEXT NOT NULL)", cancellationToken);
        await EnsureColumnAsync(connection, "ExternalEvidenceDocuments", "RequirementCode", "TEXT NULL", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS LegalCostRecoveryClaims (Id INTEGER PRIMARY KEY AUTOINCREMENT, CaseId INTEGER NOT NULL, AttorneyId INTEGER NOT NULL, Ground INTEGER NOT NULL, Justification TEXT NOT NULL, OpposingPartyName TEXT NOT NULL DEFAULT '', OpposingPartyEmail TEXT NOT NULL DEFAULT '', ClaimedAmount TEXT NOT NULL, AwardedAmount TEXT NULL, Status INTEGER NOT NULL, SubmittedAtUtc TEXT NOT NULL, DecidedByUserId INTEGER NULL, DecisionNotes TEXT NULL, DecidedAtUtc TEXT NULL, ServedAtUtc TEXT NULL, ServiceDeliveryReference TEXT NULL)", cancellationToken);
        await EnsureColumnAsync(connection, "LegalCostRecoveryClaims", "OpposingPartyName", "TEXT NOT NULL DEFAULT ''", cancellationToken);
        await EnsureColumnAsync(connection, "LegalCostRecoveryClaims", "OpposingPartyEmail", "TEXT NOT NULL DEFAULT ''", cancellationToken);
        await EnsureColumnAsync(connection, "LegalCostRecoveryClaims", "ServiceDeliveryReference", "TEXT NULL", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS LegalCostRecoveryTimeEntries (LegalCostRecoveryClaimId INTEGER NOT NULL, TimeEntryId INTEGER NOT NULL UNIQUE, AmountSnapshot TEXT NOT NULL, PRIMARY KEY (LegalCostRecoveryClaimId, TimeEntryId))", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS LegalCostRecoveryAuditEntries (Id INTEGER PRIMARY KEY AUTOINCREMENT, LegalCostRecoveryClaimId INTEGER NOT NULL, ActorUserId INTEGER NOT NULL, Action TEXT NOT NULL, Details TEXT NOT NULL, RecordedAtUtc TEXT NOT NULL)", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS LitigationStrategyDecisions (Id INTEGER PRIMARY KEY AUTOINCREMENT, CaseId INTEGER NOT NULL, AttorneyId INTEGER NOT NULL, Strategy INTEGER NOT NULL, Reasoning TEXT NOT NULL, LowProspectsJustification TEXT NULL, CostsIncurredSnapshot TEXT NOT NULL, ProjectedCostToCompletion TEXT NOT NULL, MatterValueSnapshot TEXT NOT NULL, ProspectsSnapshot TEXT NOT NULL, ComparableSettlementLow TEXT NULL, ComparableSettlementHigh TEXT NULL, ComparableMatterCount INTEGER NOT NULL, ExpectedDurationDays INTEGER NOT NULL, CostAuthorisationRequired INTEGER NOT NULL, ProspectsAuthorisationRequired INTEGER NOT NULL, Status INTEGER NOT NULL, RecordedAtUtc TEXT NOT NULL, ReviewDueAtUtc TEXT NULL, DirectorId INTEGER NULL, DirectorReason TEXT NULL, DirectorDecidedAtUtc TEXT NULL, SupersededAtUtc TEXT NULL)", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS CaseDocumentRequirements (Id INTEGER PRIMARY KEY AUTOINCREMENT, CaseType TEXT NOT NULL, Code TEXT NOT NULL, Name TEXT NOT NULL, Description TEXT NOT NULL, Category INTEGER NOT NULL, Importance INTEGER NOT NULL, DisplayOrder INTEGER NOT NULL, IsActive INTEGER NOT NULL)", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IF NOT EXISTS IX_CaseDocumentRequirements_CaseType_Code ON CaseDocumentRequirements(CaseType, Code)", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS CaseDocumentWaivers (Id INTEGER PRIMARY KEY AUTOINCREMENT, CaseId INTEGER NOT NULL, RequirementId INTEGER NOT NULL, RequestedByAttorneyId INTEGER NOT NULL, Reason TEXT NOT NULL, Status INTEGER NOT NULL, RequestedAtUtc TEXT NOT NULL, DirectorId INTEGER NULL, DirectorReason TEXT NULL, DecidedAtUtc TEXT NULL)", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS CaseReadinessReviews (Id INTEGER PRIMARY KEY AUTOINCREMENT, CaseId INTEGER NOT NULL, ReviewedByUserId INTEGER NOT NULL, HeldCount INTEGER NOT NULL, MissingMandatoryCount INTEGER NOT NULL, MissingAdvisoryCount INTEGER NOT NULL, CourtReady INTEGER NOT NULL, SnapshotJson TEXT NOT NULL, ReviewedAtUtc TEXT NOT NULL)", cancellationToken);
        await EnsureColumnAsync(connection, "TrustAccounts", "IsFrozen", "INTEGER NOT NULL DEFAULT 0", cancellationToken);
        await EnsureColumnAsync(connection, "TrustAccounts", "IsClosed", "INTEGER NOT NULL DEFAULT 0", cancellationToken);
        await EnsureColumnAsync(connection, "Retainers", "AvailableBalance", "TEXT NOT NULL DEFAULT '0'", cancellationToken);
        await EnsureColumnAsync(connection, "InvoicePenalties", "AppliedByAccountantId", "INTEGER NOT NULL DEFAULT 0", cancellationToken);
        await EnsureColumnAsync(connection, "InvoicePenalties", "Reason", "TEXT NOT NULL DEFAULT ''", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS LegalAuthorities (Id INTEGER PRIMARY KEY AUTOINCREMENT, Citation TEXT NOT NULL, Subject TEXT NOT NULL, Summary TEXT NOT NULL, SearchText TEXT NOT NULL, Rank INTEGER NOT NULL, Treatment INTEGER NOT NULL, IsInternalFallback INTEGER NOT NULL)", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS CaseAuthorityReliances (Id INTEGER PRIMARY KEY AUTOINCREMENT, CaseId INTEGER NOT NULL, LegalAuthorityId INTEGER NOT NULL, AttorneyId INTEGER NOT NULL, RelevanceReason TEXT NOT NULL, AdverseTreatmentConfirmed INTEGER NOT NULL, RecordedAtUtc TEXT NOT NULL)", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS AttorneyWhereabouts (Id INTEGER PRIMARY KEY AUTOINCREMENT, AttorneyId INTEGER NOT NULL, CalendarEventId INTEGER NULL, Venue TEXT NOT NULL, CheckedInAtUtc TEXT NOT NULL, ExpectedReturnAtUtc TEXT NOT NULL, CheckedOutAtUtc TEXT NULL, Status INTEGER NOT NULL, AlertedAtUtc TEXT NULL, DirectorEscalatedAtUtc TEXT NULL, ContactOutcome TEXT NULL)", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS BeneficiaryTrustDisbursementRequests (Id INTEGER PRIMARY KEY AUTOINCREMENT, ReferenceNumber TEXT NOT NULL UNIQUE, BeneficiaryId INTEGER NOT NULL, TrustAccountId INTEGER NOT NULL, Purpose TEXT NOT NULL, Reason TEXT NOT NULL, Amount TEXT NOT NULL, EntitlementLimitSnapshot TEXT NOT NULL, BalanceSnapshot TEXT NOT NULL, Status INTEGER NOT NULL, SubmittedAtUtc TEXT NOT NULL, DecidedByUserId INTEGER NULL, DecisionReason TEXT NULL, DecidedAtUtc TEXT NULL)", cancellationToken);
        await EnsureColumnAsync(connection, "BeneficiaryTrustDisbursementRequests", "DecidedAtUtc", "TEXT NULL", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS CasePriorityWeightConfigurations (Id INTEGER PRIMARY KEY, RiskWeight TEXT NOT NULL, UrgencyWeight TEXT NOT NULL, ComplexityWeight TEXT NOT NULL, DocumentReadinessWeight TEXT NOT NULL, FinancialExposureWeight TEXT NOT NULL, UpdatedAtUtc TEXT NOT NULL, UpdatedByUserId INTEGER NULL)", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS IX_CasePriorityWeightConfigurations_UpdatedByUserId ON CasePriorityWeightConfigurations(UpdatedByUserId)", cancellationToken);
        await EnsureColumnAsync(connection, "CaseDocumentRequests", "RequestType", "INTEGER NOT NULL DEFAULT 1", cancellationToken);
        await EnsureColumnAsync(connection, "CaseDocumentRequests", "ResponseSlaDaysSnapshot", "INTEGER NOT NULL DEFAULT 7", cancellationToken);
        await EnsureColumnAsync(connection, "CaseDocumentRequests", "OverdueAtUtc", "TEXT NULL", cancellationToken);
        await EnsureColumnAsync(connection, "CaseDocumentRequests", "LastDelayActionAtUtc", "TEXT NULL", cancellationToken);
        await EnsureColumnAsync(connection, "CaseDocumentRequests", "LastDelayAction", "INTEGER NULL", cancellationToken);
        await EnsureColumnAsync(connection, "CaseDocumentRequests", "LastDelayActionNote", "TEXT NULL", cancellationToken);
        await EnsureColumnAsync(connection, "CaseDocumentRequests", "ClientResponseText", "TEXT NULL", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS IX_CaseDocumentRequests_Status_DueAtUtc ON CaseDocumentRequests(Status, DueAtUtc)", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS ClientResponseSlaRules (Id INTEGER PRIMARY KEY AUTOINCREMENT, RequestType INTEGER NOT NULL UNIQUE, ResponseDays INTEGER NOT NULL, IsActive INTEGER NOT NULL, UpdatedAtUtc TEXT NOT NULL, UpdatedByUserId INTEGER NULL)", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS AdverseCostExposures (Id INTEGER PRIMARY KEY AUTOINCREMENT, CaseId INTEGER NOT NULL, CourtOrderDocumentId INTEGER NOT NULL, LoggedByAttorneyId INTEGER NOT NULL, IssuingCourt TEXT NOT NULL, RulingDateUtc TEXT NOT NULL, CostOrderAmount TEXT NOT NULL, ExistingCaseCostsSnapshot TEXT NOT NULL, TotalExposure TEXT NOT NULL, AvailableTrustRetainerSnapshot TEXT NOT NULL, Shortfall TEXT NOT NULL, RevisedAmount TEXT NULL, FinalSettledAmount TEXT NULL, Status INTEGER NOT NULL, ProgressNote TEXT NULL, AccountantReviewedByUserId INTEGER NULL, AccountantReviewedAtUtc TEXT NULL, DirectorReviewedByUserId INTEGER NULL, DirectorPosition TEXT NULL, DirectorReviewedAtUtc TEXT NULL, CreatedAtUtc TEXT NOT NULL, ResolvedAtUtc TEXT NULL)", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS IX_AdverseCostExposures_CaseId_Status ON AdverseCostExposures(CaseId, Status)", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS DiscoveryBundles (Id INTEGER PRIMARY KEY AUTOINCREMENT, CaseId INTEGER NOT NULL, AttorneyId INTEGER NOT NULL, Status INTEGER NOT NULL, OpposingAttorneyEmail TEXT NOT NULL, OpposingAttorneyName TEXT NOT NULL, ExchangeToken TEXT NOT NULL UNIQUE, ExchangeExpiresAtUtc TEXT NULL, ExchangedAtUtc TEXT NULL, AccessedAtUtc TEXT NULL, OpposingBundleReceivedAtUtc TEXT NULL, ComplianceNotes TEXT NULL)", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS DiscoveryBundleItems (Id INTEGER PRIMARY KEY AUTOINCREMENT, DiscoveryBundleId INTEGER NOT NULL, DocumentId INTEGER NOT NULL, ProposedPrivileged INTEGER NOT NULL, PrivilegeReason TEXT NULL, PrivilegeDecision INTEGER NOT NULL, DirectorId INTEGER NULL, DirectorReason TEXT NULL, DecidedAtUtc TEXT NULL)", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IF NOT EXISTS IX_DiscoveryBundleItems_Bundle_Document ON DiscoveryBundleItems(DiscoveryBundleId, DocumentId)", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS OpposingDisclosureDocuments (Id INTEGER PRIMARY KEY AUTOINCREMENT, DiscoveryBundleId INTEGER NOT NULL, FileName TEXT NOT NULL, RelativePath TEXT NOT NULL, ContentType TEXT NOT NULL DEFAULT 'application/octet-stream', SizeBytes INTEGER NOT NULL DEFAULT 0, UploadedAtUtc TEXT NOT NULL)", cancellationToken);
        await EnsureColumnAsync(connection, "OpposingDisclosureDocuments", "ContentType", "TEXT NOT NULL DEFAULT 'application/octet-stream'", cancellationToken);
        await EnsureColumnAsync(connection, "OpposingDisclosureDocuments", "SizeBytes", "INTEGER NOT NULL DEFAULT 0", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS OverduePaymentPenaltyPolicies (Id INTEGER PRIMARY KEY AUTOINCREMENT, GracePeriodDays INTEGER NOT NULL, PenaltyType TEXT NOT NULL, PenaltyValue TEXT NOT NULL, IsActive INTEGER NOT NULL, UpdatedAtUtc TEXT NOT NULL, UpdatedByUserId INTEGER NULL)", cancellationToken);
        await EnsureNullableInvoicePenaltyCalendarEventAsync(connection, cancellationToken);
        if (!wasOpen) await context.Database.CloseConnectionAsync();
    }

    private static async Task EnsureColumnAsync(System.Data.Common.DbConnection connection, string table, string column, string definition, CancellationToken ct)
    {
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using (var command = connection.CreateCommand()) { command.CommandText = $"PRAGMA table_info(\"{table}\")"; await using var reader = await command.ExecuteReaderAsync(ct); while (await reader.ReadAsync(ct)) columns.Add(reader.GetString(reader.GetOrdinal("name"))); }
        if (columns.Count == 0 || columns.Contains(column)) return;
        await using var alter = connection.CreateCommand(); alter.CommandText = $"ALTER TABLE \"{table}\" ADD COLUMN \"{column}\" {definition}"; await alter.ExecuteNonQueryAsync(ct);
    }

    private static async Task EnsureNullableInvoicePenaltyCalendarEventAsync(System.Data.Common.DbConnection connection, CancellationToken ct)
    {
        var exists = false; var required = false;
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA table_info(\"InvoicePenalties\")";
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct)) { exists = true; if (reader.GetString(reader.GetOrdinal("name")) == "CalendarEventId") required = reader.GetInt32(reader.GetOrdinal("notnull")) != 0; }
        }
        if (!exists || !required) return;
        await using var transaction = await connection.BeginTransactionAsync(ct);
        foreach (var sql in new[]
        {
            "ALTER TABLE InvoicePenalties RENAME TO InvoicePenalties_Legacy",
            "CREATE TABLE InvoicePenalties (Id INTEGER PRIMARY KEY AUTOINCREMENT, InvoiceId INTEGER NOT NULL, CalendarEventId INTEGER NULL, Type TEXT NOT NULL, BasisAmount TEXT NOT NULL, PenaltyValue TEXT NOT NULL, Amount TEXT NOT NULL, AppliedAtUtc TEXT NOT NULL, IdempotencyKey TEXT NOT NULL, AppliedByAccountantId INTEGER NOT NULL, Reason TEXT NOT NULL)",
            "INSERT INTO InvoicePenalties (Id,InvoiceId,CalendarEventId,Type,BasisAmount,PenaltyValue,Amount,AppliedAtUtc,IdempotencyKey,AppliedByAccountantId,Reason) SELECT Id,InvoiceId,CalendarEventId,Type,BasisAmount,PenaltyValue,Amount,AppliedAtUtc,IdempotencyKey,AppliedByAccountantId,Reason FROM InvoicePenalties_Legacy",
            "DROP TABLE InvoicePenalties_Legacy",
            "CREATE UNIQUE INDEX IF NOT EXISTS IX_InvoicePenalties_IdempotencyKey ON InvoicePenalties(IdempotencyKey)"
        }) { await using var command = connection.CreateCommand(); command.Transaction = transaction; command.CommandText = sql; await command.ExecuteNonQueryAsync(ct); }
        await transaction.CommitAsync(ct);
    }
}
