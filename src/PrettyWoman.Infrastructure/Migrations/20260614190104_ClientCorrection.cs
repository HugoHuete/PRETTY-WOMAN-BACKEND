using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ClientCorrection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_clients_instagram_user",
                table: "clients");

            migrationBuilder.DropIndex(
                name: "ix_clients_phone_number",
                table: "clients");

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_purchase_date",
                table: "clients",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.CreateIndex(
                name: "ix_clients_instagram_user",
                table: "clients",
                column: "instagram_user",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_clients_phone_number",
                table: "clients",
                column: "phone_number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_clients_instagram_user",
                table: "clients");

            migrationBuilder.DropIndex(
                name: "ix_clients_phone_number",
                table: "clients");

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_purchase_date",
                table: "clients",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_clients_instagram_user",
                table: "clients",
                column: "instagram_user");

            migrationBuilder.CreateIndex(
                name: "ix_clients_phone_number",
                table: "clients",
                column: "phone_number");
        }
    }
}
