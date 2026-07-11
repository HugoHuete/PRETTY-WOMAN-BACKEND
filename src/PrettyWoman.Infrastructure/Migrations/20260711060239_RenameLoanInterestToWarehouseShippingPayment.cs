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
            migrationBuilder.UpdateData(
                table: "financial_movement_types",
                keyColumn: "id",
                keyValue: 10,
                column: "name",
                value: "LoanInterest");
        }
    }
}
