using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthlyFinancialSummarySnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "completed_at",
                table: "sale_exchanges",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "unit_cost_nio_at_issue",
                table: "product_inventory_issues",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE product_inventory_issues AS issue
                SET unit_cost_nio_at_issue = variant.unit_cost_nio
                FROM product_variants AS variant
                WHERE variant.id = issue.product_id;
                """);

            migrationBuilder.Sql("""
                UPDATE sale_exchanges
                SET completed_at = COALESCE(updated_at, created_at)
                WHERE status_id = 4 AND completed_at IS NULL;
                """);

            migrationBuilder.AlterColumn<decimal>(
                name: "unit_cost_nio_at_issue",
                table: "product_inventory_issues",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)",
                oldPrecision: 18,
                oldScale: 6,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "completed_at",
                table: "sale_exchanges");

            migrationBuilder.DropColumn(
                name: "unit_cost_nio_at_issue",
                table: "product_inventory_issues");
        }
    }
}
