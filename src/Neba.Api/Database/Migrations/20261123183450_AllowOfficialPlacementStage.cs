using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Neba.Api.Database.Migrations
{
    /// <inheritdoc />
    public partial class AllowOfficialPlacementStage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_series_stages_placement_never_official",
                schema: "app",
                table: "series_stages");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "ck_series_stages_placement_never_official",
                schema: "app",
                table: "series_stages",
                sql: "NOT (kind = 3 AND status = 2)");
        }
    }
}
