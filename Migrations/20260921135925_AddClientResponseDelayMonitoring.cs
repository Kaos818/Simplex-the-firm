using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplexLawFirm.Migrations
{
    /// <inheritdoc />
    public partial class AddClientResponseDelayMonitoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientResponseText",
                table: "CaseDocumentRequests",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastDelayAction",
                table: "CaseDocumentRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastDelayActionAtUtc",
                table: "CaseDocumentRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastDelayActionNote",
                table: "CaseDocumentRequests",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OverdueAtUtc",
                table: "CaseDocumentRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequestType",
                table: "CaseDocumentRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ResponseSlaDaysSnapshot",
                table: "CaseDocumentRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ClientResponseSlaRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestType = table.Column<int>(type: "int", nullable: false),
                    ResponseDays = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientResponseSlaRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientResponseSlaRules_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CaseDocumentRequests_Status_DueAtUtc",
                table: "CaseDocumentRequests",
                columns: new[] { "Status", "DueAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ClientResponseSlaRules_RequestType",
                table: "ClientResponseSlaRules",
                column: "RequestType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientResponseSlaRules_UpdatedByUserId",
                table: "ClientResponseSlaRules",
                column: "UpdatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientResponseSlaRules");

            migrationBuilder.DropIndex(
                name: "IX_CaseDocumentRequests_Status_DueAtUtc",
                table: "CaseDocumentRequests");

            migrationBuilder.DropColumn(
                name: "ClientResponseText",
                table: "CaseDocumentRequests");

            migrationBuilder.DropColumn(
                name: "LastDelayAction",
                table: "CaseDocumentRequests");

            migrationBuilder.DropColumn(
                name: "LastDelayActionAtUtc",
                table: "CaseDocumentRequests");

            migrationBuilder.DropColumn(
                name: "LastDelayActionNote",
                table: "CaseDocumentRequests");

            migrationBuilder.DropColumn(
                name: "OverdueAtUtc",
                table: "CaseDocumentRequests");

            migrationBuilder.DropColumn(
                name: "RequestType",
                table: "CaseDocumentRequests");

            migrationBuilder.DropColumn(
                name: "ResponseSlaDaysSnapshot",
                table: "CaseDocumentRequests");
        }
    }
}
