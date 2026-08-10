using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplexLawFirm.Migrations
{
    /// <inheritdoc />
    public partial class FlagVulnerableClient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppointmentInterpreterAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CalendarEventId = table.Column<int>(type: "int", nullable: false),
                    InterpreterName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Language = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ContactDetails = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AssignedByUserId = table.Column<int>(type: "int", nullable: false),
                    AssignedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentInterpreterAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppointmentInterpreterAssignments_CalendarEvents_CalendarEventId",
                        column: x => x.CalendarEventId,
                        principalTable: "CalendarEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppointmentSupportPersonAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CalendarEventId = table.Column<int>(type: "int", nullable: false),
                    SupportPersonName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Relationship = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    RecordedByUserId = table.Column<int>(type: "int", nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentSupportPersonAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppointmentSupportPersonAssignments_CalendarEvents_CalendarEventId",
                        column: x => x.CalendarEventId,
                        principalTable: "CalendarEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClientSupportSessions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    AuthorisedByStaffUserId = table.Column<int>(type: "int", nullable: false),
                    SupportPersonName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    StartsAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientSupportSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VulnerableClientFlags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    Safeguard = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    LanguageRequired = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RaisedByAttorneyId = table.Column<int>(type: "int", nullable: false),
                    RaisedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewDueAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NextReviewAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByDirectorId = table.Column<int>(type: "int", nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    LastChangedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RemovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VulnerableClientFlags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VulnerableClientFlags_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VulnerableClientFlags_Users_RaisedByAttorneyId",
                        column: x => x.RaisedByAttorneyId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VulnerableClientFlags_Users_ReviewedByDirectorId",
                        column: x => x.ReviewedByDirectorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VulnerableFlagAcknowledgements",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VulnerableClientFlagId = table.Column<int>(type: "int", nullable: false),
                    CaseId = table.Column<int>(type: "int", nullable: false),
                    StaffUserId = table.Column<int>(type: "int", nullable: false),
                    AcknowledgedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VulnerableFlagAcknowledgements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VulnerableFlagAcknowledgements_VulnerableClientFlags_VulnerableClientFlagId",
                        column: x => x.VulnerableClientFlagId,
                        principalTable: "VulnerableClientFlags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentInterpreterAssignments_CalendarEventId",
                table: "AppointmentInterpreterAssignments",
                column: "CalendarEventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentSupportPersonAssignments_CalendarEventId",
                table: "AppointmentSupportPersonAssignments",
                column: "CalendarEventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientSupportSessions_ClientId_ExpiresAtUtc",
                table: "ClientSupportSessions",
                columns: new[] { "ClientId", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_VulnerableClientFlags_ClientId_Status",
                table: "VulnerableClientFlags",
                columns: new[] { "ClientId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_VulnerableClientFlags_RaisedByAttorneyId",
                table: "VulnerableClientFlags",
                column: "RaisedByAttorneyId");

            migrationBuilder.CreateIndex(
                name: "IX_VulnerableClientFlags_ReviewedByDirectorId",
                table: "VulnerableClientFlags",
                column: "ReviewedByDirectorId");

            migrationBuilder.CreateIndex(
                name: "IX_VulnerableClientFlags_Status_NextReviewAtUtc",
                table: "VulnerableClientFlags",
                columns: new[] { "Status", "NextReviewAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_VulnerableClientFlags_Status_ReviewDueAtUtc",
                table: "VulnerableClientFlags",
                columns: new[] { "Status", "ReviewDueAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_VulnerableFlagAcknowledgements_CaseId_StaffUserId_VulnerableClientFlagId",
                table: "VulnerableFlagAcknowledgements",
                columns: new[] { "CaseId", "StaffUserId", "VulnerableClientFlagId" });

            migrationBuilder.CreateIndex(
                name: "IX_VulnerableFlagAcknowledgements_VulnerableClientFlagId",
                table: "VulnerableFlagAcknowledgements",
                column: "VulnerableClientFlagId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppointmentInterpreterAssignments");

            migrationBuilder.DropTable(
                name: "AppointmentSupportPersonAssignments");

            migrationBuilder.DropTable(
                name: "ClientSupportSessions");

            migrationBuilder.DropTable(
                name: "VulnerableFlagAcknowledgements");

            migrationBuilder.DropTable(
                name: "VulnerableClientFlags");
        }
    }
}
