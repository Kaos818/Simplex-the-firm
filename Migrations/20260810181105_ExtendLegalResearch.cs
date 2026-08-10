using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplexLawFirm.Migrations
{
    /// <inheritdoc />
    public partial class ExtendLegalResearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FullText",
                table: "LegalAuthorities",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ResearchDisagreements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseId = table.Column<int>(type: "int", nullable: false),
                    AttorneyId = table.Column<int>(type: "int", nullable: false),
                    Topic = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchDisagreements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResearchDisagreements_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResearchDisagreements_Users_AttorneyId",
                        column: x => x.AttorneyId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResearchQueries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseId = table.Column<int>(type: "int", nullable: false),
                    AttorneyId = table.Column<int>(type: "int", nullable: false),
                    CaseNoteId = table.Column<int>(type: "int", nullable: true),
                    Issue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResultCount = table.Column<int>(type: "int", nullable: false),
                    LimitedToInternal = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchQueries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResearchQueries_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResearchQueries_Users_AttorneyId",
                        column: x => x.AttorneyId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ResearchDisagreements_AttorneyId",
                table: "ResearchDisagreements",
                column: "AttorneyId");

            migrationBuilder.CreateIndex(
                name: "IX_ResearchDisagreements_CaseId",
                table: "ResearchDisagreements",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ResearchQueries_AttorneyId",
                table: "ResearchQueries",
                column: "AttorneyId");

            migrationBuilder.CreateIndex(
                name: "IX_ResearchQueries_CaseId_CreatedAtUtc",
                table: "ResearchQueries",
                columns: new[] { "CaseId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResearchDisagreements");

            migrationBuilder.DropTable(
                name: "ResearchQueries");

            migrationBuilder.DropColumn(
                name: "FullText",
                table: "LegalAuthorities");
        }
    }
}
