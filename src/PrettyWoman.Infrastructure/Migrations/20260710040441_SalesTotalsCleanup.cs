using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SalesTotalsCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_comission_non_negative",
                table: "sales");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_subtotal_before_discount_non_negative",
                table: "sales");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_subtotal_matches_components",
                table: "sales");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_subtotal_non_negative",
                table: "sales");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_total_discount_not_greater_than_subtotal_before_disco~",
                table: "sales");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_total_matches_components",
                table: "sales");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_details_gross_profit_matches_components",
                table: "sale_products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_details_total_payment_comission_non_negative",
                table: "sale_products");

            migrationBuilder.Sql("UPDATE sales SET total = subtotal_before_discount - total_discount");

            migrationBuilder.RenameColumn(
                name: "subtotal_before_discount",
                table: "sales",
                newName: "subtotal");

            migrationBuilder.DropColumn(
                name: "comission",
                table: "sales");

            migrationBuilder.DropColumn(
                name: "sub_total",
                table: "sales");

            migrationBuilder.DropColumn(
                name: "total_payment_comission",
                table: "sale_products");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_subtotal_non_negative",
                table: "sales",
                sql: "subtotal >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_total_discount_not_greater_than_subtotal",
                table: "sales",
                sql: "total_discount <= subtotal");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_total_matches_components",
                table: "sales",
                sql: "total = subtotal - total_discount");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_details_gross_profit_matches_components",
                table: "sale_products",
                sql: "gross_profit = line_total - total_cost_at_sale");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_subtotal_non_negative",
                table: "sales");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_total_discount_not_greater_than_subtotal",
                table: "sales");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sales_total_matches_components",
                table: "sales");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_details_gross_profit_matches_components",
                table: "sale_products");

            migrationBuilder.RenameColumn(
                name: "subtotal",
                table: "sales",
                newName: "subtotal_before_discount");

            migrationBuilder.AddColumn<decimal>(
                name: "comission",
                table: "sales",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "sub_total",
                table: "sales",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "total_payment_comission",
                table: "sale_products",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("UPDATE sales SET sub_total = subtotal_before_discount - total_discount, total = subtotal_before_discount - total_discount");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_comission_non_negative",
                table: "sales",
                sql: "comission >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_subtotal_before_discount_non_negative",
                table: "sales",
                sql: "subtotal_before_discount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_subtotal_matches_components",
                table: "sales",
                sql: "sub_total = subtotal_before_discount - total_discount");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_subtotal_non_negative",
                table: "sales",
                sql: "sub_total >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_total_discount_not_greater_than_subtotal_before_disco~",
                table: "sales",
                sql: "total_discount <= subtotal_before_discount");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sales_total_matches_components",
                table: "sales",
                sql: "total = sub_total - comission");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_details_gross_profit_matches_components",
                table: "sale_products",
                sql: "gross_profit = line_total - total_payment_comission - total_cost_at_sale");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_details_total_payment_comission_non_negative",
                table: "sale_products",
                sql: "total_payment_comission >= 0");
        }
    }
}

