using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplexLawFirm.Migrations
{
    /// <inheritdoc />
    public partial class PracticeIntelligenceUseCases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CaseType",
                table: "Cases",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "EvidenceStrength",
                table: "Cases",
                type: "decimal(6,5)",
                precision: 6,
                scale: 5,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "RecordedOutcome",
                table: "Cases",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CaseForecasts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseId = table.Column<int>(type: "int", nullable: false),
                    AttorneyId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Probability = table.Column<decimal>(type: "decimal(6,5)", precision: 6, scale: 5, nullable: true),
                    ProbabilityBand = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConfidenceLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ComparableCount = table.Column<int>(type: "int", nullable: false),
                    FactorsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ComparableCasesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RefusalReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AttorneyAssessment = table.Column<decimal>(type: "decimal(6,5)", precision: 6, scale: 5, nullable: true),
                    AttorneyAgrees = table.Column<bool>(type: "bit", nullable: true),
                    AttorneyNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LockedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualOutcome = table.Column<int>(type: "int", nullable: true),
                    AccuracyScore = table.Column<decimal>(type: "decimal(6,5)", precision: 6, scale: 5, nullable: true),
                    ScoredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseForecasts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseForecasts_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaseForecasts_Users_AttorneyId",
                        column: x => x.AttorneyId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CaseHandovers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseId = table.Column<int>(type: "int", nullable: false),
                    OutgoingAttorneyId = table.Column<int>(type: "int", nullable: false),
                    ReceivingAttorneyId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DueAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UrgentMatters = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReadyAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcceptedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseHandovers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseHandovers_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaseHandovers_Users_OutgoingAttorneyId",
                        column: x => x.OutgoingAttorneyId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaseHandovers_Users_ReceivingAttorneyId",
                        column: x => x.ReceivingAttorneyId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceComplaints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CaseId = table.Column<int>(type: "int", nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RoutedToUserId = table.Column<int>(type: "int", nullable: false),
                    RestrictedUserIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResponseDueAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcknowledgedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DuplicateWarningAcknowledged = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceComplaints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceComplaints_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceComplaints_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceComplaints_Users_RoutedToUserId",
                        column: x => x.RoutedToUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HandoverItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseHandoverId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    ResolutionNote = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HandoverItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HandoverItems_CaseHandovers_CaseHandoverId",
                        column: x => x.CaseHandoverId,
                        principalTable: "CaseHandovers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CaseForecasts_AttorneyId",
                table: "CaseForecasts",
                column: "AttorneyId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseForecasts_CaseId",
                table: "CaseForecasts",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseHandovers_CaseId_Status",
                table: "CaseHandovers",
                columns: new[] { "CaseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CaseHandovers_OutgoingAttorneyId",
                table: "CaseHandovers",
                column: "OutgoingAttorneyId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseHandovers_ReceivingAttorneyId",
                table: "CaseHandovers",
                column: "ReceivingAttorneyId");

            migrationBuilder.CreateIndex(
                name: "IX_HandoverItems_CaseHandoverId",
                table: "HandoverItems",
                column: "CaseHandoverId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceComplaints_CaseId",
                table: "ServiceComplaints",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceComplaints_ClientId",
                table: "ServiceComplaints",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceComplaints_ReferenceNumber",
                table: "ServiceComplaints",
                column: "ReferenceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceComplaints_RoutedToUserId",
                table: "ServiceComplaints",
                column: "RoutedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceComplaints_Status_ResponseDueAtUtc",
                table: "ServiceComplaints",
                columns: new[] { "Status", "ResponseDueAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CaseForecasts");

            migrationBuilder.DropTable(
                name: "HandoverItems");

            migrationBuilder.DropTable(
                name: "ServiceComplaints");

            migrationBuilder.DropTable(
                name: "CaseHandovers");

            migrationBuilder.DropColumn(
                name: "CaseType",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "EvidenceStrength",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "RecordedOutcome",
                table: "Cases");
        }
    }
}
