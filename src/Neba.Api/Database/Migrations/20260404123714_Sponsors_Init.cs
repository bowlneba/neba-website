using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Neba.Api.Database.Migrations
{
    /// <inheritdoc />
    public partial class Sponsors_Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sponsors",
                schema: "app",
                columns: table => new
                {
                    // Keys
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    domain_id = table.Column<string>(type: "character(26)", fixedLength: true, maxLength: 26, nullable: false),
                    // Identity
                    name = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: false),
                    slug = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: false),
                    // Classification
                    current_sponsor = table.Column<bool>(type: "boolean", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    tier = table.Column<int>(type: "integer", nullable: false),
                    category = table.Column<int>(type: "integer", nullable: false),
                    // Public content
                    website_url = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    tag_phrase = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    description = table.Column<string>(type: "character varying(1023)", maxLength: 1023, nullable: true),
                    facebook_url = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    instagram_url = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    // Admin-only content
                    live_read_text = table.Column<string>(type: "character varying(2047)", maxLength: 2047, nullable: true),
                    promotional_notes = table.Column<string>(type: "character varying(4095)", maxLength: 4095, nullable: true),
                    // Business address
                    business_street = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    business_unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    business_city = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    business_region = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: true),
                    business_postal_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    business_country = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: true),
                    business_latitude = table.Column<double>(type: "double precision", nullable: true),
                    business_longitude = table.Column<double>(type: "double precision", nullable: true),
                    business_email_address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    // Sponsor contact
                    contact_name = table.Column<string>(type: "character varying(127)", maxLength: 127, nullable: true),
                    contact_email_address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    contact_phone_type = table.Column<string>(type: "character(1)", fixedLength: true, maxLength: 1, nullable: true),
                    contact_phone_country_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    contact_phone_number = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    contact_phone_extension = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    // Logo (stored file)
                    logo_container_name = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: true),
                    logo_file_path = table.Column<string>(type: "character varying(1023)", maxLength: 1023, nullable: true),
                    logo_content_type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    logo_size_in_bytes = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sponsors", x => x.id);
                    table.UniqueConstraint("ak_sponsors_domain_id", x => x.domain_id);
                    table.UniqueConstraint("ak_sponsors_slug", x => x.slug);
                });

            migrationBuilder.CreateTable(
                name: "sponsor_phone_numbers",
                schema: "app",
                columns: table => new
                {
                    sponsor_id = table.Column<int>(type: "integer", nullable: false),
                    phone_type = table.Column<string>(type: "character(1)", fixedLength: true, maxLength: 1, nullable: false),
                    phone_country_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    phone_extension = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sponsor_phone_numbers", x => new { x.sponsor_id, x.phone_type });
                    table.ForeignKey(
                        name: "fk_sponsor_phone_numbers_sponsors_sponsor_id",
                        column: x => x.sponsor_id,
                        principalSchema: "app",
                        principalTable: "sponsors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sponsor_phone_numbers",
                schema: "app");

            migrationBuilder.DropTable(
                name: "sponsors",
                schema: "app");
        }
    }
}
