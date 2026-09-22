using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplexLawFirm.Migrations
{
    /// <inheritdoc />
    public partial class AddClientRelationshipRisk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CaseWitnesses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseId = table.Column<int>(type: "int", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseWitnesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseWitnesses_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClientRelationshipRisks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    FinancialScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LegalScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReputationScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PredictedRelationshipScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OverallScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    HighRiskAlert = table.Column<bool>(type: "bit", nullable: false),
                    DirectorAction = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DirectorNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ReviewedByUserId = table.Column<int>(type: "int", nullable: true),
                    CalculatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientRelationshipRisks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientRelationshipRisks_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CourtFilingSubmissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseId = table.Column<int>(type: "int", nullable: false),
                    SubmittedByAttorneyId = table.Column<int>(type: "int", nullable: false),
                    DocumentId = table.Column<int>(type: "int", nullable: true),
                    DocumentType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    SubmittedCaseNumber = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SignedByAttorneyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SignatureDetected = table.Column<bool>(type: "bit", nullable: false),
                    AttorneyOfRecordId = table.Column<int>(type: "int", nullable: true),
                    ExpectedDocumentType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ValidationFailuresJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReturnReason = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FiledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReturnedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CourtReference = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    FilingNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TimelineEntry = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourtFilingSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourtFilingSubmissions_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourtFilingSubmissions_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CourtFilingSubmissions_Users_AttorneyOfRecordId",
                        column: x => x.AttorneyOfRecordId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CourtFilingSubmissions_Users_SubmittedByAttorneyId",
                        column: x => x.SubmittedByAttorneyId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourtFilingSubmissions_Users_ValidatedByUserId",
                        column: x => x.ValidatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CourtProcedureSteps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourtProcedureSteps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SettlementRecommendations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseId = table.Column<int>(type: "int", nullable: false),
                    TriggerType = table.Column<int>(type: "int", nullable: false),
                    ProjectedLitigationCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LikelihoodOfSuccess = table.Column<decimal>(type: "decimal(6,5)", precision: 6, scale: 5, nullable: false),
                    MatterValueSnapshot = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ExpectedLitigationValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AverageComparableSettlement = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ExpectedSettlementValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ComparableMatterCount = table.Column<int>(type: "int", nullable: false),
                    ComparableMattersJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SettlementRecommended = table.Column<bool>(type: "bit", nullable: false),
                    Rationale = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    SuggestedSettlementLow = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    SuggestedSettlementHigh = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    AttorneyId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AttorneyResponse = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    AttorneyRespondedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DirectorId = table.Column<int>(type: "int", nullable: true),
                    DirectorResponse = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    DirectorDecidedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SupersededAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SettlementRecommendations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SettlementRecommendations_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SettlementRecommendations_Users_AttorneyId",
                        column: x => x.AttorneyId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SettlementRecommendations_Users_DirectorId",
                        column: x => x.DirectorId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WitnessCommitments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseWitnessId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    StartDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WitnessCommitments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WitnessCommitments_CaseWitnesses_CaseWitnessId",
                        column: x => x.CaseWitnessId,
                        principalTable: "CaseWitnesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WitnessScheduleConflicts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseId = table.Column<int>(type: "int", nullable: false),
                    CaseWitnessId = table.Column<int>(type: "int", nullable: false),
                    CourtEventId = table.Column<int>(type: "int", nullable: false),
                    WitnessCommitmentId = table.Column<int>(type: "int", nullable: true),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CourtDateStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CourtDateEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConflictingEventSummary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DetectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ResolvedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WitnessScheduleConflicts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WitnessScheduleConflicts_CaseWitnesses_CaseWitnessId",
                        column: x => x.CaseWitnessId,
                        principalTable: "CaseWitnesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WitnessScheduleConflicts_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WitnessScheduleConflicts_WitnessCommitments_WitnessCommitmentId",
                        column: x => x.WitnessCommitmentId,
                        principalTable: "WitnessCommitments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CaseWitnesses_CaseId_IsActive",
                table: "CaseWitnesses",
                columns: new[] { "CaseId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ClientRelationshipRisks_ClientId",
                table: "ClientRelationshipRisks",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_CourtFilingSubmissions_AttorneyOfRecordId",
                table: "CourtFilingSubmissions",
                column: "AttorneyOfRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_CourtFilingSubmissions_CaseId",
                table: "CourtFilingSubmissions",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourtFilingSubmissions_DocumentId",
                table: "CourtFilingSubmissions",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_CourtFilingSubmissions_SubmittedByAttorneyId",
                table: "CourtFilingSubmissions",
                column: "SubmittedByAttorneyId");

            migrationBuilder.CreateIndex(
                name: "IX_CourtFilingSubmissions_ValidatedByUserId",
                table: "CourtFilingSubmissions",
                column: "ValidatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementRecommendations_AttorneyId",
                table: "SettlementRecommendations",
                column: "AttorneyId");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementRecommendations_CaseId_Status",
                table: "SettlementRecommendations",
                columns: new[] { "CaseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SettlementRecommendations_DirectorId",
                table: "SettlementRecommendations",
                column: "DirectorId");

            migrationBuilder.CreateIndex(
                name: "IX_WitnessCommitments_CaseWitnessId",
                table: "WitnessCommitments",
                column: "CaseWitnessId");

            migrationBuilder.CreateIndex(
                name: "IX_WitnessScheduleConflicts_CaseId_Status",
                table: "WitnessScheduleConflicts",
                columns: new[] { "CaseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_WitnessScheduleConflicts_CaseWitnessId",
                table: "WitnessScheduleConflicts",
                column: "CaseWitnessId");

            migrationBuilder.CreateIndex(
                name: "IX_WitnessScheduleConflicts_WitnessCommitmentId",
                table: "WitnessScheduleConflicts",
                column: "WitnessCommitmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientRelationshipRisks");

            migrationBuilder.DropTable(
                name: "CourtFilingSubmissions");

            migrationBuilder.DropTable(
                name: "CourtProcedureSteps");

            migrationBuilder.DropTable(
                name: "SettlementRecommendations");

            migrationBuilder.DropTable(
                name: "WitnessScheduleConflicts");

            migrationBuilder.DropTable(
                name: "WitnessCommitments");

            migrationBuilder.DropTable(
                name: "CaseWitnesses");
        }
    }
}
