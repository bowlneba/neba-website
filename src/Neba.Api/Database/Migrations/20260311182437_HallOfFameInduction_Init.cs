using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Neba.Api.Database.Migrations
{
    /// <inheritdoc />
    public partial class HallOfFameInduction_Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "hall_of_fame_inductions",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    domain_id = table.Column<string>(type: "character(26)", fixedLength: true, maxLength: 26, nullable: false),
                    induction_year = table.Column<int>(type: "integer", nullable: false),
                    bowler_id = table.Column<string>(type: "character(26)", fixedLength: true, maxLength: 26, nullable: false),
                    category = table.Column<int>(type: "integer", nullable: false),
                    photo_container = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: true),
                    photo_content_type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    photo_file_path = table.Column<string>(type: "character varying(1023)", maxLength: 1023, nullable: true),
                    photo_size_in_bytes = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hall_of_fame_inductions", x => x.id);
                    table.UniqueConstraint("ak_hall_of_fame_inductions_domain_id", x => x.domain_id);
                    table.UniqueConstraint("ak_hall_of_fame_inductions_induction_year_bowler_id", x => new { x.induction_year, x.bowler_id });
                    table.ForeignKey(
                        name: "fk_hall_of_fame_inductions_bowlers_bowler_id",
                        column: x => x.bowler_id,
                        principalSchema: "app",
                        principalTable: "bowlers",
                        principalColumn: "domain_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_hall_of_fame_inductions_bowler_id",
                schema: "app",
                table: "hall_of_fame_inductions",
                column: "bowler_id");

            migrationBuilder.CreateIndex(
                name: "ix_hall_of_fame_inductions_induction_year",
                schema: "app",
                table: "hall_of_fame_inductions",
                column: "induction_year");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "hall_of_fame_inductions",
                schema: "app");
        }
    }
}
