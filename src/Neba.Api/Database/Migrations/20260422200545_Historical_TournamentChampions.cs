using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Neba.Api.Database.Migrations
{
    /// <inheritdoc />
    public partial class Historical_TournamentChampions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "historical");

            migrationBuilder.CreateTable(
                name: "tournament_champions",
                schema: "historical",
                columns: table => new
                {
                    bowler_id = table.Column<int>(type: "integer", nullable: false),
                    tournament_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tournament_champions", x => new { x.tournament_id, x.bowler_id });
                    table.ForeignKey(
                        name: "fk_tournament_champions_bowlers_bowler_id",
                        column: x => x.bowler_id,
                        principalSchema: "app",
                        principalTable: "bowlers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_tournament_champions_tournaments_tournament_id",
                        column: x => x.tournament_id,
                        principalSchema: "app",
                        principalTable: "tournaments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tournament_champions_bowler_id",
                schema: "historical",
                table: "tournament_champions",
                column: "bowler_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tournament_champions",
                schema: "historical");
        }
    }
}
