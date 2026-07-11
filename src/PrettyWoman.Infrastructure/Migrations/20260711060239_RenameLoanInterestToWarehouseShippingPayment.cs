using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameLoanInterestToWarehouseShippingPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "product_receipt_id",
                table: "financial_movements",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_financial_movements_product_receipt_id",
                table: "financial_movements",
                column: "product_receipt_id");

            migrationBuilder.AddForeignKey(
                name: "fk_financial_movements_product_receipts_product_receipt_id",
                table: "financial_movements",
                column: "product_receipt_id",
                principalTable: "product_receipts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.UpdateData(
                table: "financial_movement_types",
                keyColumn: "id",
                keyValue: 10,
                column: "name",
                value: "WarehouseShippingPayment");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_financial_movements_product_receipts_product_receipt_id",
                table: "financial_movements");

            migrationBuilder.DropIndex(
                name: "ix_financial_movements_product_receipt_id",
                table: "financial_movements");

            migrationBuilder.DropColumn(
                name: "product_receipt_id",
                table: "financial_movements");

            migrationBuilder.UpdateData(
                table: "financial_movement_types",
                keyColumn: "id",
                keyValue: 10,
                column: "name",
                value: "LoanInterest");
        }
    }
}
