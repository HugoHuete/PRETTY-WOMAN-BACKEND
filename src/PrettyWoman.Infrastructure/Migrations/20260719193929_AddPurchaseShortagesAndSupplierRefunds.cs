using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseShortagesAndSupplierRefunds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_products_quantity_positive",
                table: "products");

            migrationBuilder.CreateTable(
                name: "purchase_shortages",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    loss_amount_nio = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    shortage_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    comments = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<string>(type: "text", nullable: true),
                    updated_by_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_shortages", x => x.id);
                    table.CheckConstraint("ck_purchase_shortages_loss_amount_non_negative", "loss_amount_nio >= 0");
                    table.CheckConstraint("ck_purchase_shortages_quantity_positive", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_purchase_shortages_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_shortages_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_refunds",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<int>(type: "integer", nullable: false),
                    financial_movement_id = table.Column<int>(type: "integer", nullable: false),
                    amount_nio = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    refunded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    comments = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<string>(type: "text", nullable: true),
                    updated_by_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_refunds", x => x.id);
                    table.CheckConstraint("ck_supplier_refunds_amount_positive", "amount_nio > 0");
                    table.ForeignKey(
                        name: "fk_supplier_refunds_financial_movements_financial_movement_id",
                        column: x => x.financial_movement_id,
                        principalTable: "financial_movements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_supplier_refunds_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_quantity_non_negative",
                table: "products",
                sql: "quantity >= 0");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_shortages_order_id",
                table: "purchase_shortages",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_shortages_product_id",
                table: "purchase_shortages",
                column: "product_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_shortages_shortage_date",
                table: "purchase_shortages",
                column: "shortage_date");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_refunds_financial_movement_id",
                table: "supplier_refunds",
                column: "financial_movement_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_supplier_refunds_order_id",
                table: "supplier_refunds",
                column: "order_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "purchase_shortages");

            migrationBuilder.DropTable(
                name: "supplier_refunds");

            migrationBuilder.DropCheckConstraint(
                name: "ck_products_quantity_non_negative",
                table: "products");

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_quantity_positive",
                table: "products",
                sql: "quantity > 0");
        }
    }
}
