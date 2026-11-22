using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Neba.Api.Database.Migrations
{
    /// <inheritdoc />
    public partial class UniquePreviousTournament : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_tournaments_previous_tournament_id",
                schema: "app",
                table: "tournaments");

            migrationBuilder.CreateIndex(
                name: "ix_tournaments_previous_tournament_id",
                schema: "app",
                table: "tournaments",
                column: "previous_tournament_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_tournaments_previous_tournament_id",
                schema: "app",
                table: "tournaments");

            migrationBuilder.CreateIndex(
                name: "ix_tournaments_previous_tournament_id",
                schema: "app",
                table: "tournaments",
                column: "previous_tournament_id");
        }
    }
}
