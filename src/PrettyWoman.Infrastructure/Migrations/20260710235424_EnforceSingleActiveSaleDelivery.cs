using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnforceSingleActiveSaleDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_sale_deliveries_sale_id",
                table: "sale_deliveries");

            migrationBuilder.CreateIndex(
                name: "ux_sale_deliveries_sale_id_active",
                table: "sale_deliveries",
                column: "sale_id",
                unique: true,
                filter: "delivery_status_id <> 2 AND delivery_status_id <> 3");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_sale_deliveries_sale_id_active",
                table: "sale_deliveries");

            migrationBuilder.CreateIndex(
                name: "ix_sale_deliveries_sale_id",
                table: "sale_deliveries",
                column: "sale_id");
        }
    }
}
