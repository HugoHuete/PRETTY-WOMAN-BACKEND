using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPostSaleDeliveries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "post_sale_deliveries",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sale_return_id = table.Column<int>(type: "integer", nullable: true),
                    sale_exchange_id = table.Column<int>(type: "integer", nullable: true),
                    delivery_agency_id = table.Column<int>(type: "integer", nullable: false),
                    status_id = table.Column<int>(type: "integer", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    shipping_charged_to_client = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    shipping_paid_to_agency = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    delivery_agency_reconciliation_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<string>(type: "text", nullable: true),
                    updated_by_id = table.Column<string>(type: "text", nullable: true),
                    picked_up_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    comments = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_post_sale_deliveries", x => x.id);
                    table.CheckConstraint("ck_post_sale_deliveries_parent", "(sale_return_id IS NOT NULL AND sale_exchange_id IS NULL) OR (sale_return_id IS NULL AND sale_exchange_id IS NOT NULL)");
                    table.CheckConstraint("ck_post_sale_deliveries_shipping_non_negative", "shipping_charged_to_client >= 0 AND shipping_paid_to_agency >= 0");
                    table.ForeignKey(
                        name: "fk_post_sale_deliveries_delivery_agencies_delivery_agency_id",
                        column: x => x.delivery_agency_id,
                        principalTable: "delivery_agencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_post_sale_deliveries_delivery_agency_reconciliations_delive",
                        column: x => x.delivery_agency_reconciliation_id,
                        principalTable: "delivery_agency_reconciliations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_post_sale_deliveries_sale_exchanges_sale_exchange_id",
                        column: x => x.sale_exchange_id,
                        principalTable: "sale_exchanges",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_post_sale_deliveries_sale_returns_sale_return_id",
                        column: x => x.sale_return_id,
                        principalTable: "sale_returns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_post_sale_deliveries_delivery_agency_id_status_id_delivery_",
                table: "post_sale_deliveries",
                columns: new[] { "delivery_agency_id", "status_id", "delivery_agency_reconciliation_id" });

            migrationBuilder.CreateIndex(
                name: "ix_post_sale_deliveries_delivery_agency_reconciliation_id",
                table: "post_sale_deliveries",
                column: "delivery_agency_reconciliation_id");

            migrationBuilder.CreateIndex(
                name: "ix_post_sale_deliveries_sale_exchange_id",
                table: "post_sale_deliveries",
                column: "sale_exchange_id",
                unique: true,
                filter: "sale_exchange_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_post_sale_deliveries_sale_return_id",
                table: "post_sale_deliveries",
                column: "sale_return_id",
                unique: true,
                filter: "sale_return_id IS NOT NULL");

            migrationBuilder.Sql("""
                INSERT INTO post_sale_deliveries
                    (sale_return_id, delivery_agency_id, status_id, code, shipping_charged_to_client,
                     shipping_paid_to_agency, delivery_agency_reconciliation_id, created_at, picked_up_at,
                     received_at, comments)
                SELECT id, delivery_agency_id,
                       CASE status_id WHEN 2 THEN 2 WHEN 4 THEN 3 WHEN 5 THEN 5 ELSE 1 END,
                       return_code, return_shipping_charged_to_client, return_shipping_paid_to_agency,
                       delivery_agency_reconciliation_id, created_at, picked_up_at, received_at, comments
                FROM sale_returns
                WHERE delivery_agency_id IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "post_sale_deliveries");
        }
    }
}
