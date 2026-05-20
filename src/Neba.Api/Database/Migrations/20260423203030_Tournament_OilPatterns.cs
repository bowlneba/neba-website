using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Neba.Api.Database.Migrations
{
    /// <inheritdoc />
    public partial class Tournament_OilPatterns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tournament_oil_patterns",
                schema: "app",
                columns: table => new
                {
                    oil_pattern_id = table.Column<string>(type: "character(26)", nullable: false),
                    tournament_id = table.Column<int>(type: "integer", nullable: false),
                    tournament_rounds = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tournament_oil_patterns", x => new { x.tournament_id, x.oil_pattern_id });
                    table.ForeignKey(
                        name: "fk_tournament_oil_patterns_oil_patterns_oil_pattern_id",
                        column: x => x.oil_pattern_id,
                        principalSchema: "app",
                        principalTable: "oil_patterns",
                        principalColumn: "domain_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_tournament_oil_patterns_tournaments_tournament_id",
                        column: x => x.tournament_id,
                        principalSchema: "app",
                        principalTable: "tournaments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tournament_oil_patterns_oil_pattern_id",
                schema: "app",
                table: "tournament_oil_patterns",
                column: "oil_pattern_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tournament_oil_patterns",
                schema: "app");
        }
    }
}
