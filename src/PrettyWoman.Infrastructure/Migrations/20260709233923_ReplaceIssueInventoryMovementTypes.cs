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
            // Use temporary names to avoid unique-index collisions while several
            // seeded values are shifted to different ids.
            migrationBuilder.UpdateData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 6,
                column: "name",
                value: "__tmp_inventory_movement_type_6");

            migrationBuilder.UpdateData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 7,
                column: "name",
                value: "__tmp_inventory_movement_type_7");

            migrationBuilder.UpdateData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 8,
                column: "name",
                value: "__tmp_inventory_movement_type_8");

            migrationBuilder.UpdateData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 9,
                column: "name",
                value: "__tmp_inventory_movement_type_9");

            migrationBuilder.UpdateData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 10,
                column: "name",
                value: "__tmp_inventory_movement_type_10");

            migrationBuilder.UpdateData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 11,
                column: "name",
                value: "__tmp_inventory_movement_type_11");

            migrationBuilder.UpdateData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 12,
                column: "name",
                value: "__tmp_inventory_movement_type_12");

            migrationBuilder.UpdateData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 13,
                column: "name",
                value: "__tmp_inventory_movement_type_13");

            migrationBuilder.UpdateData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 14,
                column: "name",
                value: "__tmp_inventory_movement_type_14");

            migrationBuilder.UpdateData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 15,
                column: "name",
                value: "__tmp_inventory_movement_type_15");

            migrationBuilder.UpdateData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 16,
                column: "name",
                value: "__tmp_inventory_movement_type_16");

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
            // Use temporary names to avoid unique-index collisions while restoring
            // the original seed values.
            migrationBuilder.UpdateData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 6,
                column: "name",
                value: "__tmp_inventory_movement_type_6");

            migrationBuilder.UpdateData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 7,
                column: "name",
                value: "__tmp_inventory_movement_type_7");

            migrationBuilder.UpdateData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 8,
                column: "name",
                value: "__tmp_inventory_movement_type_8");

            migrationBuilder.UpdateData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 9,
                column: "name",
                value: "__tmp_inventory_movement_type_9");

            migrationBuilder.UpdateData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 10,
                column: "name",
                value: "__tmp_inventory_movement_type_10");

            migrationBuilder.UpdateData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 11,
                column: "name",
                value: "__tmp_inventory_movement_type_11");

            migrationBuilder.UpdateData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 12,
                column: "name",
                value: "__tmp_inventory_movement_type_12");

            migrationBuilder.UpdateData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 13,
                column: "name",
                value: "__tmp_inventory_movement_type_13");

            migrationBuilder.UpdateData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 14,
                column: "name",
                value: "__tmp_inventory_movement_type_14");

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
