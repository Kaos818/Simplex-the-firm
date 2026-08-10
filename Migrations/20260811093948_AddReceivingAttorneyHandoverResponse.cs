using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplexLawFirm.Migrations
{
    /// <inheritdoc />
    public partial class AddReceivingAttorneyHandoverResponse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AcceptanceRiskAcknowledged",
                table: "CaseHandovers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AcceptanceSignatureName",
                table: "CaseHandovers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceivingAttorneyDeclineReason",
                table: "CaseHandovers",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceivingAttorneyQuery",
                table: "CaseHandovers",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReceivingAttorneyRespondedAtUtc",
                table: "CaseHandovers",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptanceRiskAcknowledged",
                table: "CaseHandovers");

            migrationBuilder.DropColumn(
                name: "AcceptanceSignatureName",
                table: "CaseHandovers");

            migrationBuilder.DropColumn(
                name: "ReceivingAttorneyDeclineReason",
                table: "CaseHandovers");

            migrationBuilder.DropColumn(
                name: "ReceivingAttorneyQuery",
                table: "CaseHandovers");

            migrationBuilder.DropColumn(
                name: "ReceivingAttorneyRespondedAtUtc",
                table: "CaseHandovers");
        }
    }
}
