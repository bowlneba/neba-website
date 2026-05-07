using Microsoft.EntityFrameworkCore.Migrations;

using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Neba.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class BowlingCenters_Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "app");

            migrationBuilder.CreateTable(
                name: "bowling_centers",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    certification_number = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    legacy_id = table.Column<int>(type: "integer", nullable: true),
                    website_id = table.Column<int>(type: "integer", nullable: true),
                    name = table.Column<string>(type: "character varying(127)", maxLength: 127, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    website = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    email_address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    street = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    city = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    state = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: false),
                    postal_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    country = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bowling_centers", x => x.id);
                    table.UniqueConstraint("ak_bowling_centers_certification_number", x => x.certification_number);
                });

            migrationBuilder.CreateTable(
                name: "bowling_center_lanes",
                schema: "app",
                columns: table => new
                {
                    bowling_center_id = table.Column<int>(type: "integer", nullable: false),
                    start_lane = table.Column<int>(type: "integer", nullable: false),
                    end_lane = table.Column<int>(type: "integer", nullable: false),
                    pin_fall_type = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bowling_center_lanes", x => new { x.bowling_center_id, x.start_lane });
                    table.ForeignKey(
                        name: "fk_bowling_center_lanes_bowling_centers_bowling_center_id",
                        column: x => x.bowling_center_id,
                        principalSchema: "app",
                        principalTable: "bowling_centers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bowling_center_phone_numbers",
                schema: "app",
                columns: table => new
                {
                    bowling_center_id = table.Column<int>(type: "integer", nullable: false),
                    phone_type = table.Column<string>(type: "character(1)", fixedLength: true, maxLength: 1, nullable: false),
                    phone_country_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    phone_extension = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bowling_center_phone_numbers", x => new { x.bowling_center_id, x.phone_type });
                    table.ForeignKey(
                        name: "fk_bowling_center_phone_numbers_bowling_centers_bowling_center",
                        column: x => x.bowling_center_id,
                        principalSchema: "app",
                        principalTable: "bowling_centers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bowling_centers_legacy_id",
                schema: "app",
                table: "bowling_centers",
                column: "legacy_id",
                unique: true)
                .Annotation("Npgsql:NullsDistinct", true);

            migrationBuilder.CreateIndex(
                name: "ix_bowling_centers_website_id",
                schema: "app",
                table: "bowling_centers",
                column: "website_id",
                unique: true)
                .Annotation("Npgsql:NullsDistinct", true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bowling_center_lanes",
                schema: "app");

            migrationBuilder.DropTable(
                name: "bowling_center_phone_numbers",
                schema: "app");

            migrationBuilder.DropTable(
                name: "bowling_centers",
                schema: "app");
        }
    }
}
