using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Neba.Api.Database.Migrations
{
    /// <inheritdoc />
    public partial class Articles_Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "articles",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    domain_id = table.Column<string>(type: "character(26)", fixedLength: true, maxLength: 26, nullable: false),
                    title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    slug = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    publication_status = table.Column<int>(type: "integer", nullable: false),
                    publish_date_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tournament_id = table.Column<string>(type: "character(26)", nullable: true),
                    header_image_container = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: true),
                    header_image_content_type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    header_image_file_path = table.Column<string>(type: "character varying(1023)", maxLength: 1023, nullable: true),
                    header_image_size_in_bytes = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_articles", x => x.id);
                    table.UniqueConstraint("ak_articles_domain_id", x => x.domain_id);
                    table.ForeignKey(
                        name: "fk_articles_tournaments_tournament_id",
                        column: x => x.tournament_id,
                        principalSchema: "app",
                        principalTable: "tournaments",
                        principalColumn: "domain_id");
                });

            migrationBuilder.CreateTable(
                name: "article_attachments",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    domain_id = table.Column<string>(type: "character(26)", fixedLength: true, maxLength: 26, nullable: false),
                    article_id = table.Column<int>(type: "integer", nullable: false),
                    display_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    is_inline = table.Column<bool>(type: "boolean", nullable: false),
                    file_container = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: false),
                    file_content_type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    file_path = table.Column<string>(type: "character varying(1023)", maxLength: 1023, nullable: false),
                    file_size_in_bytes = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_article_attachments", x => x.id);
                    table.ForeignKey(
                        name: "fk_article_attachments_articles_article_id",
                        column: x => x.article_id,
                        principalSchema: "app",
                        principalTable: "articles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_article_attachments_article_id",
                schema: "app",
                table: "article_attachments",
                column: "article_id");

            migrationBuilder.CreateIndex(
                name: "ix_article_attachments_domain_id",
                schema: "app",
                table: "article_attachments",
                column: "domain_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_articles_publication_status_publish_date_utc",
                schema: "app",
                table: "articles",
                columns: new[] { "publication_status", "publish_date_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_articles_slug",
                schema: "app",
                table: "articles",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_articles_tournament_id",
                schema: "app",
                table: "articles",
                column: "tournament_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "article_attachments",
                schema: "app");

            migrationBuilder.DropTable(
                name: "articles",
                schema: "app");
        }
    }
}
