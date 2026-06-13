using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SupplierAndTerminalChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_payments_amount_positive",
                table: "sale_payments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_payments_comission_amount_non_negative",
                table: "sale_payments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_payments_comission_not_greater_than_amount",
                table: "sale_payments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_payments_net_received_amount_matches_components",
                table: "sale_payments");

            migrationBuilder.RenameColumn(
                name: "comission_amount",
                table: "sale_payments",
                newName: "commission_amount");

            migrationBuilder.RenameColumn(
                name: "amount",
                table: "sale_payments",
                newName: "gross_amount");

            migrationBuilder.AddColumn<bool>(
                name: "enabled",
                table: "suppliers",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_national",
                table: "suppliers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "income_tax_amount",
                table: "sale_payments",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "commission_percentage",
                table: "sale_payments",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "income_tax_percentage",
                table: "sale_payments",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "income_tax_percentage",
                table: "payment_terminals",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.DropColumn(
                name: "enabled",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "is_national",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "income_tax_amount",
                table: "sale_payments");

            migrationBuilder.DropColumn(
                name: "commission_percentage",
                table: "sale_payments");

            migrationBuilder.DropColumn(
                name: "income_tax_percentage",
                table: "sale_payments");

            migrationBuilder.DropColumn(
                name: "income_tax_percentage",
                table: "payment_terminals");

            migrationBuilder.RenameColumn(
                name: "commission_amount",
                table: "sale_payments",
                newName: "comission_amount");

            migrationBuilder.RenameColumn(
                name: "gross_amount",
                table: "sale_payments",
                newName: "amount");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_payments_amount_positive",
                table: "sale_payments",
                sql: "amount > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_payments_comission_amount_non_negative",
                table: "sale_payments",
                sql: "comission_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_payments_comission_not_greater_than_amount",
                table: "sale_payments",
                sql: "comission_amount <= amount");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_payments_net_received_amount_matches_components",
                table: "sale_payments",
                sql: "net_received_amount = amount - comission_amount");
        }
    }
}
