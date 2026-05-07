using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Neba.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class Tournaments_StatsEligible : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "stats_eligible",
                schema: "app",
                table: "tournaments",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "stats_eligible",
                schema: "app",
                table: "tournaments");
        }
    }
}
