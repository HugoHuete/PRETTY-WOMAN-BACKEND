using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryAgencyReconciliations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Algunas bases creadas antes de endurecer las reglas no tienen estas restricciones.
            // IF EXISTS permite aplicar la migración tanto sobre esos esquemas como sobre el esquema esperado.
            migrationBuilder.Sql("ALTER TABLE sale_deliveries DROP CONSTRAINT IF EXISTS ck_sale_deliveries_change_amount_non_negative;");
            migrationBuilder.Sql("ALTER TABLE sale_deliveries DROP CONSTRAINT IF EXISTS ck_sale_deliveries_exchange_rate_required_for_usd;");

            migrationBuilder.RenameColumn(
                name: "exchange_rate",
                table: "sale_deliveries",
                newName: "collection_exchange_rate");

            migrationBuilder.RenameColumn(
                name: "change_amount",
                table: "sale_deliveries",
                newName: "change_given_nio");

            migrationBuilder.AddColumn<int>(
                name: "delivery_agency_reconciliation_id",
                table: "sale_payment_movements",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "delivery_agency_reconciliation_id",
                table: "sale_deliveries",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "delivery_agency_reconciliation_id",
                table: "financial_movements",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "delivery_agency_reconciliations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    delivery_agency_id = table.Column<int>(type: "integer", nullable: false),
                    reconciliation_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    settlement_exchange_rate = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    amount_received_nio = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    amount_received_usd = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    amount_paid_to_agency_nio = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    amount_paid_to_agency_usd = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    comments = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<string>(type: "text", nullable: true),
                    updated_by_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_delivery_agency_reconciliations", x => x.id);
                    table.CheckConstraint("ck_delivery_agency_reconciliations_amount_paid_nio_non_negative", "amount_paid_to_agency_nio >= 0");
                    table.CheckConstraint("ck_delivery_agency_reconciliations_amount_paid_usd_non_negative", "amount_paid_to_agency_usd >= 0");
                    table.CheckConstraint("ck_delivery_agency_reconciliations_amount_received_nio_non_neg~", "amount_received_nio >= 0");
                    table.CheckConstraint("ck_delivery_agency_reconciliations_amount_received_usd_non_neg~", "amount_received_usd >= 0");
                    table.CheckConstraint("ck_delivery_agency_reconciliations_settlement_exchange_rate_po~", "settlement_exchange_rate > 0");
                    table.ForeignKey(
                        name: "fk_delivery_agency_reconciliations_delivery_agencies_delivery_",
                        column: x => x.delivery_agency_id,
                        principalTable: "delivery_agencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "financial_movement_types",
                columns: new[] { "id", "name" },
                values: new object[] { 12, "DeliveryAgencyReconciliation" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_payment_movements_delivery_agency_reconciliation_id",
                table: "sale_payment_movements",
                column: "delivery_agency_reconciliation_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_deliveries_delivery_agency_reconciliation_id",
                table: "sale_deliveries",
                column: "delivery_agency_reconciliation_id",
                filter: "delivery_agency_reconciliation_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_deliveries_change_given_nio_non_negative",
                table: "sale_deliveries",
                sql: "change_given_nio >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_deliveries_collection_exchange_rate_required_for_usd",
                table: "sale_deliveries",
                sql: "(\n                    (amount_collected_usd = 0 AND collection_exchange_rate IS NULL)\n                    OR\n                    (amount_collected_usd > 0 AND collection_exchange_rate > 0)\n                )");

            migrationBuilder.CreateIndex(
                name: "ix_financial_movements_delivery_agency_reconciliation_id",
                table: "financial_movements",
                column: "delivery_agency_reconciliation_id");

            migrationBuilder.CreateIndex(
                name: "ix_delivery_agency_reconciliations_delivery_agency_id_reconcil",
                table: "delivery_agency_reconciliations",
                columns: new[] { "delivery_agency_id", "reconciliation_date" });

            migrationBuilder.CreateIndex(
                name: "ix_delivery_agency_reconciliations_reconciliation_date",
                table: "delivery_agency_reconciliations",
                column: "reconciliation_date");

            migrationBuilder.AddForeignKey(
                name: "fk_financial_movements_delivery_agency_reconciliations_deliver",
                table: "financial_movements",
                column: "delivery_agency_reconciliation_id",
                principalTable: "delivery_agency_reconciliations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sale_deliveries_delivery_agency_reconciliations_delivery_ag",
                table: "sale_deliveries",
                column: "delivery_agency_reconciliation_id",
                principalTable: "delivery_agency_reconciliations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_sale_payment_movements_delivery_agency_reconciliations_deli",
                table: "sale_payment_movements",
                column: "delivery_agency_reconciliation_id",
                principalTable: "delivery_agency_reconciliations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_financial_movements_delivery_agency_reconciliations_deliver",
                table: "financial_movements");

            migrationBuilder.DropForeignKey(
                name: "fk_sale_deliveries_delivery_agency_reconciliations_delivery_ag",
                table: "sale_deliveries");

            migrationBuilder.DropForeignKey(
                name: "fk_sale_payment_movements_delivery_agency_reconciliations_deli",
                table: "sale_payment_movements");

            migrationBuilder.DropTable(
                name: "delivery_agency_reconciliations");

            migrationBuilder.DropIndex(
                name: "ix_sale_payment_movements_delivery_agency_reconciliation_id",
                table: "sale_payment_movements");

            migrationBuilder.DropIndex(
                name: "ix_sale_deliveries_delivery_agency_reconciliation_id",
                table: "sale_deliveries");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_deliveries_change_given_nio_non_negative",
                table: "sale_deliveries");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_deliveries_collection_exchange_rate_required_for_usd",
                table: "sale_deliveries");

            migrationBuilder.DropIndex(
                name: "ix_financial_movements_delivery_agency_reconciliation_id",
                table: "financial_movements");

            migrationBuilder.DeleteData(
                table: "financial_movement_types",
                keyColumn: "id",
                keyValue: 12);

            migrationBuilder.DropColumn(
                name: "delivery_agency_reconciliation_id",
                table: "sale_payment_movements");

            migrationBuilder.DropColumn(
                name: "delivery_agency_reconciliation_id",
                table: "sale_deliveries");

            migrationBuilder.DropColumn(
                name: "delivery_agency_reconciliation_id",
                table: "financial_movements");

            migrationBuilder.RenameColumn(
                name: "collection_exchange_rate",
                table: "sale_deliveries",
                newName: "exchange_rate");

            migrationBuilder.RenameColumn(
                name: "change_given_nio",
                table: "sale_deliveries",
                newName: "change_amount");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_deliveries_change_amount_non_negative",
                table: "sale_deliveries",
                sql: "change_amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_deliveries_exchange_rate_required_for_usd",
                table: "sale_deliveries",
                sql: "(\n                    (amount_collected_usd = 0 AND exchange_rate IS NULL)\n                    OR\n                    (amount_collected_usd > 0 AND exchange_rate > 0)\n                )");
        }
    }
}
