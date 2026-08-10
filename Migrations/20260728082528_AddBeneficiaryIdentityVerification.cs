using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplexLawFirm.Migrations
{
    /// <inheritdoc />
    public partial class AddBeneficiaryIdentityVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AppointmentFee",
                table: "CalendarEvents",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "BillingProcessed",
                table: "CalendarEvents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "BillingProcessedAtUtc",
                table: "CalendarEvents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClientId",
                table: "CalendarEvents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClientRespondedAtUtc",
                table: "CalendarEvents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientResponseComments",
                table: "CalendarEvents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientResponseStatus",
                table: "CalendarEvents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "GeneratedInvoiceId",
                table: "CalendarEvents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LatePenaltyGraceDays",
                table: "CalendarEvents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LatePenaltyType",
                table: "CalendarEvents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "LatePenaltyValue",
                table: "CalendarEvents",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PaymentDueDays",
                table: "CalendarEvents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "CalendarEvents",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateTable(
                name: "AppointmentBillingRecords",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CalendarEventId = table.Column<int>(type: "int", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AppointmentFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CoveredAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    InvoicedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TrustTransactionId = table.Column<int>(type: "int", nullable: true),
                    InvoiceId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentBillingRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppointmentBillingRecords_CalendarEvents_CalendarEventId",
                        column: x => x.CalendarEventId,
                        principalTable: "CalendarEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppointmentInvitations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CalendarEventId = table.Column<int>(type: "int", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppointmentInvitations_CalendarEvents_CalendarEventId",
                        column: x => x.CalendarEventId,
                        principalTable: "CalendarEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuditEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActorUserId = table.Column<int>(type: "int", nullable: true),
                    EntityType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SafeMetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Beneficiaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BenefactorClientId = table.Column<int>(type: "int", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IdentificationNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RelationshipToBenefactor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ManualReviewReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByUserId = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Beneficiaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Beneficiaries_Clients_BenefactorClientId",
                        column: x => x.BenefactorClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Beneficiaries_Users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BeneficiaryDocumentRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    RequiresCertifiedCopy = table.Column<bool>(type: "bit", nullable: false),
                    RequiresExpiryCheck = table.Column<bool>(type: "bit", nullable: false),
                    MaximumAgeDays = table.Column<int>(type: "int", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BeneficiaryDocumentRequirements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BiometricConsentRecords",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VerificationSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BeneficiaryId = table.Column<int>(type: "int", nullable: false),
                    NoticeVersion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NoticeTextHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConsentGranted = table.Column<bool>(type: "bit", nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddressHash = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BiometricConsentRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailOutboxMessages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ToAddress = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    HtmlBody = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TextBody = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NextAttemptAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DeduplicationKey = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailOutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InvoicePenalties",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    CalendarEventId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BasisAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PenaltyValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AppliedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoicePenalties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoicePenalties_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SystemNotifications",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActionUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReadAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeduplicationKey = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemNotifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BeneficiaryInvitations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BeneficiaryId = table.Column<int>(type: "int", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BeneficiaryInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BeneficiaryInvitations_Beneficiaries_BeneficiaryId",
                        column: x => x.BeneficiaryId,
                        principalTable: "Beneficiaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FacialVerificationSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BeneficiaryId = table.Column<int>(type: "int", nullable: false),
                    ChallengeJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ConsentGranted = table.Column<bool>(type: "bit", nullable: false),
                    ConsentGrantedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConsentNoticeVersion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LivenessPassed = table.Column<bool>(type: "bit", nullable: true),
                    FaceMatched = table.Column<bool>(type: "bit", nullable: true),
                    SimilarityScore = table.Column<decimal>(type: "decimal(6,5)", precision: 6, scale: 5, nullable: true),
                    ValidFrameRatio = table.Column<decimal>(type: "decimal(6,5)", precision: 6, scale: 5, nullable: true),
                    DuplicateFrameRatio = table.Column<decimal>(type: "decimal(6,5)", precision: 6, scale: 5, nullable: true),
                    ResultReasonCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResultReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacialVerificationSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FacialVerificationSessions_Beneficiaries_BeneficiaryId",
                        column: x => x.BeneficiaryId,
                        principalTable: "Beneficiaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BeneficiaryDocuments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BeneficiaryId = table.Column<int>(type: "int", nullable: false),
                    RequirementId = table.Column<int>(type: "int", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StoredFileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RelativeStoragePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256Hash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PreScreenStatus = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    QualityScore = table.Column<decimal>(type: "decimal(6,5)", precision: 6, scale: 5, nullable: true),
                    OcrConfidence = table.Column<decimal>(type: "decimal(6,5)", precision: 6, scale: 5, nullable: true),
                    CertificationWordingDetected = table.Column<bool>(type: "bit", nullable: true),
                    CertificationStampDetected = table.Column<bool>(type: "bit", nullable: true),
                    SignatureDetected = table.Column<bool>(type: "bit", nullable: true),
                    DetectedCertificationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DetectedExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExtractedDocumentType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReasonCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserFacingReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TechnicalResultJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UploadedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AnalysedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BeneficiaryDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BeneficiaryDocuments_Beneficiaries_BeneficiaryId",
                        column: x => x.BeneficiaryId,
                        principalTable: "Beneficiaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BeneficiaryDocuments_BeneficiaryDocumentRequirements_RequirementId",
                        column: x => x.RequirementId,
                        principalTable: "BeneficiaryDocumentRequirements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BeneficiaryRequirementAssignments",
                columns: table => new
                {
                    BeneficiaryId = table.Column<int>(type: "int", nullable: false),
                    RequirementId = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BeneficiaryRequirementAssignments", x => new { x.BeneficiaryId, x.RequirementId });
                    table.ForeignKey(
                        name: "FK_BeneficiaryRequirementAssignments_Beneficiaries_BeneficiaryId",
                        column: x => x.BeneficiaryId,
                        principalTable: "Beneficiaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BeneficiaryRequirementAssignments_BeneficiaryDocumentRequirements_RequirementId",
                        column: x => x.RequirementId,
                        principalTable: "BeneficiaryDocumentRequirements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_ClientId",
                table: "CalendarEvents",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_GeneratedInvoiceId",
                table: "CalendarEvents",
                column: "GeneratedInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentBillingRecords_CalendarEventId",
                table: "AppointmentBillingRecords",
                column: "CalendarEventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentBillingRecords_IdempotencyKey",
                table: "AppointmentBillingRecords",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentInvitations_CalendarEventId",
                table: "AppointmentInvitations",
                column: "CalendarEventId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentInvitations_ExpiresAtUtc",
                table: "AppointmentInvitations",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentInvitations_TokenHash",
                table: "AppointmentInvitations",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_CreatedAtUtc",
                table: "AuditEntries",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_EntityType_EntityId",
                table: "AuditEntries",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_Beneficiaries_BenefactorClientId",
                table: "Beneficiaries",
                column: "BenefactorClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Beneficiaries_Email",
                table: "Beneficiaries",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Beneficiaries_ReviewedByUserId",
                table: "Beneficiaries",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Beneficiaries_Status",
                table: "Beneficiaries",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BeneficiaryDocumentRequirements_Code",
                table: "BeneficiaryDocumentRequirements",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BeneficiaryDocuments_BeneficiaryId",
                table: "BeneficiaryDocuments",
                column: "BeneficiaryId");

            migrationBuilder.CreateIndex(
                name: "IX_BeneficiaryDocuments_PreScreenStatus",
                table: "BeneficiaryDocuments",
                column: "PreScreenStatus");

            migrationBuilder.CreateIndex(
                name: "IX_BeneficiaryDocuments_RequirementId",
                table: "BeneficiaryDocuments",
                column: "RequirementId");

            migrationBuilder.CreateIndex(
                name: "IX_BeneficiaryInvitations_BeneficiaryId",
                table: "BeneficiaryInvitations",
                column: "BeneficiaryId");

            migrationBuilder.CreateIndex(
                name: "IX_BeneficiaryInvitations_ExpiresAtUtc",
                table: "BeneficiaryInvitations",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_BeneficiaryInvitations_TokenHash",
                table: "BeneficiaryInvitations",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BeneficiaryRequirementAssignments_RequirementId",
                table: "BeneficiaryRequirementAssignments",
                column: "RequirementId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailOutboxMessages_DeduplicationKey",
                table: "EmailOutboxMessages",
                column: "DeduplicationKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailOutboxMessages_Status_NextAttemptAtUtc",
                table: "EmailOutboxMessages",
                columns: new[] { "Status", "NextAttemptAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_FacialVerificationSessions_BeneficiaryId",
                table: "FacialVerificationSessions",
                column: "BeneficiaryId");

            migrationBuilder.CreateIndex(
                name: "IX_FacialVerificationSessions_ExpiresAtUtc",
                table: "FacialVerificationSessions",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_FacialVerificationSessions_Status",
                table: "FacialVerificationSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_InvoicePenalties_IdempotencyKey",
                table: "InvoicePenalties",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoicePenalties_InvoiceId",
                table: "InvoicePenalties",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemNotifications_DeduplicationKey",
                table: "SystemNotifications",
                column: "DeduplicationKey",
                unique: true,
                filter: "[DeduplicationKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SystemNotifications_UserId",
                table: "SystemNotifications",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CalendarEvents_Clients_ClientId",
                table: "CalendarEvents",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CalendarEvents_Invoices_GeneratedInvoiceId",
                table: "CalendarEvents",
                column: "GeneratedInvoiceId",
                principalTable: "Invoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CalendarEvents_Clients_ClientId",
                table: "CalendarEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_CalendarEvents_Invoices_GeneratedInvoiceId",
                table: "CalendarEvents");

            migrationBuilder.DropTable(
                name: "AppointmentBillingRecords");

            migrationBuilder.DropTable(
                name: "AppointmentInvitations");

            migrationBuilder.DropTable(
                name: "AuditEntries");

            migrationBuilder.DropTable(
                name: "BeneficiaryDocuments");

            migrationBuilder.DropTable(
                name: "BeneficiaryInvitations");

            migrationBuilder.DropTable(
                name: "BeneficiaryRequirementAssignments");

            migrationBuilder.DropTable(
                name: "BiometricConsentRecords");

            migrationBuilder.DropTable(
                name: "EmailOutboxMessages");

            migrationBuilder.DropTable(
                name: "FacialVerificationSessions");

            migrationBuilder.DropTable(
                name: "InvoicePenalties");

            migrationBuilder.DropTable(
                name: "SystemNotifications");

            migrationBuilder.DropTable(
                name: "BeneficiaryDocumentRequirements");

            migrationBuilder.DropTable(
                name: "Beneficiaries");

            migrationBuilder.DropIndex(
                name: "IX_CalendarEvents_ClientId",
                table: "CalendarEvents");

            migrationBuilder.DropIndex(
                name: "IX_CalendarEvents_GeneratedInvoiceId",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "AppointmentFee",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "BillingProcessed",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "BillingProcessedAtUtc",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "ClientRespondedAtUtc",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "ClientResponseComments",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "ClientResponseStatus",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "GeneratedInvoiceId",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "LatePenaltyGraceDays",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "LatePenaltyType",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "LatePenaltyValue",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "PaymentDueDays",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "CalendarEvents");
        }
    }
}
