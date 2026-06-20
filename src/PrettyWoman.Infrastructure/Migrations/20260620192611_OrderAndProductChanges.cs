using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OrderAndProductChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_products_cost_at_sale_non_negative",
                table: "sale_products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_products_discount_amount_non_negative",
                table: "sale_products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_products_discount_amount_not_greater_than_original_sal~",
                table: "sale_products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_products_final_sale_price_non_negative",
                table: "sale_products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_products_gross_profit_matches_components",
                table: "sale_products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_products_original_sale_price_non_negative",
                table: "sale_products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_products_payment_comission_non_negative",
                table: "sale_products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_products_quantity_positive",
                table: "sale_products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_products_cost_non_negative",
                table: "products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_products_quantity_non_negative",
                table: "products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_products_sale_price_non_negative",
                table: "products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_products_unit_cost_with_shipping_non_negative",
                table: "products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_order_amount_non_negative",
                table: "orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_order_amount_usd_non_negative",
                table: "orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_order_exchange_rate_non_negative",
                table: "orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_order_received_amount_non_negative",
                table: "orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_order_total_shipping_cost_non_negative",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "cost_at_sale",
                table: "sale_products");

            migrationBuilder.DropColumn(
                name: "final_sale_price",
                table: "sale_products");

            migrationBuilder.DropColumn(
                name: "original_sale_price",
                table: "sale_products");

            migrationBuilder.DropColumn(
                name: "payment_comission",
                table: "sale_products");

            migrationBuilder.DropColumn(
                name: "unit_cost",
                table: "products");

            migrationBuilder.DropColumn(
                name: "unit_cost_with_shipping",
                table: "products");

            migrationBuilder.DropColumn(
                name: "amount",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "received_amount",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "total_shipping_cost",
                table: "orders");

            migrationBuilder.AlterColumn<decimal>(
                name: "gross_profit",
                table: "sale_products",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldPrecision: 12,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "discount_amount",
                table: "sale_products",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldPrecision: 12,
                oldScale: 2);

            migrationBuilder.AddColumn<decimal>(
                name: "final_unit_price",
                table: "sale_products",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "line_total",
                table: "sale_products",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "original_unit_price",
                table: "sale_products",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "total_cost_at_sale",
                table: "sale_products",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "total_payment_comission",
                table: "sale_products",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "unit_cost_at_sale",
                table: "sale_products",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "allocated_shipping_cost_nio",
                table: "products",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "merchandise_total_cost_nio",
                table: "products",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "total_cost_nio",
                table: "products",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "unit_cost_nio",
                table: "products",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "unit_cost_usd",
                table: "products",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "amount_usd",
                table: "orders",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldPrecision: 12,
                oldScale: 2);

            migrationBuilder.AddColumn<decimal>(
                name: "merchandise_total_nio",
                table: "orders",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "received_amount_nio",
                table: "orders",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "shipping_cost_nio",
                table: "orders",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "total_cost_nio",
                table: "orders",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_details_discount_amount_non_negative",
                table: "sale_products",
                sql: "discount_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_details_discount_amount_not_greater_than_original_unit~",
                table: "sale_products",
                sql: "discount_amount <= original_unit_price");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_details_final_unit_price_non_negative",
                table: "sale_products",
                sql: "final_unit_price >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_details_gross_profit_matches_components",
                table: "sale_products",
                sql: "gross_profit = line_total - total_payment_comission - total_cost_at_sale");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_details_line_total_non_negative",
                table: "sale_products",
                sql: "line_total >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_details_original_unit_price_non_negative",
                table: "sale_products",
                sql: "original_unit_price >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_details_quantity_positive",
                table: "sale_products",
                sql: "quantity > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_details_total_cost_at_sale_non_negative",
                table: "sale_products",
                sql: "total_cost_at_sale >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_details_total_payment_comission_non_negative",
                table: "sale_products",
                sql: "total_payment_comission >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_details_unit_cost_at_sale_non_negative",
                table: "sale_products",
                sql: "unit_cost_at_sale >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_allocated_shipping_cost_nio_non_negative",
                table: "products",
                sql: "allocated_shipping_cost_nio >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_available_reserved_not_greater_than_received",
                table: "products",
                sql: "available_quantity + reserved_quantity <= received_quantity");

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_merchandise_total_cost_nio_non_negative",
                table: "products",
                sql: "merchandise_total_cost_nio >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_quantity_positive",
                table: "products",
                sql: "quantity > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_received_quantity_not_greater_than_quantity",
                table: "products",
                sql: "received_quantity <= quantity");

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_sale_price_positive",
                table: "products",
                sql: "sale_price >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_total_cost_nio_non_negative",
                table: "products",
                sql: "total_cost_nio >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_unit_cost_nio_non_negative",
                table: "products",
                sql: "unit_cost_nio >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_unit_cost_usd_non_negative",
                table: "products",
                sql: "unit_cost_usd >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_orders_amount_usd_non_negative",
                table: "orders",
                sql: "amount_usd >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_orders_exchange_rate_positive",
                table: "orders",
                sql: "exchange_rate > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_orders_merchandise_total_nio_non_negative",
                table: "orders",
                sql: "merchandise_total_nio >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_orders_received_amount_nio_non_negative",
                table: "orders",
                sql: "received_amount_nio >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_orders_shipping_cost_nio_non_negative",
                table: "orders",
                sql: "shipping_cost_nio >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_orders_total_cost_nio_non_negative",
                table: "orders",
                sql: "total_cost_nio >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_details_discount_amount_non_negative",
                table: "sale_products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_details_discount_amount_not_greater_than_original_unit~",
                table: "sale_products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_details_final_unit_price_non_negative",
                table: "sale_products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_details_gross_profit_matches_components",
                table: "sale_products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_details_line_total_non_negative",
                table: "sale_products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_details_original_unit_price_non_negative",
                table: "sale_products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_details_quantity_positive",
                table: "sale_products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_details_total_cost_at_sale_non_negative",
                table: "sale_products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_details_total_payment_comission_non_negative",
                table: "sale_products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_details_unit_cost_at_sale_non_negative",
                table: "sale_products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_products_allocated_shipping_cost_nio_non_negative",
                table: "products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_products_available_reserved_not_greater_than_received",
                table: "products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_products_merchandise_total_cost_nio_non_negative",
                table: "products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_products_quantity_positive",
                table: "products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_products_received_quantity_not_greater_than_quantity",
                table: "products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_products_sale_price_positive",
                table: "products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_products_total_cost_nio_non_negative",
                table: "products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_products_unit_cost_nio_non_negative",
                table: "products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_products_unit_cost_usd_non_negative",
                table: "products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_orders_amount_usd_non_negative",
                table: "orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_orders_exchange_rate_positive",
                table: "orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_orders_merchandise_total_nio_non_negative",
                table: "orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_orders_received_amount_nio_non_negative",
                table: "orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_orders_shipping_cost_nio_non_negative",
                table: "orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_orders_total_cost_nio_non_negative",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "final_unit_price",
                table: "sale_products");

            migrationBuilder.DropColumn(
                name: "line_total",
                table: "sale_products");

            migrationBuilder.DropColumn(
                name: "original_unit_price",
                table: "sale_products");

            migrationBuilder.DropColumn(
                name: "total_cost_at_sale",
                table: "sale_products");

            migrationBuilder.DropColumn(
                name: "total_payment_comission",
                table: "sale_products");

            migrationBuilder.DropColumn(
                name: "unit_cost_at_sale",
                table: "sale_products");

            migrationBuilder.DropColumn(
                name: "allocated_shipping_cost_nio",
                table: "products");

            migrationBuilder.DropColumn(
                name: "merchandise_total_cost_nio",
                table: "products");

            migrationBuilder.DropColumn(
                name: "total_cost_nio",
                table: "products");

            migrationBuilder.DropColumn(
                name: "unit_cost_nio",
                table: "products");

            migrationBuilder.DropColumn(
                name: "unit_cost_usd",
                table: "products");

            migrationBuilder.DropColumn(
                name: "merchandise_total_nio",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "received_amount_nio",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "shipping_cost_nio",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "total_cost_nio",
                table: "orders");

            migrationBuilder.AlterColumn<decimal>(
                name: "gross_profit",
                table: "sale_products",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)",
                oldPrecision: 18,
                oldScale: 6);

            migrationBuilder.AlterColumn<decimal>(
                name: "discount_amount",
                table: "sale_products",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(14,2)",
                oldPrecision: 14,
                oldScale: 2);

            migrationBuilder.AddColumn<decimal>(
                name: "cost_at_sale",
                table: "sale_products",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "final_sale_price",
                table: "sale_products",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "original_sale_price",
                table: "sale_products",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "payment_comission",
                table: "sale_products",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "unit_cost",
                table: "products",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "unit_cost_with_shipping",
                table: "products",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "amount_usd",
                table: "orders",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(14,2)",
                oldPrecision: 14,
                oldScale: 2);

            migrationBuilder.AddColumn<decimal>(
                name: "amount",
                table: "orders",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "received_amount",
                table: "orders",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "total_shipping_cost",
                table: "orders",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_products_cost_at_sale_non_negative",
                table: "sale_products",
                sql: "cost_at_sale >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_products_discount_amount_non_negative",
                table: "sale_products",
                sql: "discount_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_products_discount_amount_not_greater_than_original_sal~",
                table: "sale_products",
                sql: "discount_amount <= original_sale_price");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_products_final_sale_price_non_negative",
                table: "sale_products",
                sql: "final_sale_price >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_products_gross_profit_matches_components",
                table: "sale_products",
                sql: "gross_profit = final_sale_price - payment_comission - cost_at_sale");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_products_original_sale_price_non_negative",
                table: "sale_products",
                sql: "original_sale_price >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_products_payment_comission_non_negative",
                table: "sale_products",
                sql: "payment_comission >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_products_quantity_positive",
                table: "sale_products",
                sql: "quantity > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_cost_non_negative",
                table: "products",
                sql: "unit_cost >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_quantity_non_negative",
                table: "products",
                sql: "quantity >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_sale_price_non_negative",
                table: "products",
                sql: "sale_price >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_unit_cost_with_shipping_non_negative",
                table: "products",
                sql: "unit_cost_with_shipping >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_order_amount_non_negative",
                table: "orders",
                sql: "amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_order_amount_usd_non_negative",
                table: "orders",
                sql: "amount_usd >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_order_exchange_rate_non_negative",
                table: "orders",
                sql: "exchange_rate >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_order_received_amount_non_negative",
                table: "orders",
                sql: "received_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_order_total_shipping_cost_non_negative",
                table: "orders",
                sql: "total_shipping_cost >= 0");
        }
    }
}
