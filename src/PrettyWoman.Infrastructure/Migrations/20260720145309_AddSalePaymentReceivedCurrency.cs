using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSalePaymentReceivedCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "amount_received_nio",
                table: "sale_payment_movements",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "amount_received_usd",
                table: "sale_payment_movements",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "change_given_nio",
                table: "sale_payment_movements",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "exchange_difference_nio",
                table: "sale_payment_movements",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "exchange_rate",
                table: "sale_payment_movements",
                type: "numeric(10,4)",
                precision: 10,
                scale: 4,
                nullable: true);

            migrationBuilder.Sql("UPDATE sale_payment_movements SET amount_received_nio = gross_amount WHERE amount_received_nio = 0 AND amount_received_usd = 0;");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_payment_movements_amount_received_nio_non_negative",
                table: "sale_payment_movements",
                sql: "amount_received_nio >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_payment_movements_amount_received_usd_non_negative",
                table: "sale_payment_movements",
                sql: "amount_received_usd >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_payment_movements_change_given_nio_non_negative",
                table: "sale_payment_movements",
                sql: "change_given_nio >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_payment_movements_exchange_rate_required_for_usd",
                table: "sale_payment_movements",
                sql: "(amount_received_usd = 0 AND exchange_rate IS NULL) OR (amount_received_usd > 0 AND exchange_rate > 0)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_payment_movements_single_received_currency",
                table: "sale_payment_movements",
                sql: "(amount_received_nio > 0 AND amount_received_usd = 0) OR (amount_received_nio = 0 AND amount_received_usd > 0) OR ((delivery_agency_reconciliation_id IS NOT NULL OR reversed_sale_payment_movement_id IS NOT NULL) AND amount_received_nio > 0 AND amount_received_usd > 0)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_payment_movements_amount_received_nio_non_negative",
                table: "sale_payment_movements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_payment_movements_amount_received_usd_non_negative",
                table: "sale_payment_movements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_payment_movements_change_given_nio_non_negative",
                table: "sale_payment_movements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_payment_movements_exchange_rate_required_for_usd",
                table: "sale_payment_movements");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_payment_movements_single_received_currency",
                table: "sale_payment_movements");

            migrationBuilder.DropColumn(
                name: "amount_received_nio",
                table: "sale_payment_movements");

            migrationBuilder.DropColumn(
                name: "amount_received_usd",
                table: "sale_payment_movements");

            migrationBuilder.DropColumn(
                name: "change_given_nio",
                table: "sale_payment_movements");

            migrationBuilder.DropColumn(
                name: "exchange_difference_nio",
                table: "sale_payment_movements");

            migrationBuilder.DropColumn(
                name: "exchange_rate",
                table: "sale_payment_movements");
        }
    }
}
