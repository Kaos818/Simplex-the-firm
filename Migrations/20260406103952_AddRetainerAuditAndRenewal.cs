using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplexLawFirm.Migrations
{
    /// <inheritdoc />
    public partial class AddRetainerAuditAndRenewal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ActivatedDate",
                table: "Retainers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssignedLawyerId",
                table: "Retainers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "Retainers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CancelledByUserId",
                table: "Retainers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledDate",
                table: "Retainers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ChangeRequestedDate",
                table: "Retainers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientChangeRequest",
                table: "Retainers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PaymentDueDays",
                table: "Retainers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RejectedByUserId",
                table: "Retainers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedDate",
                table: "Retainers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresUpfrontPayment",
                table: "Retainers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RevisionRequestedByUserId",
                table: "Retainers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RevisionRequestedDate",
                table: "Retainers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "Retainers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Retainers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RetainerActionLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RetainerId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetainerActionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RetainerActionLogs_Retainers_RetainerId",
                        column: x => x.RetainerId,
                        principalTable: "Retainers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RetainerActionLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RetainerRenewals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RetainerId = table.Column<int>(type: "int", nullable: false),
                    PreviousEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NewEndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RenewedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RenewedByUserId = table.Column<int>(type: "int", nullable: false),
                    AmountAdjustment = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetainerRenewals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RetainerRenewals_Retainers_RetainerId",
                        column: x => x.RetainerId,
                        principalTable: "Retainers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RetainerRenewals_Users_RenewedByUserId",
                        column: x => x.RenewedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Retainers_AssignedLawyerId",
                table: "Retainers",
                column: "AssignedLawyerId");

            migrationBuilder.CreateIndex(
                name: "IX_RetainerActionLogs_CreatedAt",
                table: "RetainerActionLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RetainerActionLogs_RetainerId",
                table: "RetainerActionLogs",
                column: "RetainerId");

            migrationBuilder.CreateIndex(
                name: "IX_RetainerActionLogs_UserId",
                table: "RetainerActionLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RetainerRenewals_RenewedByUserId",
                table: "RetainerRenewals",
                column: "RenewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RetainerRenewals_RetainerId",
                table: "RetainerRenewals",
                column: "RetainerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Retainers_Users_AssignedLawyerId",
                table: "Retainers",
                column: "AssignedLawyerId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Retainers_Users_AssignedLawyerId",
                table: "Retainers");

            migrationBuilder.DropTable(
                name: "RetainerActionLogs");

            migrationBuilder.DropTable(
                name: "RetainerRenewals");

            migrationBuilder.DropIndex(
                name: "IX_Retainers_AssignedLawyerId",
                table: "Retainers");

            migrationBuilder.DropColumn(
                name: "ActivatedDate",
                table: "Retainers");

            migrationBuilder.DropColumn(
                name: "AssignedLawyerId",
                table: "Retainers");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "Retainers");

            migrationBuilder.DropColumn(
                name: "CancelledByUserId",
                table: "Retainers");

            migrationBuilder.DropColumn(
                name: "CancelledDate",
                table: "Retainers");

            migrationBuilder.DropColumn(
                name: "ChangeRequestedDate",
                table: "Retainers");

            migrationBuilder.DropColumn(
                name: "ClientChangeRequest",
                table: "Retainers");

            migrationBuilder.DropColumn(
                name: "PaymentDueDays",
                table: "Retainers");

            migrationBuilder.DropColumn(
                name: "RejectedByUserId",
                table: "Retainers");

            migrationBuilder.DropColumn(
                name: "RejectedDate",
                table: "Retainers");

            migrationBuilder.DropColumn(
                name: "RequiresUpfrontPayment",
                table: "Retainers");

            migrationBuilder.DropColumn(
                name: "RevisionRequestedByUserId",
                table: "Retainers");

            migrationBuilder.DropColumn(
                name: "RevisionRequestedDate",
                table: "Retainers");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Retainers");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Retainers");
        }
    }
}
