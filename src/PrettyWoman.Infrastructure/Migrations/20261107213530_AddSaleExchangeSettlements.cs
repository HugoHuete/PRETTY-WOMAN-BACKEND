using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSaleExchangeSettlements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "sale_exchange_settlement_tender_id",
                table: "financial_movements",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "sale_exchange_settlements",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sale_exchange_id = table.Column<int>(type: "integer", nullable: false),
                    movement_direction_id = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    movement_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    comments = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<string>(type: "text", nullable: true),
                    updated_by_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sale_exchange_settlements", x => x.id);
                    table.CheckConstraint("ck_sale_exchange_settlements_amount_positive", "amount > 0");
                    table.CheckConstraint("ck_sale_exchange_settlements_direction_valid", "movement_direction_id IN (1, 2)");
                    table.ForeignKey(
                        name: "fk_sale_exchange_settlements_sale_exchanges_sale_exchange_id",
                        column: x => x.sale_exchange_id,
                        principalTable: "sale_exchanges",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sale_exchange_settlement_tenders",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sale_exchange_settlement_id = table.Column<int>(type: "integer", nullable: false),
                    payment_method_id = table.Column<int>(type: "integer", nullable: false),
                    payment_terminal_id = table.Column<int>(type: "integer", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    commission_percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    commission_amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    income_tax_percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    income_tax_amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<string>(type: "text", nullable: true),
                    updated_by_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sale_exchange_settlement_tenders", x => x.id);
                    table.CheckConstraint("ck_sale_exchange_settlement_tenders_amount_positive", "amount > 0");
                    table.CheckConstraint("ck_sale_exchange_settlement_tenders_card_requires_terminal", "payment_method_id <> 3 OR payment_terminal_id IS NOT NULL");
                    table.CheckConstraint("ck_sale_exchange_settlement_tenders_commission_non_negative", "commission_percentage >= 0 AND commission_amount >= 0");
                    table.CheckConstraint("ck_sale_exchange_settlement_tenders_only_card_uses_terminal", "payment_method_id = 3 OR payment_terminal_id IS NULL");
                    table.CheckConstraint("ck_sale_exchange_settlement_tenders_tax_non_negative", "income_tax_percentage >= 0 AND income_tax_amount >= 0");
                    table.ForeignKey(
                        name: "fk_sale_exchange_settlement_tenders_payment_methods_payment_me",
                        column: x => x.payment_method_id,
                        principalTable: "payment_methods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_exchange_settlement_tenders_payment_terminals_payment_",
                        column: x => x.payment_terminal_id,
                        principalTable: "payment_terminals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_exchange_settlement_tenders_sale_exchange_settlements_",
                        column: x => x.sale_exchange_settlement_id,
                        principalTable: "sale_exchange_settlements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "financial_movement_types",
                columns: new[] { "id", "name" },
                values: new object[] { 13, "SaleExchangeSettlement" });

            migrationBuilder.CreateIndex(
                name: "ix_financial_movements_sale_exchange_settlement_tender_id",
                table: "financial_movements",
                column: "sale_exchange_settlement_tender_id",
                unique: true,
                filter: "sale_exchange_settlement_tender_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_sale_exchange_settlement_tenders_payment_method_id",
                table: "sale_exchange_settlement_tenders",
                column: "payment_method_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_exchange_settlement_tenders_payment_terminal_id",
                table: "sale_exchange_settlement_tenders",
                column: "payment_terminal_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_exchange_settlement_tenders_sale_exchange_settlement_id",
                table: "sale_exchange_settlement_tenders",
                column: "sale_exchange_settlement_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_exchange_settlements_movement_date",
                table: "sale_exchange_settlements",
                column: "movement_date");

            migrationBuilder.CreateIndex(
                name: "ix_sale_exchange_settlements_sale_exchange_id",
                table: "sale_exchange_settlements",
                column: "sale_exchange_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_financial_movements_sale_exchange_settlement_tenders_sale_e",
                table: "financial_movements",
                column: "sale_exchange_settlement_tender_id",
                principalTable: "sale_exchange_settlement_tenders",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_financial_movements_sale_exchange_settlement_tenders_sale_e",
                table: "financial_movements");

            migrationBuilder.DropTable(
                name: "sale_exchange_settlement_tenders");

            migrationBuilder.DropTable(
                name: "sale_exchange_settlements");

            migrationBuilder.DropIndex(
                name: "ix_financial_movements_sale_exchange_settlement_tender_id",
                table: "financial_movements");

            migrationBuilder.DeleteData(
                table: "financial_movement_types",
                keyColumn: "id",
                keyValue: 13);

            migrationBuilder.DropColumn(
                name: "sale_exchange_settlement_tender_id",
                table: "financial_movements");
        }
    }
}
