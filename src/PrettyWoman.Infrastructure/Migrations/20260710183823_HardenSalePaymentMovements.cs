using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HardenSalePaymentMovements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_sale_payment_movements_reversed_sale_payment_movement_id",
                table: "sale_payment_movements");

            migrationBuilder.DropIndex(
                name: "ix_financial_movements_sale_payment_movement_id",
                table: "financial_movements");

            migrationBuilder.CreateIndex(
                name: "ix_sale_payment_movements_reversed_sale_payment_movement_id",
                table: "sale_payment_movements",
                column: "reversed_sale_payment_movement_id",
                unique: true,
                filter: "movement_direction_id = 2 AND payment_method_id = 3");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_payment_movements_card_requires_terminal",
                table: "sale_payment_movements",
                sql: "payment_method_id <> 3 OR payment_terminal_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_payment_movements_only_card_uses_terminal",
                table: "sale_payment_movements",
                sql: "payment_method_id = 3 OR payment_terminal_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_financial_movements_sale_payment_movement_id",
                table: "financial_movements",
                column: "sale_payment_movement_id",
                unique: true,
                filter: "sale_payment_movement_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_sale_payment_movements_reversed_sale_payment_movement_id",
                table: "sale_payment_movements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_payment_movements_card_requires_terminal",
                table: "sale_payment_movements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_payment_movements_only_card_uses_terminal",
                table: "sale_payment_movements");

            migrationBuilder.DropIndex(
                name: "ix_financial_movements_sale_payment_movement_id",
                table: "financial_movements");

            migrationBuilder.CreateIndex(
                name: "ix_sale_payment_movements_reversed_sale_payment_movement_id",
                table: "sale_payment_movements",
                column: "reversed_sale_payment_movement_id");

            migrationBuilder.CreateIndex(
                name: "ix_financial_movements_sale_payment_movement_id",
                table: "financial_movements",
                column: "sale_payment_movement_id");
        }
    }
}
