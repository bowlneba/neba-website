using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Neba.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class SideCut_Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "side_cuts",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    domain_id = table.Column<string>(type: "character(26)", fixedLength: true, maxLength: 26, nullable: false),
                    name = table.Column<string>(type: "character varying(31)", maxLength: 31, nullable: false),
                    color_indicator = table.Column<int>(type: "integer", nullable: false),
                    logical_operator = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_side_cuts", x => x.id);
                    table.UniqueConstraint("ak_side_cuts_domain_id", x => x.domain_id);
                });

            migrationBuilder.CreateTable(
                name: "side_cut_criteria_groups",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    domain_id = table.Column<string>(type: "character(26)", fixedLength: true, maxLength: 26, nullable: false),
                    side_cut_id = table.Column<int>(type: "integer", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    logical_operator = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_side_cut_criteria_groups", x => x.id);
                    table.UniqueConstraint("ak_side_cut_criteria_groups_domain_id", x => x.domain_id);
                    table.ForeignKey(
                        name: "fk_side_cut_criteria_groups_side_cuts_side_cut_id",
                        column: x => x.side_cut_id,
                        principalSchema: "app",
                        principalTable: "side_cuts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "side_cut_criteria",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    side_cut_criteria_group_id = table.Column<int>(type: "integer", nullable: false),
                    minimum_age = table.Column<int>(type: "integer", nullable: true),
                    maximum_age = table.Column<int>(type: "integer", nullable: true),
                    gender_requirement = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_side_cut_criteria", x => x.id);
                    table.ForeignKey(
                        name: "fk_side_cut_criteria_side_cut_criteria_groups_side_cut_criteri",
                        column: x => x.side_cut_criteria_group_id,
                        principalSchema: "app",
                        principalTable: "side_cut_criteria_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_side_cut_criteria_side_cut_criteria_group_id",
                schema: "app",
                table: "side_cut_criteria",
                column: "side_cut_criteria_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_side_cut_criteria_groups_side_cut_id_sort_order",
                schema: "app",
                table: "side_cut_criteria_groups",
                columns: new[] { "side_cut_id", "sort_order" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "side_cut_criteria",
                schema: "app");

            migrationBuilder.DropTable(
                name: "side_cut_criteria_groups",
                schema: "app");

            migrationBuilder.DropTable(
                name: "side_cuts",
                schema: "app");
        }
    }
}
