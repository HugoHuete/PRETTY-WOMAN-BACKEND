using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceIssueInventoryMovementTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 16);

            migrationBuilder.UpdateData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 6,
                column: "name",
                value: "IssueOpened");

            migrationBuilder.UpdateData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 7,
                column: "name",
                value: "IssueReturnedToAvailable");

            migrationBuilder.UpdateData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 8,
                column: "name",
                value: "IssueRemovedFromInventory");

            migrationBuilder.UpdateData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 9,
                column: "name",
                value: "ReservationCreated");

            migrationBuilder.UpdateData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 10,
                column: "name",
                value: "ReservationReleased");

            migrationBuilder.UpdateData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 11,
                column: "name",
                value: "ReservationConvertedToSale");

            migrationBuilder.UpdateData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 12,
                column: "name",
                value: "Donation");

            migrationBuilder.UpdateData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 13,
                column: "name",
                value: "AdjustmentIncrease");

            migrationBuilder.UpdateData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 14,
                column: "name",
                value: "AdjustmentDecrease");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 6,
                column: "name",
                value: "Damaged");

            migrationBuilder.UpdateData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 7,
                column: "name",
                value: "Repaired");

            migrationBuilder.UpdateData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 8,
                column: "name",
                value: "Lost");

            migrationBuilder.UpdateData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 9,
                column: "name",
                value: "Found");

            migrationBuilder.UpdateData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 10,
                column: "name",
                value: "Discarded");

            migrationBuilder.UpdateData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 11,
                column: "name",
                value: "Donation");

            migrationBuilder.UpdateData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 12,
                column: "name",
                value: "AdjustmentIncrease");

            migrationBuilder.UpdateData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 13,
                column: "name",
                value: "AdjustmentDecrease");

            migrationBuilder.UpdateData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 14,
                column: "name",
                value: "ReservationCreated");

            migrationBuilder.InsertData(
                table: "inventory_movement_types",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 15, "ReservationReleased" },
                    { 16, "ReservationConvertedToSale" }
                });
        }
    }
}
