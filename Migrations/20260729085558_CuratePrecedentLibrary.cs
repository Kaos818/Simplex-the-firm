using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SimplexLawFirm.Migrations
{
    /// <inheritdoc />
    public partial class CuratePrecedentLibrary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "OutcomeIsConfidential",
                table: "Cases",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OutcomeIsPrivileged",
                table: "Cases",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OutcomeSummary",
                table: "Cases",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsConfidential",
                table: "CaseNotes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPrivileged",
                table: "CaseNotes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "KnowledgeArticles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsPrivileged = table.Column<bool>(type: "bit", nullable: false),
                    IsConfidential = table.Column<bool>(type: "bit", nullable: false),
                    SuggestedSubjectId = table.Column<int>(type: "int", nullable: true),
                    AuthorUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeArticles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LegalSubjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Keywords = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalSubjects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PrecedentIndexJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceType = table.Column<int>(type: "int", nullable: false),
                    SourceId = table.Column<int>(type: "int", nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    SourceText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MatterType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    SuggestedSubjectId = table.Column<int>(type: "int", nullable: true),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    IsPrivileged = table.Column<bool>(type: "bit", nullable: false),
                    IsConfidential = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ExclusionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    QueuedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NextAttemptAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrecedentIndexJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CoverageCommissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LegalSubjectId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Brief = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CommissionedByUserId = table.Column<int>(type: "int", nullable: false),
                    CommissionedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoverageCommissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoverageCommissions_LegalSubjects_LegalSubjectId",
                        column: x => x.LegalSubjectId,
                        principalTable: "LegalSubjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrecedentItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceType = table.Column<int>(type: "int", nullable: false),
                    SourceId = table.Column<int>(type: "int", nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    SourceText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LegalSubjectId = table.Column<int>(type: "int", nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    SourceDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IndexedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CuratorNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RetiredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrecedentItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrecedentItems_LegalSubjects_LegalSubjectId",
                        column: x => x.LegalSubjectId,
                        principalTable: "LegalSubjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrecedentConflictFlags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NewPrecedentItemId = table.Column<int>(type: "int", nullable: false),
                    ExistingPrecedentItemId = table.Column<int>(type: "int", nullable: false),
                    Similarity = table.Column<decimal>(type: "decimal(6,5)", precision: 6, scale: 5, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReviewedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrecedentConflictFlags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrecedentConflictFlags_PrecedentItems_ExistingPrecedentItemId",
                        column: x => x.ExistingPrecedentItemId,
                        principalTable: "PrecedentItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrecedentConflictFlags_PrecedentItems_NewPrecedentItemId",
                        column: x => x.NewPrecedentItemId,
                        principalTable: "PrecedentItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrecedentPassages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PrecedentItemId = table.Column<int>(type: "int", nullable: false),
                    PassageNumber = table.Column<int>(type: "int", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmbeddingJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrecedentPassages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrecedentPassages_PrecedentItems_PrecedentItemId",
                        column: x => x.PrecedentItemId,
                        principalTable: "PrecedentItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "LegalSubjects",
                columns: new[] { "Id", "IsActive", "Keywords", "Name" },
                values: new object[,]
                {
                    { 1, true, "litigation,court,trial,appeal,interdict,damages", "Civil Litigation" },
                    { 2, true, "commercial,contract,company,business,shareholder", "Commercial Law" },
                    { 3, true, "family,divorce,custody,maintenance,matrimonial", "Family Law" },
                    { 4, true, "labour,employment,employee,ccma,dismissal", "Labour Law" },
                    { 5, true, "criminal,bail,prosecution,sentence,accused", "Criminal Law" },
                    { 6, true, "property,transfer,lease,eviction,conveyancing", "Property Law" },
                    { 7, true, "estate,will,trust,beneficiary,executor", "Estates and Trusts" },
                    { 8, true, "injury,accident,raf,medical negligence,compensation", "Personal Injury" },
                    { 9, true, "general,advice,procedure", "General Practice" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CoverageCommissions_LegalSubjectId_Status",
                table: "CoverageCommissions",
                columns: new[] { "LegalSubjectId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_LegalSubjects_Name",
                table: "LegalSubjects",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrecedentConflictFlags_ExistingPrecedentItemId_NewPrecedentItemId",
                table: "PrecedentConflictFlags",
                columns: new[] { "ExistingPrecedentItemId", "NewPrecedentItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrecedentConflictFlags_NewPrecedentItemId",
                table: "PrecedentConflictFlags",
                column: "NewPrecedentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PrecedentIndexJobs_SourceType_SourceId_ContentHash",
                table: "PrecedentIndexJobs",
                columns: new[] { "SourceType", "SourceId", "ContentHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrecedentIndexJobs_Status_NextAttemptAtUtc",
                table: "PrecedentIndexJobs",
                columns: new[] { "Status", "NextAttemptAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PrecedentItems_LegalSubjectId_IsCurrent",
                table: "PrecedentItems",
                columns: new[] { "LegalSubjectId", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_PrecedentItems_SourceType_SourceId_ContentHash",
                table: "PrecedentItems",
                columns: new[] { "SourceType", "SourceId", "ContentHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrecedentPassages_PrecedentItemId_PassageNumber",
                table: "PrecedentPassages",
                columns: new[] { "PrecedentItemId", "PassageNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CoverageCommissions");

            migrationBuilder.DropTable(
                name: "KnowledgeArticles");

            migrationBuilder.DropTable(
                name: "PrecedentConflictFlags");

            migrationBuilder.DropTable(
                name: "PrecedentIndexJobs");

            migrationBuilder.DropTable(
                name: "PrecedentPassages");

            migrationBuilder.DropTable(
                name: "PrecedentItems");

            migrationBuilder.DropTable(
                name: "LegalSubjects");

            migrationBuilder.DropColumn(
                name: "OutcomeIsConfidential",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "OutcomeIsPrivileged",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "OutcomeSummary",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "IsConfidential",
                table: "CaseNotes");

            migrationBuilder.DropColumn(
                name: "IsPrivileged",
                table: "CaseNotes");
        }
    }
}
