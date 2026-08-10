using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplexLawFirm.Migrations
{
    /// <inheritdoc />
    public partial class AddDirectorHandoverQueryResponse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DirectorQueryRespondedAtUtc",
                table: "CaseHandovers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DirectorQueryRespondedByUserId",
                table: "CaseHandovers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DirectorQueryResponse",
                table: "CaseHandovers",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DirectorQueryRespondedAtUtc",
                table: "CaseHandovers");

            migrationBuilder.DropColumn(
                name: "DirectorQueryRespondedByUserId",
                table: "CaseHandovers");

            migrationBuilder.DropColumn(
                name: "DirectorQueryResponse",
                table: "CaseHandovers");
        }
    }
}
