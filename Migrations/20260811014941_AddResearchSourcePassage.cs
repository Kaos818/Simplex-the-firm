using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplexLawFirm.Migrations
{
    /// <inheritdoc />
    public partial class AddResearchSourcePassage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourcePassage",
                table: "ResearchQueries",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourcePassage",
                table: "ResearchQueries");
        }
    }
}
