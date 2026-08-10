using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplexLawFirm.Migrations
{
    /// <inheritdoc />
    public partial class EstimateMatterCost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MatterCostEstimateId",
                table: "Invoices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresEstimateAuthorisation",
                table: "Invoices",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CostEstimateCoverageGaps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MatterType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ComparableMatterCount = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcknowledgedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcknowledgedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostEstimateCoverageGaps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CostEstimateEnquiries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicToken = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: true),
                    ContactName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MatterType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MatterValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Urgency = table.Column<int>(type: "int", nullable: false),
                    RequiresCourtProceedings = table.Column<bool>(type: "bit", nullable: false),
                    DocumentReadiness = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConsultationRequestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostEstimateEnquiries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostEstimateEnquiries_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MatterCostEstimates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CostEstimateEnquiryId = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ComparableMatterCount = table.Column<int>(type: "int", nullable: false),
                    ProfessionalFeesLow = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ProfessionalFeesHigh = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DisbursementsLow = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DisbursementsHigh = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VatLow = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VatHigh = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalLow = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalHigh = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PermittedVarianceTolerance = table.Column<decimal>(type: "decimal(6,5)", precision: 6, scale: 5, nullable: false),
                    RatesSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssumptionsSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ComparableMattersSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeclineReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LockedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LinkedCaseId = table.Column<int>(type: "int", nullable: true),
                    LinkedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatterCostEstimates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatterCostEstimates_Cases_LinkedCaseId",
                        column: x => x.LinkedCaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatterCostEstimates_CostEstimateEnquiries_CostEstimateEnquiryId",
                        column: x => x.CostEstimateEnquiryId,
                        principalTable: "CostEstimateEnquiries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceEstimateAuthorisations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    MatterCostEstimateId = table.Column<int>(type: "int", nullable: false),
                    InvoiceTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VariancePercent = table.Column<decimal>(type: "decimal(8,5)", precision: 8, scale: 5, nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    ApprovedByUserId = table.Column<int>(type: "int", nullable: true),
                    RequestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovalReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceEstimateAuthorisations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceEstimateAuthorisations_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvoiceEstimateAuthorisations_MatterCostEstimates_MatterCostEstimateId",
                        column: x => x.MatterCostEstimateId,
                        principalTable: "MatterCostEstimates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvoiceEstimateAuthorisations_Users_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_MatterCostEstimateId",
                table: "Invoices",
                column: "MatterCostEstimateId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateCoverageGaps_Status_RecordedAtUtc",
                table: "CostEstimateCoverageGaps",
                columns: new[] { "Status", "RecordedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateEnquiries_ClientId",
                table: "CostEstimateEnquiries",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateEnquiries_Email_CreatedAtUtc",
                table: "CostEstimateEnquiries",
                columns: new[] { "Email", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimateEnquiries_PublicToken",
                table: "CostEstimateEnquiries",
                column: "PublicToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceEstimateAuthorisations_ApprovedByUserId",
                table: "InvoiceEstimateAuthorisations",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceEstimateAuthorisations_InvoiceId",
                table: "InvoiceEstimateAuthorisations",
                column: "InvoiceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceEstimateAuthorisations_MatterCostEstimateId",
                table: "InvoiceEstimateAuthorisations",
                column: "MatterCostEstimateId");

            migrationBuilder.CreateIndex(
                name: "IX_MatterCostEstimates_CostEstimateEnquiryId_Version",
                table: "MatterCostEstimates",
                columns: new[] { "CostEstimateEnquiryId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatterCostEstimates_LinkedCaseId",
                table: "MatterCostEstimates",
                column: "LinkedCaseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_MatterCostEstimates_MatterCostEstimateId",
                table: "Invoices",
                column: "MatterCostEstimateId",
                principalTable: "MatterCostEstimates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_MatterCostEstimates_MatterCostEstimateId",
                table: "Invoices");

            migrationBuilder.DropTable(
                name: "CostEstimateCoverageGaps");

            migrationBuilder.DropTable(
                name: "InvoiceEstimateAuthorisations");

            migrationBuilder.DropTable(
                name: "MatterCostEstimates");

            migrationBuilder.DropTable(
                name: "CostEstimateEnquiries");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_MatterCostEstimateId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "MatterCostEstimateId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "RequiresEstimateAuthorisation",
                table: "Invoices");
        }
    }
}
