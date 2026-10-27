using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Neba.Api.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleAnnouncedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "announced_at_utc",
                schema: "app",
                table: "articles",
                type: "timestamp with time zone",
                nullable: true);

            // Articles already published must never be announced.
            migrationBuilder.Sql(
                """
                UPDATE app.articles
                SET announced_at_utc = publish_date_utc
                WHERE publication_status = 1;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "announced_at_utc",
                schema: "app",
                table: "articles");
        }
    }
}
