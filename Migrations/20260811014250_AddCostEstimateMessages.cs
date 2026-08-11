using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplexLawFirm.Migrations
{
    /// <inheritdoc />
    public partial class AddCostEstimateMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CostEstimateMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CostEstimateEnquiryId = table.Column<int>(type: "int", nullable: false),
                    SenderName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsFromAdmin = table.Column<bool>(type: "bit", nullable: false),
                    AdminUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostEstimateMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostEstimateMessages_CostEstimateEnquiries_CostEstimateEnquiryId",
                        column: x => x.CostEstimateEnquiryId,
                        principalTable: "CostEstimateEnquiries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateMessages_CostEstimateEnquiryId",
                table: "CostEstimateMessages",
                column: "CostEstimateEnquiryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CostEstimateMessages");
        }
    }
}
