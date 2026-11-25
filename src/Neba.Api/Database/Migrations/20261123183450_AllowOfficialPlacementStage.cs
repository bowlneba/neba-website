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
            // One-way on populated data: this fails if any Placement Stage is already Official. Roll back by
            // first returning those stages to Computed through the domain (a recompute), which also clears
            // official_at_utc. A raw status update would leave official_at_utc set.
            migrationBuilder.AddCheckConstraint(
                name: "ck_series_stages_placement_never_official",
                schema: "app",
                table: "series_stages",
                sql: "NOT (kind = 3 AND status = 2)");
        }
    }
}
