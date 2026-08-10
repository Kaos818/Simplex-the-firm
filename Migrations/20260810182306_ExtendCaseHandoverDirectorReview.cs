using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplexLawFirm.Migrations
{
    /// <inheritdoc />
    public partial class ExtendCaseHandoverDirectorReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AcknowledgedByReceiving",
                table: "HandoverItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DirectorReturnReason",
                table: "CaseHandovers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DirectorReviewedAtUtc",
                table: "CaseHandovers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DirectorReviewedByUserId",
                table: "CaseHandovers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DirectorRiskFlags",
                table: "CaseHandovers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DirectorSummary",
                table: "CaseHandovers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceivingSignature",
                table: "CaseHandovers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RiskFlagsAcknowledgedByReceiving",
                table: "CaseHandovers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedForReviewAtUtc",
                table: "CaseHandovers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HandoverQueries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseHandoverId = table.Column<int>(type: "int", nullable: false),
                    RaisedByUserId = table.Column<int>(type: "int", nullable: false),
                    Question = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RaisedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HandoverQueries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HandoverQueries_CaseHandovers_CaseHandoverId",
                        column: x => x.CaseHandoverId,
                        principalTable: "CaseHandovers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HandoverQueries_Users_RaisedByUserId",
                        column: x => x.RaisedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CaseHandovers_DirectorReviewedByUserId",
                table: "CaseHandovers",
                column: "DirectorReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HandoverQueries_CaseHandoverId",
                table: "HandoverQueries",
                column: "CaseHandoverId");

            migrationBuilder.CreateIndex(
                name: "IX_HandoverQueries_RaisedByUserId",
                table: "HandoverQueries",
                column: "RaisedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CaseHandovers_Users_DirectorReviewedByUserId",
                table: "CaseHandovers",
                column: "DirectorReviewedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CaseHandovers_Users_DirectorReviewedByUserId",
                table: "CaseHandovers");

            migrationBuilder.DropTable(
                name: "HandoverQueries");

            migrationBuilder.DropIndex(
                name: "IX_CaseHandovers_DirectorReviewedByUserId",
                table: "CaseHandovers");

            migrationBuilder.DropColumn(
                name: "AcknowledgedByReceiving",
                table: "HandoverItems");

            migrationBuilder.DropColumn(
                name: "DirectorReturnReason",
                table: "CaseHandovers");

            migrationBuilder.DropColumn(
                name: "DirectorReviewedAtUtc",
                table: "CaseHandovers");

            migrationBuilder.DropColumn(
                name: "DirectorReviewedByUserId",
                table: "CaseHandovers");

            migrationBuilder.DropColumn(
                name: "DirectorRiskFlags",
                table: "CaseHandovers");

            migrationBuilder.DropColumn(
                name: "DirectorSummary",
                table: "CaseHandovers");

            migrationBuilder.DropColumn(
                name: "ReceivingSignature",
                table: "CaseHandovers");

            migrationBuilder.DropColumn(
                name: "RiskFlagsAcknowledgedByReceiving",
                table: "CaseHandovers");

            migrationBuilder.DropColumn(
                name: "SubmittedForReviewAtUtc",
                table: "CaseHandovers");
        }
    }
}
