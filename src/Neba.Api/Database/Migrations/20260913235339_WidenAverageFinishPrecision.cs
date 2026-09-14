using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Neba.Api.Database.Migrations
{
    /// <inheritdoc />
    public partial class WidenAverageFinishPrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "average_finish",
                schema: "app",
                table: "bowler_season_stats",
                type: "numeric(5,1)",
                precision: 5,
                scale: 1,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(3,1)",
                oldPrecision: 3,
                oldScale: 1,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "average_finish",
                schema: "app",
                table: "bowler_season_stats",
                type: "numeric(3,1)",
                precision: 3,
                scale: 1,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,1)",
                oldPrecision: 5,
                oldScale: 1,
                oldNullable: true);
        }
    }
}
