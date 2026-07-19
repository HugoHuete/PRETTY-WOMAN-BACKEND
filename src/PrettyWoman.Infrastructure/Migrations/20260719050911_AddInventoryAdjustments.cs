using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryAdjustments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            for (var movementTypeId = 5; movementTypeId <= 22; movementTypeId++)
            {
                migrationBuilder.DeleteData(
                    table: "inventory_movement_types",
                    keyColumn: "id",
                    keyValue: movementTypeId);
            }

            migrationBuilder.AddColumn<int>(
                name: "inventory_adjustment_item_id",
                table: "inventory_movements",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "inventory_adjustment_reasons",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_adjustment_reasons", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "inventory_adjustments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    inventory_adjustment_reason_id = table.Column<int>(type: "integer", nullable: false),
                    adjustment_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    comments = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<string>(type: "text", nullable: true),
                    updated_by_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_adjustments", x => x.id);
                    table.ForeignKey(
                        name: "fk_inventory_adjustments_inventory_adjustment_reasons_inventor",
                        column: x => x.inventory_adjustment_reason_id,
                        principalTable: "inventory_adjustment_reasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_adjustment_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    inventory_adjustment_id = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    from_stock_bucket_id = table.Column<int>(type: "integer", nullable: false),
                    to_stock_bucket_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    comments = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_adjustment_items", x => x.id);
                    table.CheckConstraint("ck_inventory_adjustment_items_different_buckets", "from_stock_bucket_id <> to_stock_bucket_id");
                    table.CheckConstraint("ck_inventory_adjustment_items_quantity_positive", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_inventory_adjustment_items_inventory_adjustments_inventory_",
                        column: x => x.inventory_adjustment_id,
                        principalTable: "inventory_adjustments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_inventory_adjustment_items_inventory_stock_buckets_from_sto",
                        column: x => x.from_stock_bucket_id,
                        principalTable: "inventory_stock_buckets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_adjustment_items_inventory_stock_buckets_to_stock",
                        column: x => x.to_stock_bucket_id,
                        principalTable: "inventory_stock_buckets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_adjustment_items_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "inventory_adjustment_reasons",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 1, "ManualCorrection" },
                    { 2, "ProductCodeMixUp" },
                    { 3, "PurchaseSurplus" },
                    { 4, "PurchaseShortage" },
                    { 5, "LostItem" },
                    { 6, "FoundItem" },
                    { 7, "Donation" },
                    { 8, "Other" }
                });

            migrationBuilder.InsertData(
                table: "inventory_movement_types",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 5, "IssueOpened" },
                    { 6, "IssueReturnedToAvailable" },
                    { 7, "IssueRemovedFromInventory" },
                    { 8, "ReservationCreated" },
                    { 9, "ReservationReleased" },
                    { 10, "ReservationConvertedToSale" },
                    { 11, "SelectionSent" },
                    { 12, "SelectionConvertedToSale" },
                    { 13, "SelectionReturned" },
                    { 14, "ExchangeReplacementReserved" },
                    { 15, "ExchangeReplacementDelivered" },
                    { 16, "ExchangeReplacementReservationReleased" },
                    { 17, "ExchangeReturnReceivedByAgency" },
                    { 18, "ExchangeReturnMissing" },
                    { 19, "AdjustmentTransfer" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_inventory_adjustment_item_id",
                table: "inventory_movements",
                column: "inventory_adjustment_item_id",
                unique: true,
                filter: "inventory_adjustment_item_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_adjustment_items_from_stock_bucket_id",
                table: "inventory_adjustment_items",
                column: "from_stock_bucket_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_adjustment_items_inventory_adjustment_id",
                table: "inventory_adjustment_items",
                column: "inventory_adjustment_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_adjustment_items_product_id",
                table: "inventory_adjustment_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_adjustment_items_to_stock_bucket_id",
                table: "inventory_adjustment_items",
                column: "to_stock_bucket_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_adjustment_reasons_name",
                table: "inventory_adjustment_reasons",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_inventory_adjustments_adjustment_date",
                table: "inventory_adjustments",
                column: "adjustment_date");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_adjustments_created_at",
                table: "inventory_adjustments",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_adjustments_inventory_adjustment_reason_id",
                table: "inventory_adjustments",
                column: "inventory_adjustment_reason_id");

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_movements_inventory_adjustment_items_inventory_ad",
                table: "inventory_movements",
                column: "inventory_adjustment_item_id",
                principalTable: "inventory_adjustment_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_inventory_movements_inventory_adjustment_items_inventory_ad",
                table: "inventory_movements");

            migrationBuilder.DropTable(
                name: "inventory_adjustment_items");

            migrationBuilder.DropTable(
                name: "inventory_adjustments");

            migrationBuilder.DropTable(
                name: "inventory_adjustment_reasons");

            migrationBuilder.DropIndex(
                name: "ix_inventory_movements_inventory_adjustment_item_id",
                table: "inventory_movements");

            migrationBuilder.DropColumn(
                name: "inventory_adjustment_item_id",
                table: "inventory_movements");

            for (var movementTypeId = 5; movementTypeId <= 19; movementTypeId++)
            {
                migrationBuilder.DeleteData(
                    table: "inventory_movement_types",
                    keyColumn: "id",
                    keyValue: movementTypeId);
            }

            migrationBuilder.InsertData(
                table: "inventory_movement_types",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 5, "ExchangeReturn" },
                    { 6, "IssueOpened" },
                    { 7, "IssueReturnedToAvailable" },
                    { 8, "IssueRemovedFromInventory" },
                    { 9, "ReservationCreated" },
                    { 10, "ReservationReleased" },
                    { 11, "ReservationConvertedToSale" },
                    { 12, "Donation" },
                    { 13, "AdjustmentIncrease" },
                    { 14, "AdjustmentDecrease" },
                    { 15, "SelectionSent" },
                    { 16, "SelectionConvertedToSale" },
                    { 17, "SelectionReturned" },
                    { 18, "ExchangeReplacementReserved" },
                    { 19, "ExchangeReplacementDelivered" },
                    { 20, "ExchangeReplacementReservationReleased" },
                    { 21, "ExchangeReturnReceivedByAgency" },
                    { 22, "ExchangeReturnMissing" }
                });
        }
    }
}
