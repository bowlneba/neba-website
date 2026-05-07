using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Neba.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class TournamentSponsors_Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tournament_sponsors",
                schema: "app",
                columns: table => new
                {
                    sponsor_id = table.Column<string>(type: "character(26)", fixedLength: true, maxLength: 26, nullable: false),
                    tournament_id = table.Column<int>(type: "integer", nullable: false),
                    is_title_sponsor = table.Column<bool>(type: "boolean", nullable: false),
                    sponsorship_amount = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tournament_sponsors", x => new { x.tournament_id, x.sponsor_id });
                    table.ForeignKey(
                        name: "fk_tournament_sponsors_sponsors_sponsor_id",
                        column: x => x.sponsor_id,
                        principalSchema: "app",
                        principalTable: "sponsors",
                        principalColumn: "domain_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_tournament_sponsors_tournaments_tournament_id",
                        column: x => x.tournament_id,
                        principalSchema: "app",
                        principalTable: "tournaments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tournament_sponsors_sponsor_id",
                schema: "app",
                table: "tournament_sponsors",
                column: "sponsor_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tournament_sponsors",
                schema: "app");
        }
    }
}
