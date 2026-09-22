using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplexLawFirm.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscoveryDisclosureExchange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiscoveryBundles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseId = table.Column<int>(type: "int", nullable: false),
                    AttorneyId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OpposingAttorneyEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    OpposingAttorneyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ExchangeToken = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ExchangeExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExchangedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AccessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OpposingBundleReceivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ComplianceNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscoveryBundles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscoveryBundles_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DiscoveryBundles_Users_AttorneyId",
                        column: x => x.AttorneyId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DiscoveryBundleItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DiscoveryBundleId = table.Column<int>(type: "int", nullable: false),
                    DocumentId = table.Column<int>(type: "int", nullable: false),
                    ProposedPrivileged = table.Column<bool>(type: "bit", nullable: false),
                    PrivilegeReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PrivilegeDecision = table.Column<int>(type: "int", nullable: false),
                    DirectorId = table.Column<int>(type: "int", nullable: true),
                    DirectorReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DecidedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscoveryBundleItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscoveryBundleItems_DiscoveryBundles_DiscoveryBundleId",
                        column: x => x.DiscoveryBundleId,
                        principalTable: "DiscoveryBundles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DiscoveryBundleItems_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DiscoveryBundleItems_Users_DirectorId",
                        column: x => x.DirectorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OpposingDisclosureDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DiscoveryBundleId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    RelativePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DiscoveryBundleId1 = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpposingDisclosureDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpposingDisclosureDocuments_DiscoveryBundles_DiscoveryBundleId",
                        column: x => x.DiscoveryBundleId,
                        principalTable: "DiscoveryBundles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OpposingDisclosureDocuments_DiscoveryBundles_DiscoveryBundleId1",
                        column: x => x.DiscoveryBundleId1,
                        principalTable: "DiscoveryBundles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiscoveryBundleItems_DirectorId",
                table: "DiscoveryBundleItems",
                column: "DirectorId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscoveryBundleItems_DiscoveryBundleId_DocumentId",
                table: "DiscoveryBundleItems",
                columns: new[] { "DiscoveryBundleId", "DocumentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiscoveryBundleItems_DocumentId",
                table: "DiscoveryBundleItems",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscoveryBundles_AttorneyId",
                table: "DiscoveryBundles",
                column: "AttorneyId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscoveryBundles_CaseId",
                table: "DiscoveryBundles",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscoveryBundles_ExchangeToken",
                table: "DiscoveryBundles",
                column: "ExchangeToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpposingDisclosureDocuments_DiscoveryBundleId",
                table: "OpposingDisclosureDocuments",
                column: "DiscoveryBundleId");

            migrationBuilder.CreateIndex(
                name: "IX_OpposingDisclosureDocuments_DiscoveryBundleId1",
                table: "OpposingDisclosureDocuments",
                column: "DiscoveryBundleId1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscoveryBundleItems");

            migrationBuilder.DropTable(
                name: "OpposingDisclosureDocuments");

            migrationBuilder.DropTable(
                name: "DiscoveryBundles");
        }
    }
}
