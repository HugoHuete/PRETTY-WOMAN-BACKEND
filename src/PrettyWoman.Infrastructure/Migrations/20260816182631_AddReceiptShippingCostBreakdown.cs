using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiptShippingCostBreakdown : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "warehouse_shipping_cost_nio",
                table: "product_receipts",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "warehouse_shipping_cost_usd",
                table: "product_receipts",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "allocated_warehouse_shipping_cost_nio",
                table: "product_receipt_details",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "weight",
                table: "product_receipt_details",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.Sql("""
                UPDATE product_receipts AS receipt
                SET warehouse_shipping_cost_nio = COALESCE((
                        SELECT SUM(movement.amount)
                        FROM financial_movements AS movement
                        WHERE movement.product_receipt_id = receipt.id
                          AND movement.financial_movement_type_id = 10
                    ), 0),
                    warehouse_shipping_cost_usd = CASE
                        WHEN (SELECT exchange_rate FROM orders WHERE id = receipt.order_id) > 0
                            THEN ROUND(
                                COALESCE((
                                    SELECT SUM(movement.amount)
                                    FROM financial_movements AS movement
                                    WHERE movement.product_receipt_id = receipt.id
                                      AND movement.financial_movement_type_id = 10
                                ), 0)
                                / (SELECT exchange_rate FROM orders WHERE id = receipt.order_id),
                                2)
                        ELSE 0
                    END
                WHERE EXISTS (SELECT 1 FROM orders WHERE id = receipt.order_id);

                WITH detail_data AS (
                    SELECT
                        detail.id,
                        detail.product_receipt_id,
                        detail.quantity,
                        receipt.warehouse_shipping_cost_nio,
                        ROW_NUMBER() OVER (PARTITION BY detail.product_receipt_id ORDER BY detail.id) AS row_number,
                        COUNT(*) OVER (PARTITION BY detail.product_receipt_id) AS row_count,
                        SUM(detail.quantity) OVER (PARTITION BY detail.product_receipt_id) AS total_quantity
                    FROM product_receipt_details AS detail
                    INNER JOIN product_receipts AS receipt ON receipt.id = detail.product_receipt_id
                ), rounded_data AS (
                    SELECT
                        detail_data.*,
                        CASE
                            WHEN detail_data.total_quantity = 0 THEN 0
                            ELSE ROUND(detail_data.warehouse_shipping_cost_nio * detail_data.quantity / detail_data.total_quantity, 2)
                        END AS rounded_allocation
                    FROM detail_data
                ), assigned_data AS (
                    SELECT
                        rounded_data.*,
                        SUM(rounded_data.rounded_allocation) OVER (
                            PARTITION BY rounded_data.product_receipt_id
                            ORDER BY rounded_data.id
                            ROWS BETWEEN UNBOUNDED PRECEDING AND 1 PRECEDING
                        ) AS assigned_before
                    FROM rounded_data
                )
                UPDATE product_receipt_details AS detail
                SET allocated_warehouse_shipping_cost_nio = CASE
                    WHEN assigned_data.row_number = assigned_data.row_count
                        THEN assigned_data.warehouse_shipping_cost_nio - COALESCE(assigned_data.assigned_before, 0)
                    ELSE assigned_data.rounded_allocation
                END
                FROM assigned_data
                WHERE detail.id = assigned_data.id;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_receipt_warehouse_shipping_cost_nio_non_negative",
                table: "product_receipts",
                sql: "warehouse_shipping_cost_nio >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_receipt_warehouse_shipping_cost_usd_non_negative",
                table: "product_receipts",
                sql: "warehouse_shipping_cost_usd >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_receipt_detail_allocated_warehouse_shipping_cost_ni~",
                table: "product_receipt_details",
                sql: "allocated_warehouse_shipping_cost_nio >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_product_receipt_detail_weight_positive",
                table: "product_receipt_details",
                sql: "weight > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_product_receipt_warehouse_shipping_cost_nio_non_negative",
                table: "product_receipts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_receipt_warehouse_shipping_cost_usd_non_negative",
                table: "product_receipts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_receipt_detail_allocated_warehouse_shipping_cost_ni~",
                table: "product_receipt_details");

            migrationBuilder.DropCheckConstraint(
                name: "ck_product_receipt_detail_weight_positive",
                table: "product_receipt_details");

            migrationBuilder.DropColumn(
                name: "warehouse_shipping_cost_nio",
                table: "product_receipts");

            migrationBuilder.DropColumn(
                name: "warehouse_shipping_cost_usd",
                table: "product_receipts");

            migrationBuilder.DropColumn(
                name: "allocated_warehouse_shipping_cost_nio",
                table: "product_receipt_details");

            migrationBuilder.DropColumn(
                name: "weight",
                table: "product_receipt_details");
        }
    }
}
