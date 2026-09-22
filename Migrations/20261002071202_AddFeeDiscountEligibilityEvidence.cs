using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplexLawFirm.Migrations
{
    /// <inheritdoc />
    public partial class AddFeeDiscountEligibilityEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "EligibilityLimitSnapshot",
                table: "FeeDiscountRequests",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "EligibilityScore",
                table: "FeeDiscountRequests",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "EligibilityTier",
                table: "FeeDiscountRequests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SupportingDocumentPath",
                table: "FeeDiscountRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThresholdCheckSnapshot",
                table: "FeeDiscountRequests",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EligibilityLimitSnapshot",
                table: "FeeDiscountRequests");

            migrationBuilder.DropColumn(
                name: "EligibilityScore",
                table: "FeeDiscountRequests");

            migrationBuilder.DropColumn(
                name: "EligibilityTier",
                table: "FeeDiscountRequests");

            migrationBuilder.DropColumn(
                name: "SupportingDocumentPath",
                table: "FeeDiscountRequests");

            migrationBuilder.DropColumn(
                name: "ThresholdCheckSnapshot",
                table: "FeeDiscountRequests");
        }
    }
}
