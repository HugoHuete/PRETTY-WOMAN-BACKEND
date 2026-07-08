using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProductReceiptOrderReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "order_id",
                table: "product_receipts",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql("UPDATE product_receipts AS receipt SET order_id = source.order_id FROM (SELECT detail.product_receipt_id, MIN(product.order_id) AS order_id FROM product_receipt_details AS detail INNER JOIN products AS product ON product.id = detail.product_id GROUP BY detail.product_receipt_id) AS source WHERE receipt.id = source.product_receipt_id;");

            migrationBuilder.AlterColumn<int>(
                name: "order_id",
                table: "product_receipts",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_product_receipts_order_id",
                table: "product_receipts",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_receipts_order_id_received_date",
                table: "product_receipts",
                columns: new[] { "order_id", "received_date" });

            migrationBuilder.AddForeignKey(
                name: "fk_product_receipts_orders_order_id",
                table: "product_receipts",
                column: "order_id",
                principalTable: "orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_product_receipts_orders_order_id",
                table: "product_receipts");

            migrationBuilder.DropIndex(
                name: "ix_product_receipts_order_id",
                table: "product_receipts");

            migrationBuilder.DropIndex(
                name: "ix_product_receipts_order_id_received_date",
                table: "product_receipts");

            migrationBuilder.DropColumn(
                name: "order_id",
                table: "product_receipts");
        }
    }
}
