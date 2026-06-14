using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SizeGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_sizes_name",
                table: "sizes");

            migrationBuilder.AddColumn<int>(
                name: "size_group_id",
                table: "sizes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "size_groups",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_size_groups", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sizes_size_group_id_name",
                table: "sizes",
                columns: new[] { "size_group_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_size_groups_name",
                table: "size_groups",
                column: "name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_sizes_size_groups_size_group_id",
                table: "sizes",
                column: "size_group_id",
                principalTable: "size_groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_sizes_size_groups_size_group_id",
                table: "sizes");

            migrationBuilder.DropTable(
                name: "size_groups");

            migrationBuilder.DropIndex(
                name: "ix_sizes_size_group_id_name",
                table: "sizes");

            migrationBuilder.DropColumn(
                name: "size_group_id",
                table: "sizes");

            migrationBuilder.CreateIndex(
                name: "ix_sizes_name",
                table: "sizes",
                column: "name",
                unique: true);
        }
    }
}
