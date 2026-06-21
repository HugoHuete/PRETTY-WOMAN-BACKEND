using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PurchaseChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_orders_shipping_cost_nio_non_negative",
                table: "orders");

            migrationBuilder.RenameColumn(
                name: "shipping_cost_nio",
                table: "orders",
                newName: "warehouse_shipping_cost_usd");

            migrationBuilder.AddColumn<int>(
                name: "purchase_currency_id",
                table: "orders",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<decimal>(
                name: "supplier_shipping_cost_usd",
                table: "orders",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddCheckConstraint(
                name: "ck_orders_purchase_currency_valid",
                table: "orders",
                sql: "purchase_currency_id IN (1, 2)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_orders_supplier_shipping_cost_usd_non_negative",
                table: "orders",
                sql: "supplier_shipping_cost_usd >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_orders_warehouse_shipping_cost_usd_non_negative",
                table: "orders",
                sql: "warehouse_shipping_cost_usd >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_orders_purchase_currency_valid",
                table: "orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_orders_supplier_shipping_cost_usd_non_negative",
                table: "orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_orders_warehouse_shipping_cost_usd_non_negative",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "purchase_currency_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "supplier_shipping_cost_usd",
                table: "orders");

            migrationBuilder.RenameColumn(
                name: "warehouse_shipping_cost_usd",
                table: "orders",
                newName: "shipping_cost_nio");

            migrationBuilder.AddCheckConstraint(
                name: "ck_orders_shipping_cost_nio_non_negative",
                table: "orders",
                sql: "shipping_cost_nio >= 0");
        }
    }
}
