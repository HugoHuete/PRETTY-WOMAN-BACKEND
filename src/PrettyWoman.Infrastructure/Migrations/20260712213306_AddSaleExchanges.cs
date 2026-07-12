using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSaleExchanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "exchange_outbound_item_id",
                table: "inventory_movements",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "exchange_return_item_id",
                table: "inventory_movements",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "sale_exchanges",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    original_sale_id = table.Column<int>(type: "integer", nullable: false),
                    status_id = table.Column<int>(type: "integer", nullable: false),
                    recognized_return_total = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    outbound_items_total = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    balance_to_collect = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    comments = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<string>(type: "text", nullable: true),
                    updated_by_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sale_exchanges", x => x.id);
                    table.CheckConstraint("ck_sale_exchanges_totals_non_negative", "recognized_return_total >= 0 AND outbound_items_total >= 0");
                    table.ForeignKey(
                        name: "fk_sale_exchanges_sales_original_sale_id",
                        column: x => x.original_sale_id,
                        principalTable: "sales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "exchange_outbound_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sale_exchange_id = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    item_type_id = table.Column<int>(type: "integer", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    line_total = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    total_cost = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    delivered = table.Column<bool>(type: "boolean", nullable: false),
                    delivered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    comments = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<string>(type: "text", nullable: true),
                    updated_by_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_exchange_outbound_items", x => x.id);
                    table.CheckConstraint("ck_exchange_outbound_items_quantity_positive", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_exchange_outbound_items_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_exchange_outbound_items_sale_exchanges_sale_exchange_id",
                        column: x => x.sale_exchange_id,
                        principalTable: "sale_exchanges",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "exchange_return_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sale_exchange_id = table.Column<int>(type: "integer", nullable: false),
                    original_sale_product_id = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    recognized_unit_amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    original_unit_cost = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    status_id = table.Column<int>(type: "integer", nullable: false),
                    handed_to_agency_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    comments = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<string>(type: "text", nullable: true),
                    updated_by_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_exchange_return_items", x => x.id);
                    table.CheckConstraint("ck_exchange_return_items_quantity_positive", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_exchange_return_items_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_exchange_return_items_sale_exchanges_sale_exchange_id",
                        column: x => x.sale_exchange_id,
                        principalTable: "sale_exchanges",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_exchange_return_items_sale_products_original_sale_product_id",
                        column: x => x.original_sale_product_id,
                        principalTable: "sale_products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "inventory_movement_types",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 18, "ExchangeReplacementReserved" },
                    { 19, "ExchangeReplacementDelivered" },
                    { 20, "ExchangeReplacementReservationReleased" },
                    { 21, "ExchangeReturnReceivedByAgency" },
                    { 22, "ExchangeReturnMissing" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_exchange_outbound_item_id",
                table: "inventory_movements",
                column: "exchange_outbound_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_exchange_return_item_id",
                table: "inventory_movements",
                column: "exchange_return_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_exchange_outbound_items_product_id",
                table: "exchange_outbound_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_exchange_outbound_items_sale_exchange_id",
                table: "exchange_outbound_items",
                column: "sale_exchange_id");

            migrationBuilder.CreateIndex(
                name: "ix_exchange_return_items_original_sale_product_id",
                table: "exchange_return_items",
                column: "original_sale_product_id");

            migrationBuilder.CreateIndex(
                name: "ix_exchange_return_items_product_id",
                table: "exchange_return_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_exchange_return_items_sale_exchange_id",
                table: "exchange_return_items",
                column: "sale_exchange_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_exchanges_original_sale_id",
                table: "sale_exchanges",
                column: "original_sale_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_exchanges_original_sale_id_status_id",
                table: "sale_exchanges",
                columns: new[] { "original_sale_id", "status_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_movements_exchange_outbound_items_exchange_outbou",
                table: "inventory_movements",
                column: "exchange_outbound_item_id",
                principalTable: "exchange_outbound_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_movements_exchange_return_items_exchange_return_i",
                table: "inventory_movements",
                column: "exchange_return_item_id",
                principalTable: "exchange_return_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_inventory_movements_exchange_outbound_items_exchange_outbou",
                table: "inventory_movements");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_movements_exchange_return_items_exchange_return_i",
                table: "inventory_movements");

            migrationBuilder.DropTable(
                name: "exchange_outbound_items");

            migrationBuilder.DropTable(
                name: "exchange_return_items");

            migrationBuilder.DropTable(
                name: "sale_exchanges");

            migrationBuilder.DropIndex(
                name: "ix_inventory_movements_exchange_outbound_item_id",
                table: "inventory_movements");

            migrationBuilder.DropIndex(
                name: "ix_inventory_movements_exchange_return_item_id",
                table: "inventory_movements");

            migrationBuilder.DeleteData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "inventory_movement_types",
                keyColumn: "id",
                keyValue: 22);

            migrationBuilder.DropColumn(
                name: "exchange_outbound_item_id",
                table: "inventory_movements");

            migrationBuilder.DropColumn(
                name: "exchange_return_item_id",
                table: "inventory_movements");
        }
    }
}
