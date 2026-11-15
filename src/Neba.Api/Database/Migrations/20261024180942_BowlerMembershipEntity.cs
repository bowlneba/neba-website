using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Neba.Api.Database.Migrations
{
    /// <inheritdoc />
    public partial class BowlerMembershipEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bowler_memberships",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    domain_id = table.Column<string>(type: "character(26)", fixedLength: true, maxLength: 26, nullable: false),
                    bowler_id = table.Column<int>(type: "integer", nullable: false),
                    legacy_id = table.Column<int>(type: "integer", nullable: true),
                    membership_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    cost = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bowler_memberships", x => x.id);
                    table.UniqueConstraint("ak_bowler_memberships_domain_id", x => x.domain_id);
                    table.ForeignKey(
                        name: "fk_bowler_memberships_bowlers_bowler_id",
                        column: x => x.bowler_id,
                        principalSchema: "app",
                        principalTable: "bowlers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bowler_memberships_bowler_id_start_date",
                schema: "app",
                table: "bowler_memberships",
                columns: new[] { "bowler_id", "start_date" });

            migrationBuilder.CreateIndex(
                name: "ix_bowler_memberships_legacy_id",
                schema: "app",
                table: "bowler_memberships",
                column: "legacy_id",
                unique: true)
                .Annotation("Npgsql:NullsDistinct", true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bowler_memberships",
                schema: "app");
        }
    }
}
