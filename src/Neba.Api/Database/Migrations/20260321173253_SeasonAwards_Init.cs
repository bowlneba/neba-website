using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Neba.Api.Database.Migrations
{
    /// <inheritdoc />
    public partial class SeasonAwards_Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bowler_of_the_year_awards",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    domain_id = table.Column<string>(type: "character(26)", fixedLength: true, maxLength: 26, nullable: false),
                    season_id = table.Column<int>(type: "integer", nullable: false),
                    bowler_id = table.Column<string>(type: "character(26)", fixedLength: true, maxLength: 26, nullable: false),
                    category = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bowler_of_the_year_awards", x => x.id);
                    table.UniqueConstraint("ak_bowler_of_the_year_awards_domain_id", x => x.domain_id);
                    table.ForeignKey(
                        name: "fk_bowler_of_the_year_awards_bowlers_bowler_id",
                        column: x => x.bowler_id,
                        principalSchema: "app",
                        principalTable: "bowlers",
                        principalColumn: "domain_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_bowler_of_the_year_awards_seasons_season_id",
                        column: x => x.season_id,
                        principalSchema: "app",
                        principalTable: "seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "high_average_awards",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    domain_id = table.Column<string>(type: "character(26)", fixedLength: true, maxLength: 26, nullable: false),
                    season_id = table.Column<int>(type: "integer", nullable: false),
                    bowler_id = table.Column<string>(type: "character(26)", fixedLength: true, maxLength: 26, nullable: false),
                    average = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    total_games = table.Column<int>(type: "integer", nullable: true),
                    tournaments_participated = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_high_average_awards", x => x.id);
                    table.UniqueConstraint("ak_high_average_awards_domain_id", x => x.domain_id);
                    table.ForeignKey(
                        name: "fk_high_average_awards_bowlers_bowler_id",
                        column: x => x.bowler_id,
                        principalSchema: "app",
                        principalTable: "bowlers",
                        principalColumn: "domain_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_high_average_awards_seasons_season_id",
                        column: x => x.season_id,
                        principalSchema: "app",
                        principalTable: "seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "high_block_awards",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    domain_id = table.Column<string>(type: "character(26)", fixedLength: true, maxLength: 26, nullable: false),
                    season_id = table.Column<int>(type: "integer", nullable: false),
                    bowler_id = table.Column<string>(type: "character(26)", fixedLength: true, maxLength: 26, nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_high_block_awards", x => x.id);
                    table.UniqueConstraint("ak_high_block_awards_domain_id", x => x.domain_id);
                    table.ForeignKey(
                        name: "fk_high_block_awards_bowlers_bowler_id",
                        column: x => x.bowler_id,
                        principalSchema: "app",
                        principalTable: "bowlers",
                        principalColumn: "domain_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_high_block_awards_seasons_season_id",
                        column: x => x.season_id,
                        principalSchema: "app",
                        principalTable: "seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bowler_of_the_year_awards_bowler_id",
                schema: "app",
                table: "bowler_of_the_year_awards",
                column: "bowler_id");

            migrationBuilder.CreateIndex(
                name: "ix_bowler_of_the_year_awards_season_id",
                schema: "app",
                table: "bowler_of_the_year_awards",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "ix_high_average_awards_bowler_id",
                schema: "app",
                table: "high_average_awards",
                column: "bowler_id");

            migrationBuilder.CreateIndex(
                name: "ix_high_average_awards_season_id",
                schema: "app",
                table: "high_average_awards",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "ix_high_block_awards_bowler_id",
                schema: "app",
                table: "high_block_awards",
                column: "bowler_id");

            migrationBuilder.CreateIndex(
                name: "ix_high_block_awards_season_id",
                schema: "app",
                table: "high_block_awards",
                column: "season_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bowler_of_the_year_awards",
                schema: "app");

            migrationBuilder.DropTable(
                name: "high_average_awards",
                schema: "app");

            migrationBuilder.DropTable(
                name: "high_block_awards",
                schema: "app");
        }
    }
}
