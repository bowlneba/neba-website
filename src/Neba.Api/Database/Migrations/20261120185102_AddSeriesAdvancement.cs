using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Neba.Api.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddSeriesAdvancement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "computed_at_utc",
                schema: "app",
                table: "series_stages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_stale",
                schema: "app",
                table: "series_stages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "official_at_utc",
                schema: "app",
                table: "series_stages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "slot_count",
                schema: "app",
                table: "series_stages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "backfill_as_of",
                schema: "app",
                table: "series",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "version",
                schema: "app",
                table: "series",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "series_field_entries",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    domain_id = table.Column<string>(type: "character(26)", fixedLength: true, maxLength: 26, nullable: false),
                    bowler_id = table.Column<string>(type: "character(26)", fixedLength: true, maxLength: 26, nullable: false),
                    source_stage_id = table.Column<string>(type: "character(26)", fixedLength: true, maxLength: 26, nullable: true),
                    place = table.Column<int>(type: "integer", nullable: true),
                    backfill_points = table.Column<int>(type: "integer", nullable: true),
                    via_override = table.Column<bool>(type: "boolean", nullable: false),
                    series_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_series_field_entries", x => x.id);
                    table.UniqueConstraint("ak_series_field_entries_domain_id", x => x.domain_id);
                    table.CheckConstraint("ck_series_field_entries_backfill_points_not_negative", "backfill_points IS NULL OR backfill_points >= 0");
                    table.CheckConstraint("ck_series_field_entries_place_positive", "place IS NULL OR place > 0");
                    table.CheckConstraint("ck_series_field_entries_source_shape", "(source_stage_id IS NOT NULL AND place IS NOT NULL AND backfill_points IS NULL) OR (source_stage_id IS NULL AND place IS NULL AND backfill_points IS NOT NULL AND via_override = FALSE)");
                    table.ForeignKey(
                        name: "fk_series_field_entries_bowlers_bowler_id",
                        column: x => x.bowler_id,
                        principalSchema: "app",
                        principalTable: "bowlers",
                        principalColumn: "domain_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_series_field_entries_series_series_id",
                        column: x => x.series_id,
                        principalSchema: "app",
                        principalTable: "series",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_series_field_entries_series_stages_source_stage_id",
                        column: x => x.source_stage_id,
                        principalSchema: "app",
                        principalTable: "series_stages",
                        principalColumn: "domain_id");
                });

            migrationBuilder.CreateTable(
                name: "series_positions",
                schema: "app",
                columns: table => new
                {
                    bowler_id = table.Column<string>(type: "character(26)", fixedLength: true, maxLength: 26, nullable: false),
                    series_id = table.Column<int>(type: "integer", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    bowled = table.Column<bool>(type: "boolean", nullable: false),
                    is_finalist = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_series_positions", x => new { x.series_id, x.bowler_id });
                    table.CheckConstraint("ck_series_positions_position_positive", "position > 0");
                    table.ForeignKey(
                        name: "fk_series_positions_bowlers_bowler_id",
                        column: x => x.bowler_id,
                        principalSchema: "app",
                        principalTable: "bowlers",
                        principalColumn: "domain_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_series_positions_series_series_id",
                        column: x => x.series_id,
                        principalSchema: "app",
                        principalTable: "series",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "series_stage_overrides",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    domain_id = table.Column<string>(type: "character(26)", fixedLength: true, maxLength: 26, nullable: false),
                    bowler_id = table.Column<string>(type: "character(26)", fixedLength: true, maxLength: 26, nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    applied = table.Column<bool>(type: "boolean", nullable: false),
                    series_stage_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_series_stage_overrides", x => x.id);
                    table.UniqueConstraint("ak_series_stage_overrides_domain_id", x => x.domain_id);
                    table.ForeignKey(
                        name: "fk_series_stage_overrides_bowlers_bowler_id",
                        column: x => x.bowler_id,
                        principalSchema: "app",
                        principalTable: "bowlers",
                        principalColumn: "domain_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_series_stage_overrides_series_stages_series_stage_id",
                        column: x => x.series_stage_id,
                        principalSchema: "app",
                        principalTable: "series_stages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "series_survivor_outcomes",
                schema: "app",
                columns: table => new
                {
                    bowler_id = table.Column<string>(type: "character(26)", fixedLength: true, maxLength: 26, nullable: false),
                    series_stage_id = table.Column<int>(type: "integer", nullable: false),
                    block_total = table.Column<int>(type: "integer", nullable: true),
                    advanced = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_series_survivor_outcomes", x => new { x.series_stage_id, x.bowler_id });
                    table.CheckConstraint("ck_series_survivor_outcomes_block_total_not_negative", "block_total IS NULL OR block_total >= 0");
                    table.ForeignKey(
                        name: "fk_series_survivor_outcomes_bowlers_bowler_id",
                        column: x => x.bowler_id,
                        principalSchema: "app",
                        principalTable: "bowlers",
                        principalColumn: "domain_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_series_survivor_outcomes_series_stages_series_stage_id",
                        column: x => x.series_stage_id,
                        principalSchema: "app",
                        principalTable: "series_stages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_series_stages_placement_never_official",
                schema: "app",
                table: "series_stages",
                sql: "NOT (kind = 3 AND status = 2)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_series_stages_slot_count_not_negative",
                schema: "app",
                table: "series_stages",
                sql: "slot_count IS NULL OR slot_count >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_series_stages_status_timestamps",
                schema: "app",
                table: "series_stages",
                sql: "(status = 0 AND computed_at_utc IS NULL AND official_at_utc IS NULL) OR (status = 1 AND computed_at_utc IS NOT NULL AND official_at_utc IS NULL) OR (status = 2 AND computed_at_utc IS NOT NULL AND official_at_utc IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "ix_series_field_entries_bowler_id",
                schema: "app",
                table: "series_field_entries",
                column: "bowler_id");

            migrationBuilder.CreateIndex(
                name: "ix_series_field_entries_series_id_bowler_id",
                schema: "app",
                table: "series_field_entries",
                columns: new[] { "series_id", "bowler_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_series_field_entries_source_stage_id",
                schema: "app",
                table: "series_field_entries",
                column: "source_stage_id");

            migrationBuilder.CreateIndex(
                name: "ix_series_positions_bowler_id",
                schema: "app",
                table: "series_positions",
                column: "bowler_id");

            migrationBuilder.CreateIndex(
                name: "ix_series_positions_series_id_position",
                schema: "app",
                table: "series_positions",
                columns: new[] { "series_id", "position" });

            migrationBuilder.CreateIndex(
                name: "ix_series_stage_overrides_bowler_id",
                schema: "app",
                table: "series_stage_overrides",
                column: "bowler_id");

            migrationBuilder.CreateIndex(
                name: "ix_series_stage_overrides_series_stage_id_bowler_id",
                schema: "app",
                table: "series_stage_overrides",
                columns: new[] { "series_stage_id", "bowler_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_series_survivor_outcomes_bowler_id",
                schema: "app",
                table: "series_survivor_outcomes",
                column: "bowler_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "series_field_entries",
                schema: "app");

            migrationBuilder.DropTable(
                name: "series_positions",
                schema: "app");

            migrationBuilder.DropTable(
                name: "series_stage_overrides",
                schema: "app");

            migrationBuilder.DropTable(
                name: "series_survivor_outcomes",
                schema: "app");

            migrationBuilder.DropCheckConstraint(
                name: "ck_series_stages_placement_never_official",
                schema: "app",
                table: "series_stages");

            migrationBuilder.DropCheckConstraint(
                name: "ck_series_stages_slot_count_not_negative",
                schema: "app",
                table: "series_stages");

            migrationBuilder.DropCheckConstraint(
                name: "ck_series_stages_status_timestamps",
                schema: "app",
                table: "series_stages");

            migrationBuilder.DropColumn(
                name: "computed_at_utc",
                schema: "app",
                table: "series_stages");

            migrationBuilder.DropColumn(
                name: "is_stale",
                schema: "app",
                table: "series_stages");

            migrationBuilder.DropColumn(
                name: "official_at_utc",
                schema: "app",
                table: "series_stages");

            migrationBuilder.DropColumn(
                name: "slot_count",
                schema: "app",
                table: "series_stages");

            migrationBuilder.DropColumn(
                name: "backfill_as_of",
                schema: "app",
                table: "series");

            migrationBuilder.DropColumn(
                name: "version",
                schema: "app",
                table: "series");
        }
    }
}
