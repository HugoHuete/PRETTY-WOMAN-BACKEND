using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierRefundResolutionAndPendingRefundStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "supplier_refund_decline_comments",
                table: "orders",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "supplier_refund_declined_at",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "supplier_refund_resolution",
                table: "orders",
                type: "integer",
                nullable: true);

            migrationBuilder.InsertData(
                table: "order_statuses",
                columns: new[] { "id", "name" },
                values: new object[] { 5, "PendingRefund" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_orders_supplier_refund_resolution_valid",
                table: "orders",
                sql: "supplier_refund_resolution IS NULL OR supplier_refund_resolution IN (1, 2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_orders_supplier_refund_resolution_valid",
                table: "orders");

            migrationBuilder.DeleteData(
                table: "order_statuses",
                keyColumn: "id",
                keyValue: 5);

            migrationBuilder.DropColumn(
                name: "supplier_refund_decline_comments",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "supplier_refund_declined_at",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "supplier_refund_resolution",
                table: "orders");
        }
    }
}
