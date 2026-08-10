using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplexLawFirm.Migrations
{
    /// <inheritdoc />
    public partial class ExtendComplaintResolution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ClientNotifiedOfResolution",
                table: "ServiceComplaints",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FormalResponse",
                table: "ServiceComplaints",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MediationSteps",
                table: "ServiceComplaints",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Outcome",
                table: "ServiceComplaints",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Remedy",
                table: "ServiceComplaints",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResolvedAtUtc",
                table: "ServiceComplaints",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResolvedByUserId",
                table: "ServiceComplaints",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ComplaintAppointments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceComplaintId = table.Column<int>(type: "int", nullable: false),
                    ScheduledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Format = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BookedByUserId = table.Column<int>(type: "int", nullable: false),
                    BookedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplaintAppointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComplaintAppointments_ServiceComplaints_ServiceComplaintId",
                        column: x => x.ServiceComplaintId,
                        principalTable: "ServiceComplaints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComplaintAppointments_Users_BookedByUserId",
                        column: x => x.BookedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceComplaints_ResolvedByUserId",
                table: "ServiceComplaints",
                column: "ResolvedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplaintAppointments_BookedByUserId",
                table: "ComplaintAppointments",
                column: "BookedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplaintAppointments_ServiceComplaintId",
                table: "ComplaintAppointments",
                column: "ServiceComplaintId");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceComplaints_Users_ResolvedByUserId",
                table: "ServiceComplaints",
                column: "ResolvedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceComplaints_Users_ResolvedByUserId",
                table: "ServiceComplaints");

            migrationBuilder.DropTable(
                name: "ComplaintAppointments");

            migrationBuilder.DropIndex(
                name: "IX_ServiceComplaints_ResolvedByUserId",
                table: "ServiceComplaints");

            migrationBuilder.DropColumn(
                name: "ClientNotifiedOfResolution",
                table: "ServiceComplaints");

            migrationBuilder.DropColumn(
                name: "FormalResponse",
                table: "ServiceComplaints");

            migrationBuilder.DropColumn(
                name: "MediationSteps",
                table: "ServiceComplaints");

            migrationBuilder.DropColumn(
                name: "Outcome",
                table: "ServiceComplaints");

            migrationBuilder.DropColumn(
                name: "Remedy",
                table: "ServiceComplaints");

            migrationBuilder.DropColumn(
                name: "ResolvedAtUtc",
                table: "ServiceComplaints");

            migrationBuilder.DropColumn(
                name: "ResolvedByUserId",
                table: "ServiceComplaints");
        }
    }
}
