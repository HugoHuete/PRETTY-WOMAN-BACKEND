using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SaleDeliveryChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "amount_transferred_nio",
                table: "sale_deliveries");

            migrationBuilder.DropColumn(
                name: "amount_transferred_usd",
                table: "sale_deliveries");

            migrationBuilder.DropColumn(
                name: "last_purchase_date",
                table: "clients");

            migrationBuilder.AddColumn<string>(
                name: "messenger_user",
                table: "clients",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_clients_messenger_user",
                table: "clients",
                column: "messenger_user",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_clients_messenger_user",
                table: "clients");

            migrationBuilder.DropColumn(
                name: "messenger_user",
                table: "clients");

            migrationBuilder.AddColumn<decimal>(
                name: "amount_transferred_nio",
                table: "sale_deliveries",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "amount_transferred_usd",
                table: "sale_deliveries",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_purchase_date",
                table: "clients",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
