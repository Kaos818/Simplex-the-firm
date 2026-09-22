using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplexLawFirm.Migrations
{
    /// <inheritdoc />
    public partial class AddAdverseCostExposure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdverseCostExposures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseId = table.Column<int>(type: "int", nullable: false),
                    CourtOrderDocumentId = table.Column<int>(type: "int", nullable: false),
                    LoggedByAttorneyId = table.Column<int>(type: "int", nullable: false),
                    IssuingCourt = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RulingDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CostOrderAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ExistingCaseCostsSnapshot = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalExposure = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AvailableTrustRetainerSnapshot = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Shortfall = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RevisedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    FinalSettledAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ProgressNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AccountantReviewedByUserId = table.Column<int>(type: "int", nullable: true),
                    AccountantReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DirectorReviewedByUserId = table.Column<int>(type: "int", nullable: true),
                    DirectorPosition = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DirectorReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdverseCostExposures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdverseCostExposures_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdverseCostExposures_Documents_CourtOrderDocumentId",
                        column: x => x.CourtOrderDocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdverseCostExposures_Users_AccountantReviewedByUserId",
                        column: x => x.AccountantReviewedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdverseCostExposures_Users_DirectorReviewedByUserId",
                        column: x => x.DirectorReviewedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdverseCostExposures_Users_LoggedByAttorneyId",
                        column: x => x.LoggedByAttorneyId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdverseCostExposures_AccountantReviewedByUserId",
                table: "AdverseCostExposures",
                column: "AccountantReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AdverseCostExposures_CaseId_Status",
                table: "AdverseCostExposures",
                columns: new[] { "CaseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AdverseCostExposures_CourtOrderDocumentId",
                table: "AdverseCostExposures",
                column: "CourtOrderDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_AdverseCostExposures_DirectorReviewedByUserId",
                table: "AdverseCostExposures",
                column: "DirectorReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AdverseCostExposures_LoggedByAttorneyId",
                table: "AdverseCostExposures",
                column: "LoggedByAttorneyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdverseCostExposures");
        }
    }
}
