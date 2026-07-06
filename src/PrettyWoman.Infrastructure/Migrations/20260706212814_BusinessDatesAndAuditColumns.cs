using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BusinessDatesAndAuditColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_sales_sale_channel_id_created_at",
                table: "sales");

            migrationBuilder.DropIndex(
                name: "ix_sales_sale_payment_status_id_created_at",
                table: "sales");

            migrationBuilder.DropIndex(
                name: "ix_sales_sale_status_id_created_at",
                table: "sales");

            migrationBuilder.DropIndex(
                name: "ix_sales_user_id_created_at",
                table: "sales");

            migrationBuilder.DropIndex(
                name: "ix_sale_payments_sale_id_created_at",
                table: "sale_payments");

            migrationBuilder.DropIndex(
                name: "ix_sale_payments_user_id_created_at",
                table: "sale_payments");

            migrationBuilder.DropIndex(
                name: "ix_orders_order_status_id",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "ix_orders_supplier_id",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "ix_loans_loan_owner_id_created_at",
                table: "loans");

            migrationBuilder.DropIndex(
                name: "ix_loan_payments_loan_id_created_at",
                table: "loan_payments");

            migrationBuilder.DropIndex(
                name: "ix_inventory_movements_inventory_movement_type_id_created_at",
                table: "inventory_movements");

            migrationBuilder.DropIndex(
                name: "ix_inventory_movements_product_id_created_at",
                table: "inventory_movements");

            migrationBuilder.DropIndex(
                name: "ix_financial_movements_financial_movement_type_id_created_at",
                table: "financial_movements");

            migrationBuilder.DropIndex(
                name: "ix_financial_movements_movement_direction_id_created_at",
                table: "financial_movements");

            migrationBuilder.AddColumn<string>(
                name: "created_by_id",
                table: "sales",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "sale_date",
                table: "sales",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "sales",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "updated_by_id",
                table: "sales",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "created_by_id",
                table: "sale_payments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "payment_date",
                table: "sale_payments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "sale_payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "updated_by_id",
                table: "sale_payments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "product_receipts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "created_by_id",
                table: "product_receipts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "product_receipts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "updated_by_id",
                table: "product_receipts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "created_by_id",
                table: "product_inventory_issues",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "issue_date",
                table: "product_inventory_issues",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "product_inventory_issues",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "updated_by_id",
                table: "product_inventory_issues",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "created_by_id",
                table: "product_holds",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "hold_date",
                table: "product_holds",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "product_holds",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "updated_by_id",
                table: "product_holds",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "created_by_id",
                table: "orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "purchase_date",
                table: "orders",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "updated_by_id",
                table: "orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "created_by_id",
                table: "loans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "loan_date",
                table: "loans",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "loans",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "updated_by_id",
                table: "loans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "created_by_id",
                table: "loan_payments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "payment_date",
                table: "loan_payments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "loan_payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "updated_by_id",
                table: "loan_payments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "created_by_id",
                table: "inventory_movements",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "movement_date",
                table: "inventory_movements",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "inventory_movements",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "updated_by_id",
                table: "inventory_movements",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "created_by_id",
                table: "financial_movements",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "movement_date",
                table: "financial_movements",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "financial_movements",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "updated_by_id",
                table: "financial_movements",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "discount_campaigns",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "created_by_id",
                table: "discount_campaigns",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "discount_campaigns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "updated_by_id",
                table: "discount_campaigns",
                type: "text",
                nullable: true);

            migrationBuilder.Sql("UPDATE sales SET sale_date = created_at;");
            migrationBuilder.Sql("UPDATE sale_payments SET payment_date = created_at;");
            migrationBuilder.Sql("UPDATE product_receipts SET created_at = received_date;");
            migrationBuilder.Sql("UPDATE product_inventory_issues SET issue_date = created_at;");
            migrationBuilder.Sql("UPDATE product_holds SET hold_date = created_at;");
            migrationBuilder.Sql("UPDATE orders SET purchase_date = created_at;");
            migrationBuilder.Sql("UPDATE loans SET loan_date = created_at;");
            migrationBuilder.Sql("UPDATE loan_payments SET payment_date = created_at;");
            migrationBuilder.Sql("UPDATE inventory_movements SET movement_date = created_at;");
            migrationBuilder.Sql("UPDATE financial_movements SET movement_date = created_at;");
            migrationBuilder.Sql("UPDATE discount_campaigns SET created_at = start_date;");
            migrationBuilder.CreateIndex(
                name: "ix_sales_sale_channel_id_sale_date",
                table: "sales",
                columns: new[] { "sale_channel_id", "sale_date" });

            migrationBuilder.CreateIndex(
                name: "ix_sales_sale_date",
                table: "sales",
                column: "sale_date");

            migrationBuilder.CreateIndex(
                name: "ix_sales_sale_payment_status_id_sale_date",
                table: "sales",
                columns: new[] { "sale_payment_status_id", "sale_date" });

            migrationBuilder.CreateIndex(
                name: "ix_sales_sale_status_id_sale_date",
                table: "sales",
                columns: new[] { "sale_status_id", "sale_date" });

            migrationBuilder.CreateIndex(
                name: "ix_sales_user_id_sale_date",
                table: "sales",
                columns: new[] { "user_id", "sale_date" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_payments_payment_date",
                table: "sale_payments",
                column: "payment_date");

            migrationBuilder.CreateIndex(
                name: "ix_sale_payments_sale_id_payment_date",
                table: "sale_payments",
                columns: new[] { "sale_id", "payment_date" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_payments_user_id_payment_date",
                table: "sale_payments",
                columns: new[] { "user_id", "payment_date" });

            migrationBuilder.CreateIndex(
                name: "ix_product_receipts_created_at",
                table: "product_receipts",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_product_receipts_received_date",
                table: "product_receipts",
                column: "received_date");

            migrationBuilder.CreateIndex(
                name: "ix_product_inventory_issues_issue_date",
                table: "product_inventory_issues",
                column: "issue_date");

            migrationBuilder.CreateIndex(
                name: "ix_orders_created_at",
                table: "orders",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_orders_order_status_id_purchase_date",
                table: "orders",
                columns: new[] { "order_status_id", "purchase_date" });

            migrationBuilder.CreateIndex(
                name: "ix_orders_purchase_date",
                table: "orders",
                column: "purchase_date");

            migrationBuilder.CreateIndex(
                name: "ix_orders_supplier_id_purchase_date",
                table: "orders",
                columns: new[] { "supplier_id", "purchase_date" });

            migrationBuilder.CreateIndex(
                name: "ix_loans_loan_date",
                table: "loans",
                column: "loan_date");

            migrationBuilder.CreateIndex(
                name: "ix_loans_loan_owner_id_loan_date",
                table: "loans",
                columns: new[] { "loan_owner_id", "loan_date" });

            migrationBuilder.CreateIndex(
                name: "ix_loan_payments_loan_id_payment_date",
                table: "loan_payments",
                columns: new[] { "loan_id", "payment_date" });

            migrationBuilder.CreateIndex(
                name: "ix_loan_payments_payment_date",
                table: "loan_payments",
                column: "payment_date");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_inventory_movement_type_id_movement_date",
                table: "inventory_movements",
                columns: new[] { "inventory_movement_type_id", "movement_date" });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_movement_date",
                table: "inventory_movements",
                column: "movement_date");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_product_id_movement_date",
                table: "inventory_movements",
                columns: new[] { "product_id", "movement_date" });

            migrationBuilder.CreateIndex(
                name: "ix_financial_movements_financial_movement_type_id_movement_date",
                table: "financial_movements",
                columns: new[] { "financial_movement_type_id", "movement_date" });

            migrationBuilder.CreateIndex(
                name: "ix_financial_movements_movement_date",
                table: "financial_movements",
                column: "movement_date");

            migrationBuilder.CreateIndex(
                name: "ix_financial_movements_movement_direction_id_movement_date",
                table: "financial_movements",
                columns: new[] { "movement_direction_id", "movement_date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_sales_sale_channel_id_sale_date",
                table: "sales");

            migrationBuilder.DropIndex(
                name: "ix_sales_sale_date",
                table: "sales");

            migrationBuilder.DropIndex(
                name: "ix_sales_sale_payment_status_id_sale_date",
                table: "sales");

            migrationBuilder.DropIndex(
                name: "ix_sales_sale_status_id_sale_date",
                table: "sales");

            migrationBuilder.DropIndex(
                name: "ix_sales_user_id_sale_date",
                table: "sales");

            migrationBuilder.DropIndex(
                name: "ix_sale_payments_payment_date",
                table: "sale_payments");

            migrationBuilder.DropIndex(
                name: "ix_sale_payments_sale_id_payment_date",
                table: "sale_payments");

            migrationBuilder.DropIndex(
                name: "ix_sale_payments_user_id_payment_date",
                table: "sale_payments");

            migrationBuilder.DropIndex(
                name: "ix_product_receipts_created_at",
                table: "product_receipts");

            migrationBuilder.DropIndex(
                name: "ix_product_receipts_received_date",
                table: "product_receipts");

            migrationBuilder.DropIndex(
                name: "ix_product_inventory_issues_issue_date",
                table: "product_inventory_issues");

            migrationBuilder.DropIndex(
                name: "ix_orders_created_at",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "ix_orders_order_status_id_purchase_date",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "ix_orders_purchase_date",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "ix_orders_supplier_id_purchase_date",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "ix_loans_loan_date",
                table: "loans");

            migrationBuilder.DropIndex(
                name: "ix_loans_loan_owner_id_loan_date",
                table: "loans");

            migrationBuilder.DropIndex(
                name: "ix_loan_payments_loan_id_payment_date",
                table: "loan_payments");

            migrationBuilder.DropIndex(
                name: "ix_loan_payments_payment_date",
                table: "loan_payments");

            migrationBuilder.DropIndex(
                name: "ix_inventory_movements_inventory_movement_type_id_movement_date",
                table: "inventory_movements");

            migrationBuilder.DropIndex(
                name: "ix_inventory_movements_movement_date",
                table: "inventory_movements");

            migrationBuilder.DropIndex(
                name: "ix_inventory_movements_product_id_movement_date",
                table: "inventory_movements");

            migrationBuilder.DropIndex(
                name: "ix_financial_movements_financial_movement_type_id_movement_date",
                table: "financial_movements");

            migrationBuilder.DropIndex(
                name: "ix_financial_movements_movement_date",
                table: "financial_movements");

            migrationBuilder.DropIndex(
                name: "ix_financial_movements_movement_direction_id_movement_date",
                table: "financial_movements");

            migrationBuilder.DropColumn(
                name: "created_by_id",
                table: "sales");

            migrationBuilder.DropColumn(
                name: "sale_date",
                table: "sales");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "sales");

            migrationBuilder.DropColumn(
                name: "updated_by_id",
                table: "sales");

            migrationBuilder.DropColumn(
                name: "created_by_id",
                table: "sale_payments");

            migrationBuilder.DropColumn(
                name: "payment_date",
                table: "sale_payments");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "sale_payments");

            migrationBuilder.DropColumn(
                name: "updated_by_id",
                table: "sale_payments");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "product_receipts");

            migrationBuilder.DropColumn(
                name: "created_by_id",
                table: "product_receipts");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "product_receipts");

            migrationBuilder.DropColumn(
                name: "updated_by_id",
                table: "product_receipts");

            migrationBuilder.DropColumn(
                name: "created_by_id",
                table: "product_inventory_issues");

            migrationBuilder.DropColumn(
                name: "issue_date",
                table: "product_inventory_issues");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "product_inventory_issues");

            migrationBuilder.DropColumn(
                name: "updated_by_id",
                table: "product_inventory_issues");

            migrationBuilder.DropColumn(
                name: "created_by_id",
                table: "product_holds");

            migrationBuilder.DropColumn(
                name: "hold_date",
                table: "product_holds");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "product_holds");

            migrationBuilder.DropColumn(
                name: "updated_by_id",
                table: "product_holds");

            migrationBuilder.DropColumn(
                name: "created_by_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "purchase_date",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "updated_by_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "created_by_id",
                table: "loans");

            migrationBuilder.DropColumn(
                name: "loan_date",
                table: "loans");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "loans");

            migrationBuilder.DropColumn(
                name: "updated_by_id",
                table: "loans");

            migrationBuilder.DropColumn(
                name: "created_by_id",
                table: "loan_payments");

            migrationBuilder.DropColumn(
                name: "payment_date",
                table: "loan_payments");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "loan_payments");

            migrationBuilder.DropColumn(
                name: "updated_by_id",
                table: "loan_payments");

            migrationBuilder.DropColumn(
                name: "created_by_id",
                table: "inventory_movements");

            migrationBuilder.DropColumn(
                name: "movement_date",
                table: "inventory_movements");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "inventory_movements");

            migrationBuilder.DropColumn(
                name: "updated_by_id",
                table: "inventory_movements");

            migrationBuilder.DropColumn(
                name: "created_by_id",
                table: "financial_movements");

            migrationBuilder.DropColumn(
                name: "movement_date",
                table: "financial_movements");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "financial_movements");

            migrationBuilder.DropColumn(
                name: "updated_by_id",
                table: "financial_movements");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "discount_campaigns");

            migrationBuilder.DropColumn(
                name: "created_by_id",
                table: "discount_campaigns");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "discount_campaigns");

            migrationBuilder.DropColumn(
                name: "updated_by_id",
                table: "discount_campaigns");

            migrationBuilder.CreateIndex(
                name: "ix_sales_sale_channel_id_created_at",
                table: "sales",
                columns: new[] { "sale_channel_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_sales_sale_payment_status_id_created_at",
                table: "sales",
                columns: new[] { "sale_payment_status_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_sales_sale_status_id_created_at",
                table: "sales",
                columns: new[] { "sale_status_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_sales_user_id_created_at",
                table: "sales",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_payments_sale_id_created_at",
                table: "sale_payments",
                columns: new[] { "sale_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_payments_user_id_created_at",
                table: "sale_payments",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_orders_order_status_id",
                table: "orders",
                column: "order_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_supplier_id",
                table: "orders",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_loans_loan_owner_id_created_at",
                table: "loans",
                columns: new[] { "loan_owner_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_loan_payments_loan_id_created_at",
                table: "loan_payments",
                columns: new[] { "loan_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_inventory_movement_type_id_created_at",
                table: "inventory_movements",
                columns: new[] { "inventory_movement_type_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_product_id_created_at",
                table: "inventory_movements",
                columns: new[] { "product_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_financial_movements_financial_movement_type_id_created_at",
                table: "financial_movements",
                columns: new[] { "financial_movement_type_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_financial_movements_movement_direction_id_created_at",
                table: "financial_movements",
                columns: new[] { "movement_direction_id", "created_at" });
        }
    }
}
