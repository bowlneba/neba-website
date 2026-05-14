using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Neba.Api.Database.Migrations
{
    /// <inheritdoc />
    public partial class OilPatterns_Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "oil_patterns",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    domain_id = table.Column<string>(type: "character(26)", fixedLength: true, maxLength: 26, nullable: false),
                    name = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: false),
                    length = table.Column<int>(type: "integer", nullable: false),
                    volume = table.Column<decimal>(type: "numeric(5,3)", precision: 5, scale: 3, nullable: false),
                    left_ratio = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                    right_ratio = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                    kegel_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_oil_patterns", x => x.id);
                    table.UniqueConstraint("ak_oil_patterns_domain_id", x => x.domain_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_oil_patterns_kegel_id",
                schema: "app",
                table: "oil_patterns",
                column: "kegel_id",
                unique: true)
                .Annotation("Npgsql:NullsDistinct", true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "oil_patterns",
                schema: "app");
        }
    }
}
