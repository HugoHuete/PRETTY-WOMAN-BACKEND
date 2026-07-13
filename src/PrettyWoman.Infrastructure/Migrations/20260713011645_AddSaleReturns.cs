using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSaleReturns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "sale_return_item_id",
                table: "inventory_movements",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sale_return_id",
                table: "financial_movements",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "sale_returns",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    original_sale_id = table.Column<int>(type: "integer", nullable: false),
                    reason_id = table.Column<int>(type: "integer", nullable: false),
                    method_id = table.Column<int>(type: "integer", nullable: false),
                    status_id = table.Column<int>(type: "integer", nullable: false),
                    delivery_agency_id = table.Column<int>(type: "integer", nullable: true),
                    return_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    product_refund_total = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    return_shipping_charged_to_client = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    return_shipping_paid_to_agency = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    original_shipping_refund = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    refund_total = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    refund_payment_method_id = table.Column<int>(type: "integer", nullable: true),
                    delivery_agency_reconciliation_id = table.Column<int>(type: "integer", nullable: true),
                    refunded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    picked_up_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    comments = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<string>(type: "text", nullable: true),
                    updated_by_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sale_returns", x => x.id);
                    table.CheckConstraint("ck_sale_returns_totals_non_negative", "product_refund_total >= 0 AND return_shipping_charged_to_client >= 0 AND return_shipping_paid_to_agency >= 0 AND original_shipping_refund >= 0 AND refund_total >= 0");
                    table.ForeignKey(
                        name: "fk_sale_returns_delivery_agencies_delivery_agency_id",
                        column: x => x.delivery_agency_id,
                        principalTable: "delivery_agencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_returns_delivery_agency_reconciliations_delivery_agenc",
                        column: x => x.delivery_agency_reconciliation_id,
                        principalTable: "delivery_agency_reconciliations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_returns_payment_methods_refund_payment_method_id",
                        column: x => x.refund_payment_method_id,
                        principalTable: "payment_methods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_returns_sales_original_sale_id",
                        column: x => x.original_sale_id,
                        principalTable: "sales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sale_return_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sale_return_id = table.Column<int>(type: "integer", nullable: false),
                    original_sale_product_id = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    recognized_unit_amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    original_unit_cost = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    product_inventory_issue_id = table.Column<int>(type: "integer", nullable: true),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    comments = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<string>(type: "text", nullable: true),
                    updated_by_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sale_return_items", x => x.id);
                    table.CheckConstraint("ck_sale_return_items_quantity_positive", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_sale_return_items_product_inventory_issues_product_inventor",
                        column: x => x.product_inventory_issue_id,
                        principalTable: "product_inventory_issues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_return_items_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_return_items_sale_products_original_sale_product_id",
                        column: x => x.original_sale_product_id,
                        principalTable: "sale_products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_return_items_sale_returns_sale_return_id",
                        column: x => x.sale_return_id,
                        principalTable: "sale_returns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_sale_return_item_id",
                table: "inventory_movements",
                column: "sale_return_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_financial_movements_sale_return_id",
                table: "financial_movements",
                column: "sale_return_id",
                unique: true,
                filter: "sale_return_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_sale_return_items_original_sale_product_id",
                table: "sale_return_items",
                column: "original_sale_product_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_return_items_product_id",
                table: "sale_return_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_return_items_product_inventory_issue_id",
                table: "sale_return_items",
                column: "product_inventory_issue_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_return_items_sale_return_id",
                table: "sale_return_items",
                column: "sale_return_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_returns_delivery_agency_id",
                table: "sale_returns",
                column: "delivery_agency_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_returns_delivery_agency_reconciliation_id",
                table: "sale_returns",
                column: "delivery_agency_reconciliation_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_returns_original_sale_id",
                table: "sale_returns",
                column: "original_sale_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_returns_original_sale_id_status_id",
                table: "sale_returns",
                columns: new[] { "original_sale_id", "status_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_returns_refund_payment_method_id",
                table: "sale_returns",
                column: "refund_payment_method_id");

            migrationBuilder.AddForeignKey(
                name: "fk_financial_movements_sale_returns_sale_return_id",
                table: "financial_movements",
                column: "sale_return_id",
                principalTable: "sale_returns",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_movements_sale_return_items_sale_return_item_id",
                table: "inventory_movements",
                column: "sale_return_item_id",
                principalTable: "sale_return_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_financial_movements_sale_returns_sale_return_id",
                table: "financial_movements");

            migrationBuilder.DropForeignKey(
                name: "fk_inventory_movements_sale_return_items_sale_return_item_id",
                table: "inventory_movements");

            migrationBuilder.DropTable(
                name: "sale_return_items");

            migrationBuilder.DropTable(
                name: "sale_returns");

            migrationBuilder.DropIndex(
                name: "ix_inventory_movements_sale_return_item_id",
                table: "inventory_movements");

            migrationBuilder.DropIndex(
                name: "ix_financial_movements_sale_return_id",
                table: "financial_movements");

            migrationBuilder.DropColumn(
                name: "sale_return_item_id",
                table: "inventory_movements");

            migrationBuilder.DropColumn(
                name: "sale_return_id",
                table: "financial_movements");
        }
    }
}
