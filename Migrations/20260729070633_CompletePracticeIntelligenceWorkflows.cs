using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplexLawFirm.Migrations
{
    /// <inheritdoc />
    public partial class CompletePracticeIntelligenceWorkflows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceFingerprint",
                table: "HandoverItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CaseReassignmentId",
                table: "CaseHandovers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CaseReassignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseId = table.Column<int>(type: "int", nullable: false),
                    OutgoingAttorneyId = table.Column<int>(type: "int", nullable: false),
                    ReceivingAttorneyId = table.Column<int>(type: "int", nullable: false),
                    ApprovedByUserId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseReassignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseReassignments_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaseReassignments_Users_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaseReassignments_Users_OutgoingAttorneyId",
                        column: x => x.OutgoingAttorneyId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaseReassignments_Users_ReceivingAttorneyId",
                        column: x => x.ReceivingAttorneyId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClientCorrespondence",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseId = table.Column<int>(type: "int", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AnsweredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientCorrespondence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientCorrespondence_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ComplaintAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceComplaintId = table.Column<int>(type: "int", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RelativePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256Hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UploadedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplaintAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComplaintAttachments_ServiceComplaints_ServiceComplaintId",
                        column: x => x.ServiceComplaintId,
                        principalTable: "ServiceComplaints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ForecastCalibrations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttorneyId = table.Column<int>(type: "int", nullable: true),
                    ForecastCount = table.Column<int>(type: "int", nullable: false),
                    MeanAccuracy = table.Column<decimal>(type: "decimal(6,5)", precision: 6, scale: 5, nullable: false),
                    MeanBias = table.Column<decimal>(type: "decimal(6,5)", precision: 6, scale: 5, nullable: false),
                    OptimisticForecastCount = table.Column<int>(type: "int", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForecastCalibrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ForecastCalibrations_Users_AttorneyId",
                        column: x => x.AttorneyId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CaseHandovers_CaseReassignmentId",
                table: "CaseHandovers",
                column: "CaseReassignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseReassignments_ApprovedByUserId",
                table: "CaseReassignments",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseReassignments_CaseId_Status",
                table: "CaseReassignments",
                columns: new[] { "CaseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CaseReassignments_OutgoingAttorneyId",
                table: "CaseReassignments",
                column: "OutgoingAttorneyId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseReassignments_ReceivingAttorneyId",
                table: "CaseReassignments",
                column: "ReceivingAttorneyId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientCorrespondence_CaseId_AnsweredAtUtc",
                table: "ClientCorrespondence",
                columns: new[] { "CaseId", "AnsweredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ComplaintAttachments_ServiceComplaintId",
                table: "ComplaintAttachments",
                column: "ServiceComplaintId");

            migrationBuilder.CreateIndex(
                name: "IX_ForecastCalibrations_AttorneyId",
                table: "ForecastCalibrations",
                column: "AttorneyId",
                unique: true,
                filter: "[AttorneyId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_CaseHandovers_CaseReassignments_CaseReassignmentId",
                table: "CaseHandovers",
                column: "CaseReassignmentId",
                principalTable: "CaseReassignments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CaseHandovers_CaseReassignments_CaseReassignmentId",
                table: "CaseHandovers");

            migrationBuilder.DropTable(
                name: "CaseReassignments");

            migrationBuilder.DropTable(
                name: "ClientCorrespondence");

            migrationBuilder.DropTable(
                name: "ComplaintAttachments");

            migrationBuilder.DropTable(
                name: "ForecastCalibrations");

            migrationBuilder.DropIndex(
                name: "IX_CaseHandovers_CaseReassignmentId",
                table: "CaseHandovers");

            migrationBuilder.DropColumn(
                name: "SourceFingerprint",
                table: "HandoverItems");

            migrationBuilder.DropColumn(
                name: "CaseReassignmentId",
                table: "CaseHandovers");
        }
    }
}
