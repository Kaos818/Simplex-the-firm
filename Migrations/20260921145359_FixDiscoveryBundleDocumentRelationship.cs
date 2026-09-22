using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplexLawFirm.Migrations
{
    /// <inheritdoc />
    public partial class FixDiscoveryBundleDocumentRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OpposingDisclosureDocuments_DiscoveryBundles_DiscoveryBundleId1",
                table: "OpposingDisclosureDocuments");

            migrationBuilder.DropIndex(
                name: "IX_OpposingDisclosureDocuments_DiscoveryBundleId1",
                table: "OpposingDisclosureDocuments");

            migrationBuilder.DropColumn(
                name: "DiscoveryBundleId1",
                table: "OpposingDisclosureDocuments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DiscoveryBundleId1",
                table: "OpposingDisclosureDocuments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpposingDisclosureDocuments_DiscoveryBundleId1",
                table: "OpposingDisclosureDocuments",
                column: "DiscoveryBundleId1");

            migrationBuilder.AddForeignKey(
                name: "FK_OpposingDisclosureDocuments_DiscoveryBundles_DiscoveryBundleId1",
                table: "OpposingDisclosureDocuments",
                column: "DiscoveryBundleId1",
                principalTable: "DiscoveryBundles",
                principalColumn: "Id");
        }
    }
}
