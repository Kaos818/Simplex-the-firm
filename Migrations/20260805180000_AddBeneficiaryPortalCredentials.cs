using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SimplexLawFirm.Data;

#nullable disable

namespace SimplexLawFirm.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260805180000_AddBeneficiaryPortalCredentials")]
    public partial class AddBeneficiaryPortalCredentials : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(name: "PortalAccessEnabled", table: "Beneficiaries", type: "bit", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<string>(name: "PortalPasswordHash", table: "Beneficiaries", type: "nvarchar(500)", maxLength: 500, nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "PortalPasswordSetAtUtc", table: "Beneficiaries", type: "datetime2", nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "PortalAccessEnabled", table: "Beneficiaries");
            migrationBuilder.DropColumn(name: "PortalPasswordHash", table: "Beneficiaries");
            migrationBuilder.DropColumn(name: "PortalPasswordSetAtUtc", table: "Beneficiaries");
        }
    }
}
