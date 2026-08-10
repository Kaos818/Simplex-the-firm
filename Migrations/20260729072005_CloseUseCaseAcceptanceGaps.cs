using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplexLawFirm.Migrations
{
    /// <inheritdoc />
    public partial class CloseUseCaseAcceptanceGaps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClientForecastRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseId = table.Column<int>(type: "int", nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ClientMessage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FulfilledByForecastId = table.Column<int>(type: "int", nullable: true),
                    FulfilledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientForecastRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientForecastRequests_CaseForecasts_FulfilledByForecastId",
                        column: x => x.FulfilledByForecastId,
                        principalTable: "CaseForecasts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClientForecastRequests_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClientForecastRequests_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StaffServiceRecordEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StaffUserId = table.Column<int>(type: "int", nullable: false),
                    ServiceComplaintId = table.Column<int>(type: "int", nullable: false),
                    CaseId = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffServiceRecordEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffServiceRecordEntries_ServiceComplaints_ServiceComplaintId",
                        column: x => x.ServiceComplaintId,
                        principalTable: "ServiceComplaints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StaffServiceRecordEntries_Users_StaffUserId",
                        column: x => x.StaffUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClientForecastRequests_CaseId_Status",
                table: "ClientForecastRequests",
                columns: new[] { "CaseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ClientForecastRequests_ClientId",
                table: "ClientForecastRequests",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientForecastRequests_FulfilledByForecastId",
                table: "ClientForecastRequests",
                column: "FulfilledByForecastId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffServiceRecordEntries_ServiceComplaintId_StaffUserId",
                table: "StaffServiceRecordEntries",
                columns: new[] { "ServiceComplaintId", "StaffUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaffServiceRecordEntries_StaffUserId_RecordedAtUtc",
                table: "StaffServiceRecordEntries",
                columns: new[] { "StaffUserId", "RecordedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientForecastRequests");

            migrationBuilder.DropTable(
                name: "StaffServiceRecordEntries");
        }
    }
}
