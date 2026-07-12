using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDeliveryAgencyReconciliationPaidUsd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_delivery_agency_reconciliations_amount_paid_usd_non_negative",
                table: "delivery_agency_reconciliations");

            migrationBuilder.DropColumn(
                name: "amount_paid_to_agency_usd",
                table: "delivery_agency_reconciliations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "amount_paid_to_agency_usd",
                table: "delivery_agency_reconciliations",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddCheckConstraint(
                name: "ck_delivery_agency_reconciliations_amount_paid_usd_non_negative",
                table: "delivery_agency_reconciliations",
                sql: "amount_paid_to_agency_usd >= 0");
        }
    }
}
