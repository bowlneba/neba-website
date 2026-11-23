using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Neba.Api.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddSeries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "series",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    domain_id = table.Column<string>(type: "character(26)", fixedLength: true, maxLength: 26, nullable: false),
                    name = table.Column<string>(type: "character varying(127)", maxLength: 127, nullable: false),
                    season_id = table.Column<string>(type: "character(26)", fixedLength: true, maxLength: 26, nullable: false),
                    finale_finalist_count = table.Column<int>(type: "integer", nullable: true),
                    finale_date = table.Column<DateOnly>(type: "date", nullable: true),
                    finale_start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    finale_bowling_center_id = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: true),
                    logo_container = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: true),
                    logo_content_type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    logo_file_path = table.Column<string>(type: "character varying(1023)", maxLength: 1023, nullable: true),
                    logo_size_in_bytes = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_series", x => x.id);
                    table.UniqueConstraint("ak_series_domain_id", x => x.domain_id);
                    table.CheckConstraint("ck_series_finale_all_or_none", "(finale_finalist_count IS NULL AND finale_date IS NULL AND finale_start_time IS NULL) OR (finale_finalist_count IS NOT NULL AND finale_date IS NOT NULL AND finale_start_time IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_series_bowling_centers_finale_bowling_center_id",
                        column: x => x.finale_bowling_center_id,
                        principalSchema: "app",
                        principalTable: "bowling_centers",
                        principalColumn: "certification_number");
                    table.ForeignKey(
                        name: "fk_series_seasons_season_id",
                        column: x => x.season_id,
                        principalSchema: "app",
                        principalTable: "seasons",
                        principalColumn: "domain_id");
                });

            migrationBuilder.CreateTable(
                name: "series_stages",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    domain_id = table.Column<string>(type: "character(26)", fixedLength: true, maxLength: 26, nullable: false),
                    stage_order = table.Column<int>(type: "integer", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    tournament_id = table.Column<string>(type: "character(26)", fixedLength: true, maxLength: 26, nullable: false),
                    advancing_count = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    series_id = table.Column<int>(type: "integer", nullable: false),
                    squad_date = table.Column<DateOnly>(type: "date", nullable: true),
                    squad_id = table.Column<string>(type: "character(26)", fixedLength: true, maxLength: 26, nullable: true),
                    squad_start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_series_stages", x => x.id);
                    table.UniqueConstraint("ak_series_stages_domain_id", x => x.domain_id);
                    table.ForeignKey(
                        name: "fk_series_stages_series_series_id",
                        column: x => x.series_id,
                        principalSchema: "app",
                        principalTable: "series",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_series_stages_tournaments_tournament_id",
                        column: x => x.tournament_id,
                        principalSchema: "app",
                        principalTable: "tournaments",
                        principalColumn: "domain_id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_series_finale_bowling_center_id",
                schema: "app",
                table: "series",
                column: "finale_bowling_center_id");

            migrationBuilder.CreateIndex(
                name: "ix_series_season_id",
                schema: "app",
                table: "series",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "ix_series_stages_series_id_stage_order",
                schema: "app",
                table: "series_stages",
                columns: new[] { "series_id", "stage_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_series_stages_tournament_id",
                schema: "app",
                table: "series_stages",
                column: "tournament_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "series_stages",
                schema: "app");

            migrationBuilder.DropTable(
                name: "series",
                schema: "app");
        }
    }
}
