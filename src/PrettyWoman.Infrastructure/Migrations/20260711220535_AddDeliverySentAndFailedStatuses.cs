using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliverySentAndFailedStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_sale_deliveries_sale_id_active",
                table: "sale_deliveries");

            migrationBuilder.InsertData(
                table: "delivery_statuses",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 4, "Sent" },
                    { 5, "Failed" }
                });

            migrationBuilder.CreateIndex(
                name: "ux_sale_deliveries_sale_id_active",
                table: "sale_deliveries",
                column: "sale_id",
                unique: true,
                filter: "delivery_status_id <> 2 AND delivery_status_id <> 3 AND delivery_status_id <> 5");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_sale_deliveries_sale_id_active",
                table: "sale_deliveries");

            migrationBuilder.DeleteData(
                table: "delivery_statuses",
                keyColumn: "id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "delivery_statuses",
                keyColumn: "id",
                keyValue: 5);

            migrationBuilder.CreateIndex(
                name: "ux_sale_deliveries_sale_id_active",
                table: "sale_deliveries",
                column: "sale_id",
                unique: true,
                filter: "delivery_status_id <> 2 AND delivery_status_id <> 3");
        }
    }
}
