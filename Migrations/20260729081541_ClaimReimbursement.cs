using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SimplexLawFirm.Migrations
{
    /// <inheritdoc />
    public partial class ClaimReimbursement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExpensePolicies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpenseType = table.Column<int>(type: "int", nullable: false),
                    PerItemLimit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DelegatedApprovalLimit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DefaultClassification = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpensePolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MatterExpenseTerms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseId = table.Column<int>(type: "int", nullable: false),
                    ExpenseType = table.Column<int>(type: "int", nullable: false),
                    Classification = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatterExpenseTerms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatterExpenseTerms_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReimbursementClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClaimNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CaseId = table.Column<int>(type: "int", nullable: false),
                    AttorneyId = table.Column<int>(type: "int", nullable: false),
                    ExpenseType = table.Column<int>(type: "int", nullable: false),
                    ExpenseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    MatchedActivityType = table.Column<int>(type: "int", nullable: true),
                    MatchedActivityId = table.Column<int>(type: "int", nullable: true),
                    ValidationFailureReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProofOriginalFileName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProofRelativePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProofContentType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProofSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    ProofSha256Hash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PolicyLimitSnapshot = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DelegatedLimitSnapshot = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ExceedsPolicyLimit = table.Column<bool>(type: "bit", nullable: false),
                    Classification = table.Column<int>(type: "int", nullable: false),
                    ClassificationReason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DecidedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecidedByUserId = table.Column<int>(type: "int", nullable: true),
                    DecisionReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReimbursementClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReimbursementClaims_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReimbursementClaims_Users_AttorneyId",
                        column: x => x.AttorneyId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReimbursementClaims_Users_DecidedByUserId",
                        column: x => x.DecidedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttorneyReimbursementPayables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReimbursementClaimId = table.Column<int>(type: "int", nullable: false),
                    AttorneyId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsPaid = table.Column<bool>(type: "bit", nullable: false),
                    PaidAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentReference = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttorneyReimbursementPayables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttorneyReimbursementPayables_ReimbursementClaims_ReimbursementClaimId",
                        column: x => x.ReimbursementClaimId,
                        principalTable: "ReimbursementClaims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttorneyReimbursementPayables_Users_AttorneyId",
                        column: x => x.AttorneyId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MatterDisbursements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReimbursementClaimId = table.Column<int>(type: "int", nullable: false),
                    CaseId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IncurredDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InvoiceId = table.Column<int>(type: "int", nullable: true),
                    InvoicedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatterDisbursements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatterDisbursements_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatterDisbursements_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatterDisbursements_ReimbursementClaims_ReimbursementClaimId",
                        column: x => x.ReimbursementClaimId,
                        principalTable: "ReimbursementClaims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReimbursementAuditEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReimbursementClaimId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActorUserId = table.Column<int>(type: "int", nullable: true),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReimbursementAuditEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReimbursementAuditEntries_ReimbursementClaims_ReimbursementClaimId",
                        column: x => x.ReimbursementClaimId,
                        principalTable: "ReimbursementClaims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ExpensePolicies",
                columns: new[] { "Id", "DefaultClassification", "DelegatedApprovalLimit", "ExpenseType", "IsActive", "PerItemLimit" },
                values: new object[,]
                {
                    { 1, 0, 1500m, 0, true, 2500m },
                    { 2, 0, 5000m, 1, true, 15000m },
                    { 3, 0, 5000m, 2, true, 10000m },
                    { 4, 0, 1000m, 3, true, 1500m },
                    { 5, 0, 2500m, 4, true, 3500m },
                    { 6, 1, 500m, 5, true, 600m },
                    { 7, 0, 500m, 6, true, 500m },
                    { 8, 1, 1000m, 7, true, 2000m },
                    { 9, 1, 500m, 8, true, 1000m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttorneyReimbursementPayables_AttorneyId",
                table: "AttorneyReimbursementPayables",
                column: "AttorneyId");

            migrationBuilder.CreateIndex(
                name: "IX_AttorneyReimbursementPayables_ReimbursementClaimId",
                table: "AttorneyReimbursementPayables",
                column: "ReimbursementClaimId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpensePolicies_ExpenseType",
                table: "ExpensePolicies",
                column: "ExpenseType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatterDisbursements_CaseId_InvoiceId",
                table: "MatterDisbursements",
                columns: new[] { "CaseId", "InvoiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_MatterDisbursements_InvoiceId",
                table: "MatterDisbursements",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_MatterDisbursements_ReimbursementClaimId",
                table: "MatterDisbursements",
                column: "ReimbursementClaimId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatterExpenseTerms_CaseId_ExpenseType",
                table: "MatterExpenseTerms",
                columns: new[] { "CaseId", "ExpenseType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReimbursementAuditEntries_ReimbursementClaimId_RecordedAtUtc",
                table: "ReimbursementAuditEntries",
                columns: new[] { "ReimbursementClaimId", "RecordedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ReimbursementClaims_AttorneyId_SubmittedAtUtc",
                table: "ReimbursementClaims",
                columns: new[] { "AttorneyId", "SubmittedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ReimbursementClaims_CaseId",
                table: "ReimbursementClaims",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ReimbursementClaims_ClaimNumber",
                table: "ReimbursementClaims",
                column: "ClaimNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReimbursementClaims_DecidedByUserId",
                table: "ReimbursementClaims",
                column: "DecidedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReimbursementClaims_Status_SubmittedAtUtc",
                table: "ReimbursementClaims",
                columns: new[] { "Status", "SubmittedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttorneyReimbursementPayables");

            migrationBuilder.DropTable(
                name: "ExpensePolicies");

            migrationBuilder.DropTable(
                name: "MatterDisbursements");

            migrationBuilder.DropTable(
                name: "MatterExpenseTerms");

            migrationBuilder.DropTable(
                name: "ReimbursementAuditEntries");

            migrationBuilder.DropTable(
                name: "ReimbursementClaims");
        }
    }
}
