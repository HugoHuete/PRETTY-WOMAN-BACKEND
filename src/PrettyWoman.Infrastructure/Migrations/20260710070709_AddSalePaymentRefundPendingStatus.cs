using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSalePaymentRefundPendingStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_financial_movements_sale_payments_sale_payment_id",
                table: "financial_movements");

            migrationBuilder.DropPrimaryKey(
                name: "pk_sale_payments",
                table: "sale_payments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_payments_comission_not_greater_than_amount",
                table: "sale_payments");
            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_payments_commission_amount_non_negative",
                table: "sale_payments");
            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_payments_commission_percentage_non_negative",
                table: "sale_payments");
            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_payments_gross_amount_positive",
                table: "sale_payments");
            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_payments_income_tax_amount_non_negative",
                table: "sale_payments");
            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_payments_income_tax_percentage_non_negative",
                table: "sale_payments");
            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_payments_net_received_amount_matches_components",
                table: "sale_payments");
            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_payments_net_received_amount_non_negative",
                table: "sale_payments");

            migrationBuilder.RenameTable(
                name: "sale_payments",
                newName: "sale_payment_movements");

            migrationBuilder.RenameColumn(
                name: "payment_date",
                table: "sale_payment_movements",
                newName: "movement_date");

            migrationBuilder.RenameColumn(
                name: "sale_payment_id",
                table: "financial_movements",
                newName: "sale_payment_movement_id");

            migrationBuilder.RenameIndex(
                name: "ix_financial_movements_sale_payment_id",
                table: "financial_movements",
                newName: "ix_financial_movements_sale_payment_movement_id");

            migrationBuilder.RenameIndex(
                name: "ix_sale_payments_created_at",
                table: "sale_payment_movements",
                newName: "ix_sale_payment_movements_created_at");

            migrationBuilder.RenameIndex(
                name: "ix_sale_payments_payment_date",
                table: "sale_payment_movements",
                newName: "ix_sale_payment_movements_movement_date");

            migrationBuilder.RenameIndex(
                name: "ix_sale_payments_payment_method_id",
                table: "sale_payment_movements",
                newName: "ix_sale_payment_movements_payment_method_id");

            migrationBuilder.RenameIndex(
                name: "ix_sale_payments_payment_terminal_id",
                table: "sale_payment_movements",
                newName: "ix_sale_payment_movements_payment_terminal_id");

            migrationBuilder.RenameIndex(
                name: "ix_sale_payments_sale_id_payment_date",
                table: "sale_payment_movements",
                newName: "ix_sale_payment_movements_sale_id_movement_date");

            migrationBuilder.RenameIndex(
                name: "ix_sale_payments_user_id_payment_date",
                table: "sale_payment_movements",
                newName: "ix_sale_payment_movements_user_id_movement_date");

            migrationBuilder.AddColumn<int>(
                name: "movement_direction_id",
                table: "sale_payment_movements",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "reversed_sale_payment_movement_id",
                table: "sale_payment_movements",
                type: "integer",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "pk_sale_payment_movements",
                table: "sale_payment_movements",
                column: "id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_payment_movements_card_refund_reverses_original",
                table: "sale_payment_movements",
                sql: "movement_direction_id <> 2 OR payment_method_id <> 3 OR reversed_sale_payment_movement_id IS NOT NULL");
            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_payment_movements_commission_amount_non_negative",
                table: "sale_payment_movements",
                sql: "commission_amount >= 0");
            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_payment_movements_commission_not_greater_than_amount",
                table: "sale_payment_movements",
                sql: "commission_amount + income_tax_amount <= gross_amount");
            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_payment_movements_commission_percentage_non_negative",
                table: "sale_payment_movements",
                sql: "commission_percentage >= 0");
            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_payment_movements_gross_amount_positive",
                table: "sale_payment_movements",
                sql: "gross_amount > 0");
            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_payment_movements_in_does_not_reverse",
                table: "sale_payment_movements",
                sql: "movement_direction_id <> 1 OR reversed_sale_payment_movement_id IS NULL");
            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_payment_movements_income_tax_amount_non_negative",
                table: "sale_payment_movements",
                sql: "income_tax_amount >= 0");
            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_payment_movements_income_tax_percentage_non_negative",
                table: "sale_payment_movements",
                sql: "income_tax_percentage >= 0");
            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_payment_movements_net_received_amount_matches_components",
                table: "sale_payment_movements",
                sql: "net_received_amount = gross_amount - commission_amount - income_tax_amount");
            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_payment_movements_net_received_amount_non_negative",
                table: "sale_payment_movements",
                sql: "net_received_amount >= 0");

            migrationBuilder.InsertData(
                table: "sale_payment_statuses",
                columns: new[] { "id", "name" },
                values: new object[] { 4, "RefundPending" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_payment_movements_movement_direction_id",
                table: "sale_payment_movements",
                column: "movement_direction_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_payment_movements_reversed_sale_payment_movement_id",
                table: "sale_payment_movements",
                column: "reversed_sale_payment_movement_id");

            migrationBuilder.AddForeignKey(
                name: "fk_financial_movements_sale_payment_movements_sale_payment_mov",
                table: "financial_movements",
                column: "sale_payment_movement_id",
                principalTable: "sale_payment_movements",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sale_payment_movements_movement_directions_movement_directi",
                table: "sale_payment_movements",
                column: "movement_direction_id",
                principalTable: "movement_directions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sale_payment_movements_sale_payment_movements_reversed_sale",
                table: "sale_payment_movements",
                column: "reversed_sale_payment_movement_id",
                principalTable: "sale_payment_movements",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_financial_movements_sale_payment_movements_sale_payment_mov",
                table: "financial_movements");

            migrationBuilder.DropForeignKey(
                name: "fk_sale_payment_movements_movement_directions_movement_directi",
                table: "sale_payment_movements");

            migrationBuilder.DropForeignKey(
                name: "fk_sale_payment_movements_sale_payment_movements_reversed_sale",
                table: "sale_payment_movements");

            migrationBuilder.DropPrimaryKey(
                name: "pk_sale_payment_movements",
                table: "sale_payment_movements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_payment_movements_card_refund_reverses_original",
                table: "sale_payment_movements");
            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_payment_movements_commission_amount_non_negative",
                table: "sale_payment_movements");
            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_payment_movements_commission_not_greater_than_amount",
                table: "sale_payment_movements");
            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_payment_movements_commission_percentage_non_negative",
                table: "sale_payment_movements");
            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_payment_movements_gross_amount_positive",
                table: "sale_payment_movements");
            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_payment_movements_in_does_not_reverse",
                table: "sale_payment_movements");
            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_payment_movements_income_tax_amount_non_negative",
                table: "sale_payment_movements");
            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_payment_movements_income_tax_percentage_non_negative",
                table: "sale_payment_movements");
            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_payment_movements_net_received_amount_matches_components",
                table: "sale_payment_movements");
            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_payment_movements_net_received_amount_non_negative",
                table: "sale_payment_movements");

            migrationBuilder.DropIndex(
                name: "ix_sale_payment_movements_movement_direction_id",
                table: "sale_payment_movements");

            migrationBuilder.DropIndex(
                name: "ix_sale_payment_movements_reversed_sale_payment_movement_id",
                table: "sale_payment_movements");

            migrationBuilder.DeleteData(
                table: "sale_payment_statuses",
                keyColumn: "id",
                keyValue: 4);

            migrationBuilder.DropColumn(
                name: "movement_direction_id",
                table: "sale_payment_movements");

            migrationBuilder.DropColumn(
                name: "reversed_sale_payment_movement_id",
                table: "sale_payment_movements");

            migrationBuilder.RenameColumn(
                name: "movement_date",
                table: "sale_payment_movements",
                newName: "payment_date");

            migrationBuilder.RenameColumn(
                name: "sale_payment_movement_id",
                table: "financial_movements",
                newName: "sale_payment_id");

            migrationBuilder.RenameTable(
                name: "sale_payment_movements",
                newName: "sale_payments");

            migrationBuilder.RenameIndex(
                name: "ix_financial_movements_sale_payment_movement_id",
                table: "financial_movements",
                newName: "ix_financial_movements_sale_payment_id");

            migrationBuilder.RenameIndex(
                name: "ix_sale_payment_movements_created_at",
                table: "sale_payments",
                newName: "ix_sale_payments_created_at");

            migrationBuilder.RenameIndex(
                name: "ix_sale_payment_movements_movement_date",
                table: "sale_payments",
                newName: "ix_sale_payments_payment_date");

            migrationBuilder.RenameIndex(
                name: "ix_sale_payment_movements_payment_method_id",
                table: "sale_payments",
                newName: "ix_sale_payments_payment_method_id");

            migrationBuilder.RenameIndex(
                name: "ix_sale_payment_movements_payment_terminal_id",
                table: "sale_payments",
                newName: "ix_sale_payments_payment_terminal_id");

            migrationBuilder.RenameIndex(
                name: "ix_sale_payment_movements_sale_id_movement_date",
                table: "sale_payments",
                newName: "ix_sale_payments_sale_id_payment_date");

            migrationBuilder.RenameIndex(
                name: "ix_sale_payment_movements_user_id_movement_date",
                table: "sale_payments",
                newName: "ix_sale_payments_user_id_payment_date");

            migrationBuilder.AddPrimaryKey(
                name: "pk_sale_payments",
                table: "sale_payments",
                column: "id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_payments_comission_not_greater_than_amount",
                table: "sale_payments",
                sql: "commission_amount + income_tax_amount <= gross_amount");
            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_payments_commission_amount_non_negative",
                table: "sale_payments",
                sql: "commission_amount >= 0");
            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_payments_commission_percentage_non_negative",
                table: "sale_payments",
                sql: "commission_percentage >= 0");
            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_payments_gross_amount_positive",
                table: "sale_payments",
                sql: "gross_amount > 0");
            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_payments_income_tax_amount_non_negative",
                table: "sale_payments",
                sql: "income_tax_amount >= 0");
            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_payments_income_tax_percentage_non_negative",
                table: "sale_payments",
                sql: "income_tax_percentage >= 0");
            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_payments_net_received_amount_matches_components",
                table: "sale_payments",
                sql: "net_received_amount = gross_amount - commission_amount - income_tax_amount");
            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_payments_net_received_amount_non_negative",
                table: "sale_payments",
                sql: "net_received_amount >= 0");

            migrationBuilder.AddForeignKey(
                name: "fk_financial_movements_sale_payments_sale_payment_id",
                table: "financial_movements",
                column: "sale_payment_id",
                principalTable: "sale_payments",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
