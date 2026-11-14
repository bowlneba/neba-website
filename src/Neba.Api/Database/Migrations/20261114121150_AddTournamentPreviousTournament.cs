using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Neba.Api.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddTournamentPreviousTournament : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "previous_tournament_id",
                schema: "app",
                table: "tournaments",
                type: "character(26)",
                fixedLength: true,
                maxLength: 26,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_tournaments_previous_tournament_id",
                schema: "app",
                table: "tournaments",
                column: "previous_tournament_id");

            migrationBuilder.AddForeignKey(
                name: "fk_tournaments_tournaments_previous_tournament_id",
                schema: "app",
                table: "tournaments",
                column: "previous_tournament_id",
                principalSchema: "app",
                principalTable: "tournaments",
                principalColumn: "domain_id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_tournaments_tournaments_previous_tournament_id",
                schema: "app",
                table: "tournaments");

            migrationBuilder.DropIndex(
                name: "ix_tournaments_previous_tournament_id",
                schema: "app",
                table: "tournaments");

            migrationBuilder.DropColumn(
                name: "previous_tournament_id",
                schema: "app",
                table: "tournaments");
        }
    }
}
