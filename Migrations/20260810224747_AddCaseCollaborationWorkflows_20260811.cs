using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplexLawFirm.Migrations
{
    /// <inheritdoc />
    public partial class AddCaseCollaborationWorkflows_20260811 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DirectorFeedback",
                table: "ServiceComplaints",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RespondedAtUtc",
                table: "ServiceComplaints",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CaseDocumentRequestId",
                table: "Documents",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CaseDocumentRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseId = table.Column<int>(type: "int", nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    RequestedByAttorneyId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RequiredDocuments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DueAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReviewFeedback = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseDocumentRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseDocumentRequests_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CaseDocumentRequests_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CaseDocumentRequests_Users_RequestedByAttorneyId",
                        column: x => x.RequestedByAttorneyId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CaseHandoverRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseId = table.Column<int>(type: "int", nullable: false),
                    RequestedByAttorneyId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DirectorResponse = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByDirectorId = table.Column<int>(type: "int", nullable: true),
                    CaseHandoverId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseHandoverRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseHandoverRequests_CaseHandovers_CaseHandoverId",
                        column: x => x.CaseHandoverId,
                        principalTable: "CaseHandovers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CaseHandoverRequests_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CaseHandoverRequests_Users_RequestedByAttorneyId",
                        column: x => x.RequestedByAttorneyId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaseHandoverRequests_Users_ReviewedByDirectorId",
                        column: x => x.ReviewedByDirectorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_CaseDocumentRequestId",
                table: "Documents",
                column: "CaseDocumentRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseDocumentRequests_CaseId_Status",
                table: "CaseDocumentRequests",
                columns: new[] { "CaseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CaseDocumentRequests_ClientId",
                table: "CaseDocumentRequests",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseDocumentRequests_RequestedByAttorneyId",
                table: "CaseDocumentRequests",
                column: "RequestedByAttorneyId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseHandoverRequests_CaseHandoverId",
                table: "CaseHandoverRequests",
                column: "CaseHandoverId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseHandoverRequests_CaseId",
                table: "CaseHandoverRequests",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseHandoverRequests_RequestedByAttorneyId",
                table: "CaseHandoverRequests",
                column: "RequestedByAttorneyId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseHandoverRequests_ReviewedByDirectorId",
                table: "CaseHandoverRequests",
                column: "ReviewedByDirectorId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseHandoverRequests_Status_RequestedAtUtc",
                table: "CaseHandoverRequests",
                columns: new[] { "Status", "RequestedAtUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_CaseDocumentRequests_CaseDocumentRequestId",
                table: "Documents",
                column: "CaseDocumentRequestId",
                principalTable: "CaseDocumentRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_CaseDocumentRequests_CaseDocumentRequestId",
                table: "Documents");

            migrationBuilder.DropTable(
                name: "CaseDocumentRequests");

            migrationBuilder.DropTable(
                name: "CaseHandoverRequests");

            migrationBuilder.DropIndex(
                name: "IX_Documents_CaseDocumentRequestId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "DirectorFeedback",
                table: "ServiceComplaints");

            migrationBuilder.DropColumn(
                name: "RespondedAtUtc",
                table: "ServiceComplaints");

            migrationBuilder.DropColumn(
                name: "CaseDocumentRequestId",
                table: "Documents");
        }
    }
}
