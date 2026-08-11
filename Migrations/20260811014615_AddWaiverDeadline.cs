using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplexLawFirm.Migrations
{
    /// <inheritdoc />
    public partial class AddWaiverDeadline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeadlineAtUtc",
                table: "CaseDocumentWaivers",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeadlineAtUtc",
                table: "CaseDocumentWaivers");
        }
    }
}
