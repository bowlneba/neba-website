using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Neba.Api.Database.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceTournamentCompleteWithStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "complete",
                schema: "app",
                table: "tournaments",
                newName: "title_eligible");

            migrationBuilder.AddColumn<int>(
                name: "status",
                schema: "app",
                table: "tournaments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Backfill: EF's rename above preserved the old "complete" bool values in
            // title_eligible (matching this migration's intent 1:1 - a previously complete
            // tournament is always title eligible), so status just needs to follow the same
            // signal: complete -> Completed (1), not complete -> Scheduled (0, the column default
            // already applied above).
            migrationBuilder.Sql(
                """
                UPDATE app.tournaments
                SET status = 1
                WHERE title_eligible = true;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "status",
                schema: "app",
                table: "tournaments");

            migrationBuilder.RenameColumn(
                name: "title_eligible",
                schema: "app",
                table: "tournaments",
                newName: "complete");
        }
    }
}
