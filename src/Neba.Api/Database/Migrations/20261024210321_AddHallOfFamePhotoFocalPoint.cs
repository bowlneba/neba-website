using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Neba.Api.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddHallOfFamePhotoFocalPoint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "photo_focal_x",
                schema: "app",
                table: "hall_of_fame_inductions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "photo_focal_y",
                schema: "app",
                table: "hall_of_fame_inductions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "photo_zoom",
                schema: "app",
                table: "hall_of_fame_inductions",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "photo_focal_x",
                schema: "app",
                table: "hall_of_fame_inductions");

            migrationBuilder.DropColumn(
                name: "photo_focal_y",
                schema: "app",
                table: "hall_of_fame_inductions");

            migrationBuilder.DropColumn(
                name: "photo_zoom",
                schema: "app",
                table: "hall_of_fame_inductions");
        }
    }
}
