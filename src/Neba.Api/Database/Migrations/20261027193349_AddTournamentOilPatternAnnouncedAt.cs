using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Neba.Api.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddTournamentOilPatternAnnouncedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "oil_pattern_announced_at_utc",
                schema: "app",
                table: "tournaments",
                type: "timestamp with time zone",
                nullable: true);

            // Tournaments that already have a reveal date must never announce.
            migrationBuilder.Sql(
                """
                UPDATE app.tournaments
                SET oil_pattern_announced_at_utc = oil_pattern_reveal_date_utc
                WHERE oil_pattern_reveal_date_utc IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "oil_pattern_announced_at_utc",
                schema: "app",
                table: "tournaments");
        }
    }
}
