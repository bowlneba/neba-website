using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Neba.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class Historical_TournamentEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tournament_entries",
                schema: "historical",
                columns: table => new
                {
                    tournament_id = table.Column<int>(type: "integer", nullable: false),
                    entries = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tournament_entries", x => x.tournament_id);
                    table.ForeignKey(
                        name: "fk_tournament_entries_tournaments_tournament_id",
                        column: x => x.tournament_id,
                        principalSchema: "app",
                        principalTable: "tournaments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tournament_entries",
                schema: "historical");
        }
    }
}
