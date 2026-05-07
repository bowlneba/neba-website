using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Neba.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class Historical_TournamentResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tournament_results",
                schema: "historical",
                columns: table => new
                {
                    bowler_id = table.Column<int>(type: "integer", nullable: false),
                    tournament_id = table.Column<int>(type: "integer", nullable: false),
                    place = table.Column<int>(type: "integer", nullable: true),
                    prize_money = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    side_cut_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tournament_results", x => new { x.tournament_id, x.bowler_id });
                    table.ForeignKey(
                        name: "fk_tournament_results_bowlers_bowler_id",
                        column: x => x.bowler_id,
                        principalSchema: "app",
                        principalTable: "bowlers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_tournament_results_side_cuts_side_cut_id",
                        column: x => x.side_cut_id,
                        principalSchema: "app",
                        principalTable: "side_cuts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_tournament_results_tournaments_tournament_id",
                        column: x => x.tournament_id,
                        principalSchema: "app",
                        principalTable: "tournaments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tournament_results_bowler_id",
                schema: "historical",
                table: "tournament_results",
                column: "bowler_id");

            migrationBuilder.CreateIndex(
                name: "ix_tournament_results_side_cut_id",
                schema: "historical",
                table: "tournament_results",
                column: "side_cut_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tournament_results",
                schema: "historical");
        }
    }
}
