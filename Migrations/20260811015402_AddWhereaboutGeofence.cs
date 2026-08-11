using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplexLawFirm.Migrations
{
    /// <inheritdoc />
    public partial class AddWhereaboutGeofence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "DistanceFromVenueMeters",
                table: "AttorneyWhereabouts",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "AttorneyWhereabouts",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationOverrideReason",
                table: "AttorneyWhereabouts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LocationVerified",
                table: "AttorneyWhereabouts",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "AttorneyWhereabouts",
                type: "float",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "KnownVenues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnownVenues", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KnownVenues");

            migrationBuilder.DropColumn(
                name: "DistanceFromVenueMeters",
                table: "AttorneyWhereabouts");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "AttorneyWhereabouts");

            migrationBuilder.DropColumn(
                name: "LocationOverrideReason",
                table: "AttorneyWhereabouts");

            migrationBuilder.DropColumn(
                name: "LocationVerified",
                table: "AttorneyWhereabouts");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "AttorneyWhereabouts");
        }
    }
}
