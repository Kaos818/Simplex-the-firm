using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplexLawFirm.Migrations
{
    /// <inheritdoc />
    public partial class SyncProductionModel_20260808 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsClosed",
                table: "TrustAccounts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsFrozen",
                table: "TrustAccounts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "AvailableBalance",
                table: "Retainers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "AppliedByAccountantId",
                table: "InvoicePenalties",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "InvoicePenalties",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RequirementCode",
                table: "Documents",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CourtReadyAtUtc",
                table: "Cases",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCourtReady",
                table: "Cases",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MatterValue",
                table: "Cases",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SettlementAmount",
                table: "Cases",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "StrategyReviewRequired",
                table: "Cases",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LawyerApprovalStatus",
                table: "CalendarEvents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "BankAccountHolder",
                table: "Beneficiaries",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankAccountNumber",
                table: "Beneficiaries",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankBranchCode",
                table: "Beneficiaries",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BankDetailsConfirmedAtUtc",
                table: "Beneficiaries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "Beneficiaries",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EntitlementAmountLimit",
                table: "Beneficiaries",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AttorneyWhereabouts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttorneyId = table.Column<int>(type: "int", nullable: false),
                    CalendarEventId = table.Column<int>(type: "int", nullable: true),
                    Venue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CheckedInAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpectedReturnAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CheckedOutAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AlertedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DirectorEscalatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ContactOutcome = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttorneyWhereabouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttorneyWhereabouts_CalendarEvents_CalendarEventId",
                        column: x => x.CalendarEventId,
                        principalTable: "CalendarEvents",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AttorneyWhereabouts_Users_AttorneyId",
                        column: x => x.AttorneyId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BeneficiaryTrustDisbursementRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BeneficiaryId = table.Column<int>(type: "int", nullable: false),
                    TrustAccountId = table.Column<int>(type: "int", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EntitlementLimitSnapshot = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BalanceSnapshot = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DecidedByUserId = table.Column<int>(type: "int", nullable: true),
                    DecisionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DecidedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BeneficiaryTrustDisbursementRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BeneficiaryTrustDisbursementRequests_Beneficiaries_BeneficiaryId",
                        column: x => x.BeneficiaryId,
                        principalTable: "Beneficiaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BeneficiaryTrustDisbursementRequests_TrustAccounts_TrustAccountId",
                        column: x => x.TrustAccountId,
                        principalTable: "TrustAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CaseDocumentRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Importance = table.Column<int>(type: "int", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseDocumentRequirements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CaseReadinessReviews",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseId = table.Column<int>(type: "int", nullable: false),
                    ReviewedByUserId = table.Column<int>(type: "int", nullable: false),
                    HeldCount = table.Column<int>(type: "int", nullable: false),
                    MissingMandatoryCount = table.Column<int>(type: "int", nullable: false),
                    MissingAdvisoryCount = table.Column<int>(type: "int", nullable: false),
                    CourtReady = table.Column<bool>(type: "bit", nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseReadinessReviews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExternalEvidenceRequests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseId = table.Column<int>(type: "int", nullable: false),
                    RecipientEmail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    RecipientName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AccessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequestedByUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalEvidenceRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalEvidenceRequests_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LegalAuthorities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Citation = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SearchText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rank = table.Column<int>(type: "int", nullable: false),
                    Treatment = table.Column<int>(type: "int", nullable: false),
                    IsInternalFallback = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalAuthorities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LegalCostRecoveryClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseId = table.Column<int>(type: "int", nullable: false),
                    AttorneyId = table.Column<int>(type: "int", nullable: false),
                    Ground = table.Column<int>(type: "int", nullable: false),
                    Justification = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    OpposingPartyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OpposingPartyEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    ClaimedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AwardedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DecidedByUserId = table.Column<int>(type: "int", nullable: true),
                    DecisionNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    DecidedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ServedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ServiceDeliveryReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalCostRecoveryClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LegalCostRecoveryClaims_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LegalCostRecoveryClaims_Users_AttorneyId",
                        column: x => x.AttorneyId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LegalCostRecoveryClaims_Users_DecidedByUserId",
                        column: x => x.DecidedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LitigationStrategyDecisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseId = table.Column<int>(type: "int", nullable: false),
                    AttorneyId = table.Column<int>(type: "int", nullable: false),
                    Strategy = table.Column<int>(type: "int", nullable: false),
                    Reasoning = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    LowProspectsJustification = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CostsIncurredSnapshot = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ProjectedCostToCompletion = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MatterValueSnapshot = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ProspectsSnapshot = table.Column<decimal>(type: "decimal(6,5)", precision: 6, scale: 5, nullable: false),
                    ComparableSettlementLow = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ComparableSettlementHigh = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ComparableMatterCount = table.Column<int>(type: "int", nullable: false),
                    ExpectedDurationDays = table.Column<int>(type: "int", nullable: false),
                    CostAuthorisationRequired = table.Column<bool>(type: "bit", nullable: false),
                    ProspectsAuthorisationRequired = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewDueAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DirectorId = table.Column<int>(type: "int", nullable: true),
                    DirectorReason = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    DirectorDecidedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SupersededAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LitigationStrategyDecisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LitigationStrategyDecisions_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LitigationStrategyDecisions_Users_AttorneyId",
                        column: x => x.AttorneyId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LitigationStrategyDecisions_Users_DirectorId",
                        column: x => x.DirectorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CaseDocumentWaivers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseId = table.Column<int>(type: "int", nullable: false),
                    RequirementId = table.Column<int>(type: "int", nullable: false),
                    RequestedByAttorneyId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DirectorId = table.Column<int>(type: "int", nullable: true),
                    DirectorReason = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: true),
                    DecidedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseDocumentWaivers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseDocumentWaivers_CaseDocumentRequirements_RequirementId",
                        column: x => x.RequirementId,
                        principalTable: "CaseDocumentRequirements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaseDocumentWaivers_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExternalEvidenceDocuments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<long>(type: "bigint", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(2500)", maxLength: 2500, nullable: false),
                    RequirementCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    RelativePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256Hash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploadedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalEvidenceDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalEvidenceDocuments_ExternalEvidenceRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "ExternalEvidenceRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CaseAuthorityReliances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseId = table.Column<int>(type: "int", nullable: false),
                    LegalAuthorityId = table.Column<int>(type: "int", nullable: false),
                    AttorneyId = table.Column<int>(type: "int", nullable: false),
                    RelevanceReason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AdverseTreatmentConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseAuthorityReliances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseAuthorityReliances_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CaseAuthorityReliances_LegalAuthorities_LegalAuthorityId",
                        column: x => x.LegalAuthorityId,
                        principalTable: "LegalAuthorities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CaseAuthorityReliances_Users_AttorneyId",
                        column: x => x.AttorneyId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LegalCostRecoveryAuditEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LegalCostRecoveryClaimId = table.Column<int>(type: "int", nullable: false),
                    ActorUserId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalCostRecoveryAuditEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LegalCostRecoveryAuditEntries_LegalCostRecoveryClaims_LegalCostRecoveryClaimId",
                        column: x => x.LegalCostRecoveryClaimId,
                        principalTable: "LegalCostRecoveryClaims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LegalCostRecoveryTimeEntries",
                columns: table => new
                {
                    LegalCostRecoveryClaimId = table.Column<int>(type: "int", nullable: false),
                    TimeEntryId = table.Column<int>(type: "int", nullable: false),
                    AmountSnapshot = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalCostRecoveryTimeEntries", x => new { x.LegalCostRecoveryClaimId, x.TimeEntryId });
                    table.ForeignKey(
                        name: "FK_LegalCostRecoveryTimeEntries_LegalCostRecoveryClaims_LegalCostRecoveryClaimId",
                        column: x => x.LegalCostRecoveryClaimId,
                        principalTable: "LegalCostRecoveryClaims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LegalCostRecoveryTimeEntries_TimeEntries_TimeEntryId",
                        column: x => x.TimeEntryId,
                        principalTable: "TimeEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvoicePenalties_AppliedByAccountantId",
                table: "InvoicePenalties",
                column: "AppliedByAccountantId");

            migrationBuilder.CreateIndex(
                name: "IX_AttorneyWhereabouts_AttorneyId",
                table: "AttorneyWhereabouts",
                column: "AttorneyId");

            migrationBuilder.CreateIndex(
                name: "IX_AttorneyWhereabouts_CalendarEventId",
                table: "AttorneyWhereabouts",
                column: "CalendarEventId");

            migrationBuilder.CreateIndex(
                name: "IX_AttorneyWhereabouts_Status_ExpectedReturnAtUtc",
                table: "AttorneyWhereabouts",
                columns: new[] { "Status", "ExpectedReturnAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BeneficiaryTrustDisbursementRequests_BeneficiaryId",
                table: "BeneficiaryTrustDisbursementRequests",
                column: "BeneficiaryId");

            migrationBuilder.CreateIndex(
                name: "IX_BeneficiaryTrustDisbursementRequests_ReferenceNumber",
                table: "BeneficiaryTrustDisbursementRequests",
                column: "ReferenceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BeneficiaryTrustDisbursementRequests_TrustAccountId",
                table: "BeneficiaryTrustDisbursementRequests",
                column: "TrustAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseAuthorityReliances_AttorneyId",
                table: "CaseAuthorityReliances",
                column: "AttorneyId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseAuthorityReliances_CaseId_LegalAuthorityId_AttorneyId",
                table: "CaseAuthorityReliances",
                columns: new[] { "CaseId", "LegalAuthorityId", "AttorneyId" });

            migrationBuilder.CreateIndex(
                name: "IX_CaseAuthorityReliances_LegalAuthorityId",
                table: "CaseAuthorityReliances",
                column: "LegalAuthorityId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseDocumentRequirements_CaseType_Code",
                table: "CaseDocumentRequirements",
                columns: new[] { "CaseType", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CaseDocumentWaivers_CaseId_RequirementId_Status",
                table: "CaseDocumentWaivers",
                columns: new[] { "CaseId", "RequirementId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CaseDocumentWaivers_RequirementId",
                table: "CaseDocumentWaivers",
                column: "RequirementId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseReadinessReviews_CaseId_ReviewedAtUtc",
                table: "CaseReadinessReviews",
                columns: new[] { "CaseId", "ReviewedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalEvidenceDocuments_RequestId",
                table: "ExternalEvidenceDocuments",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalEvidenceRequests_CaseId",
                table: "ExternalEvidenceRequests",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_LegalCostRecoveryAuditEntries_LegalCostRecoveryClaimId_RecordedAtUtc",
                table: "LegalCostRecoveryAuditEntries",
                columns: new[] { "LegalCostRecoveryClaimId", "RecordedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LegalCostRecoveryClaims_AttorneyId",
                table: "LegalCostRecoveryClaims",
                column: "AttorneyId");

            migrationBuilder.CreateIndex(
                name: "IX_LegalCostRecoveryClaims_CaseId",
                table: "LegalCostRecoveryClaims",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_LegalCostRecoveryClaims_DecidedByUserId",
                table: "LegalCostRecoveryClaims",
                column: "DecidedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LegalCostRecoveryClaims_Status_SubmittedAtUtc",
                table: "LegalCostRecoveryClaims",
                columns: new[] { "Status", "SubmittedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LegalCostRecoveryTimeEntries_TimeEntryId",
                table: "LegalCostRecoveryTimeEntries",
                column: "TimeEntryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LitigationStrategyDecisions_AttorneyId",
                table: "LitigationStrategyDecisions",
                column: "AttorneyId");

            migrationBuilder.CreateIndex(
                name: "IX_LitigationStrategyDecisions_CaseId_Status",
                table: "LitigationStrategyDecisions",
                columns: new[] { "CaseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_LitigationStrategyDecisions_DirectorId",
                table: "LitigationStrategyDecisions",
                column: "DirectorId");

            migrationBuilder.AddForeignKey(
                name: "FK_InvoicePenalties_Users_AppliedByAccountantId",
                table: "InvoicePenalties",
                column: "AppliedByAccountantId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvoicePenalties_Users_AppliedByAccountantId",
                table: "InvoicePenalties");

            migrationBuilder.DropTable(
                name: "AttorneyWhereabouts");

            migrationBuilder.DropTable(
                name: "BeneficiaryTrustDisbursementRequests");

            migrationBuilder.DropTable(
                name: "CaseAuthorityReliances");

            migrationBuilder.DropTable(
                name: "CaseDocumentWaivers");

            migrationBuilder.DropTable(
                name: "CaseReadinessReviews");

            migrationBuilder.DropTable(
                name: "ExternalEvidenceDocuments");

            migrationBuilder.DropTable(
                name: "LegalCostRecoveryAuditEntries");

            migrationBuilder.DropTable(
                name: "LegalCostRecoveryTimeEntries");

            migrationBuilder.DropTable(
                name: "LitigationStrategyDecisions");

            migrationBuilder.DropTable(
                name: "LegalAuthorities");

            migrationBuilder.DropTable(
                name: "CaseDocumentRequirements");

            migrationBuilder.DropTable(
                name: "ExternalEvidenceRequests");

            migrationBuilder.DropTable(
                name: "LegalCostRecoveryClaims");

            migrationBuilder.DropIndex(
                name: "IX_InvoicePenalties_AppliedByAccountantId",
                table: "InvoicePenalties");

            migrationBuilder.DropColumn(
                name: "IsClosed",
                table: "TrustAccounts");

            migrationBuilder.DropColumn(
                name: "IsFrozen",
                table: "TrustAccounts");

            migrationBuilder.DropColumn(
                name: "AvailableBalance",
                table: "Retainers");

            migrationBuilder.DropColumn(
                name: "AppliedByAccountantId",
                table: "InvoicePenalties");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "InvoicePenalties");

            migrationBuilder.DropColumn(
                name: "RequirementCode",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "CourtReadyAtUtc",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "IsCourtReady",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "MatterValue",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "SettlementAmount",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "StrategyReviewRequired",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "LawyerApprovalStatus",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "BankAccountHolder",
                table: "Beneficiaries");

            migrationBuilder.DropColumn(
                name: "BankAccountNumber",
                table: "Beneficiaries");

            migrationBuilder.DropColumn(
                name: "BankBranchCode",
                table: "Beneficiaries");

            migrationBuilder.DropColumn(
                name: "BankDetailsConfirmedAtUtc",
                table: "Beneficiaries");

            migrationBuilder.DropColumn(
                name: "BankName",
                table: "Beneficiaries");

            migrationBuilder.DropColumn(
                name: "EntitlementAmountLimit",
                table: "Beneficiaries");
        }
    }
}
