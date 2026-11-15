using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Neba.Api.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddBowlerUsbcId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "usbc_id",
                schema: "app",
                table: "bowlers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_bowlers_usbc_id",
                schema: "app",
                table: "bowlers",
                column: "usbc_id",
                unique: true,
                filter: "usbc_id IS NOT NULL AND usbc_id <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_bowlers_usbc_id",
                schema: "app",
                table: "bowlers");

            migrationBuilder.DropColumn(
                name: "usbc_id",
                schema: "app",
                table: "bowlers");
        }
    }
}
