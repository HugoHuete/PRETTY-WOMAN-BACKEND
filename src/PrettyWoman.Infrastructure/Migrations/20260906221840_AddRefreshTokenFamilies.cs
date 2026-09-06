using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenFamilies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "family_id",
                table: "refresh_tokens",
                type: "uuid",
                nullable: true);

            // Los tokens existentes no pertenecían a una familia. Su propio Id crea
            // una familia aislada y evita que una sesión vieja afecte a otra.
            migrationBuilder.Sql("UPDATE refresh_tokens SET family_id = id WHERE family_id IS NULL;");

            migrationBuilder.AlterColumn<Guid>(
                name: "family_id",
                table: "refresh_tokens",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "replacement_token_protected",
                table: "refresh_tokens",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "replay_until_utc",
                table: "refresh_tokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_family_id",
                table: "refresh_tokens",
                column: "family_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_refresh_tokens_family_id",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "family_id",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "replacement_token_protected",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "replay_until_utc",
                table: "refresh_tokens");
        }
    }
}
