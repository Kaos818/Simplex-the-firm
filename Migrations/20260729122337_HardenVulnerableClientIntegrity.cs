using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplexLawFirm.Migrations
{
    /// <inheritdoc />
    public partial class HardenVulnerableClientIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_VulnerableFlagAcknowledgements_StaffUserId",
                table: "VulnerableFlagAcknowledgements",
                column: "StaffUserId");

            migrationBuilder.CreateIndex(
                name: "IX_VulnerableClientFlags_ClientId_Safeguard",
                table: "VulnerableClientFlags",
                columns: new[] { "ClientId", "Safeguard" },
                unique: true,
                filter: "[Status] <> 3");

            migrationBuilder.CreateIndex(
                name: "IX_ClientSupportSessions_AuthorisedByStaffUserId",
                table: "ClientSupportSessions",
                column: "AuthorisedByStaffUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentSupportPersonAssignments_RecordedByUserId",
                table: "AppointmentSupportPersonAssignments",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentInterpreterAssignments_AssignedByUserId",
                table: "AppointmentInterpreterAssignments",
                column: "AssignedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppointmentInterpreterAssignments_Users_AssignedByUserId",
                table: "AppointmentInterpreterAssignments",
                column: "AssignedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AppointmentSupportPersonAssignments_Users_RecordedByUserId",
                table: "AppointmentSupportPersonAssignments",
                column: "RecordedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClientSupportSessions_Clients_ClientId",
                table: "ClientSupportSessions",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClientSupportSessions_Users_AuthorisedByStaffUserId",
                table: "ClientSupportSessions",
                column: "AuthorisedByStaffUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VulnerableFlagAcknowledgements_Cases_CaseId",
                table: "VulnerableFlagAcknowledgements",
                column: "CaseId",
                principalTable: "Cases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VulnerableFlagAcknowledgements_Users_StaffUserId",
                table: "VulnerableFlagAcknowledgements",
                column: "StaffUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppointmentInterpreterAssignments_Users_AssignedByUserId",
                table: "AppointmentInterpreterAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_AppointmentSupportPersonAssignments_Users_RecordedByUserId",
                table: "AppointmentSupportPersonAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_ClientSupportSessions_Clients_ClientId",
                table: "ClientSupportSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_ClientSupportSessions_Users_AuthorisedByStaffUserId",
                table: "ClientSupportSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_VulnerableFlagAcknowledgements_Cases_CaseId",
                table: "VulnerableFlagAcknowledgements");

            migrationBuilder.DropForeignKey(
                name: "FK_VulnerableFlagAcknowledgements_Users_StaffUserId",
                table: "VulnerableFlagAcknowledgements");

            migrationBuilder.DropIndex(
                name: "IX_VulnerableFlagAcknowledgements_StaffUserId",
                table: "VulnerableFlagAcknowledgements");

            migrationBuilder.DropIndex(
                name: "IX_VulnerableClientFlags_ClientId_Safeguard",
                table: "VulnerableClientFlags");

            migrationBuilder.DropIndex(
                name: "IX_ClientSupportSessions_AuthorisedByStaffUserId",
                table: "ClientSupportSessions");

            migrationBuilder.DropIndex(
                name: "IX_AppointmentSupportPersonAssignments_RecordedByUserId",
                table: "AppointmentSupportPersonAssignments");

            migrationBuilder.DropIndex(
                name: "IX_AppointmentInterpreterAssignments_AssignedByUserId",
                table: "AppointmentInterpreterAssignments");
        }
    }
}
