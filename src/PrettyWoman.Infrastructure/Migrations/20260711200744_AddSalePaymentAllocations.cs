using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSalePaymentAllocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "product_amount",
                table: "sale_payment_movements",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "sale_delivery_id",
                table: "sale_payment_movements",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "shipping_amount",
                table: "sale_payment_movements",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "ix_sale_payment_movements_sale_delivery_id",
                table: "sale_payment_movements",
                column: "sale_delivery_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_payment_movements_amount_matches_allocations",
                table: "sale_payment_movements",
                sql: "gross_amount = product_amount + shipping_amount");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_payment_movements_product_amount_non_negative",
                table: "sale_payment_movements",
                sql: "product_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_payment_movements_shipping_amount_non_negative",
                table: "sale_payment_movements",
                sql: "shipping_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_payment_movements_shipping_requires_delivery",
                table: "sale_payment_movements",
                sql: "(shipping_amount = 0 AND sale_delivery_id IS NULL) OR (shipping_amount > 0 AND sale_delivery_id IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "fk_sale_payment_movements_sale_deliveries_sale_delivery_id",
                table: "sale_payment_movements",
                column: "sale_delivery_id",
                principalTable: "sale_deliveries",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_sale_payment_movements_sale_deliveries_sale_delivery_id",
                table: "sale_payment_movements");

            migrationBuilder.DropIndex(
                name: "ix_sale_payment_movements_sale_delivery_id",
                table: "sale_payment_movements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_payment_movements_amount_matches_allocations",
                table: "sale_payment_movements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_payment_movements_product_amount_non_negative",
                table: "sale_payment_movements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_payment_movements_shipping_amount_non_negative",
                table: "sale_payment_movements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_payment_movements_shipping_requires_delivery",
                table: "sale_payment_movements");

            migrationBuilder.DropColumn(
                name: "product_amount",
                table: "sale_payment_movements");

            migrationBuilder.DropColumn(
                name: "sale_delivery_id",
                table: "sale_payment_movements");

            migrationBuilder.DropColumn(
                name: "shipping_amount",
                table: "sale_payment_movements");
        }
    }
}
