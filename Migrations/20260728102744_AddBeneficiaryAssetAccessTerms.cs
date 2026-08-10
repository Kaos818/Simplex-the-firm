using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplexLawFirm.Migrations
{
    /// <inheritdoc />
    public partial class AddBeneficiaryAssetAccessTerms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AccessEligibleFromUtc",
                table: "Beneficiaries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AccessEligibleUntilUtc",
                table: "Beneficiaries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssetAccessTerms",
                table: "Beneficiaries",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EntitlementDescription",
                table: "Beneficiaries",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PermittedAssetPurposes",
                table: "Beneficiaries",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessEligibleFromUtc",
                table: "Beneficiaries");

            migrationBuilder.DropColumn(
                name: "AccessEligibleUntilUtc",
                table: "Beneficiaries");

            migrationBuilder.DropColumn(
                name: "AssetAccessTerms",
                table: "Beneficiaries");

            migrationBuilder.DropColumn(
                name: "EntitlementDescription",
                table: "Beneficiaries");

            migrationBuilder.DropColumn(
                name: "PermittedAssetPurposes",
                table: "Beneficiaries");
        }
    }
}
