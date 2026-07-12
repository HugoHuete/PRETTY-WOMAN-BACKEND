using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSelectionDeliveryStates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "delivery_statuses",
                columns: new[] { "id", "name" },
                values: new object[] { 6, "DeliveredPendingSelection" });

            migrationBuilder.InsertData(
                table: "inventory_movement_types",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 15, "SelectionSent" },
                    { 16, "SelectionConvertedToSale" },
                    { 17, "SelectionReturned" }
                });

            migrationBuilder.InsertData(
                table: "product_hold_statuses",
                columns: new[] { "id", "name" },
                values: new object[] { 4, "AwaitingReturn" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "delivery_statuses",
                keyColumn: "id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "product_hold_statuses",
                keyColumn: "id",
                keyValue: 4);
        }
    }
}
