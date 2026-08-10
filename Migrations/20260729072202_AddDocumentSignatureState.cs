using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SimplexLawFirm.Data;

#nullable disable

namespace SimplexLawFirm.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260729072202_AddDocumentSignatureState")]
public sealed class AddDocumentSignatureState : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "RequiresSignature",
            table: "Documents",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<DateTime>(
            name: "SignedAtUtc",
            table: "Documents",
            type: "datetime2",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "RequiresSignature", table: "Documents");
        migrationBuilder.DropColumn(name: "SignedAtUtc", table: "Documents");
    }
}
