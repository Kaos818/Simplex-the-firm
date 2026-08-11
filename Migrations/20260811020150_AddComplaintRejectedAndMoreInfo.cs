using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplexLawFirm.Migrations
{
    /// <inheritdoc />
    public partial class AddComplaintRejectedAndMoreInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientAdditionalInformation",
                table: "ServiceComplaints",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InformationRequestNote",
                table: "ServiceComplaints",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InformationRequestedAtUtc",
                table: "ServiceComplaints",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientAdditionalInformation",
                table: "ServiceComplaints");

            migrationBuilder.DropColumn(
                name: "InformationRequestNote",
                table: "ServiceComplaints");

            migrationBuilder.DropColumn(
                name: "InformationRequestedAtUtc",
                table: "ServiceComplaints");
        }
    }
}
