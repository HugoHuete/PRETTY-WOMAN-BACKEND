using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using PrettyWoman.Domain.Enums;

#nullable disable

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InventoryAndFinancialMovements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "dollar_exchange_rate_id",
                table: "orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "dollar_exchange_rates",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    store_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    bank_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dollar_exchange_rates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "expense_categories",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_expense_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "financial_movement_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_financial_movement_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "inventory_movement_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_movement_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "loan_owners",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_loan_owners", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "movement_directions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_movement_directions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "loans",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    loan_owner_id = table.Column<int>(type: "integer", nullable: false),
                    initial_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    initial_amount_usd = table.Column<decimal>(type: "numeric", nullable: false),
                    balance = table.Column<decimal>(type: "numeric", nullable: false),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    comments = table.Column<string>(type: "text", nullable: true),
                    dollar_exchange_rate_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_loans", x => x.id);
                    table.ForeignKey(
                        name: "fk_loans_dollar_exchange_rates_dollar_exchange_rate_id",
                        column: x => x.dollar_exchange_rate_id,
                        principalTable: "dollar_exchange_rates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_loans_loan_owners_loan_owner_id",
                        column: x => x.loan_owner_id,
                        principalTable: "loan_owners",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inventory_movements",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    movement_direction_id = table.Column<int>(type: "integer", nullable: false),
                    inventory_movement_type_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    order_id = table.Column<int>(type: "integer", nullable: true),
                    sale_product_id = table.Column<int>(type: "integer", nullable: true),
                    product_hold_id = table.Column<int>(type: "integer", nullable: true),
                    comments = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_movements", x => x.id);
                    table.ForeignKey(
                        name: "fk_inventory_movements_inventory_movement_types_inventory_move",
                        column: x => x.inventory_movement_type_id,
                        principalTable: "inventory_movement_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_inventory_movements_movement_directions_movement_direction_",
                        column: x => x.movement_direction_id,
                        principalTable: "movement_directions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_inventory_movements_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_inventory_movements_product_holds_product_hold_id",
                        column: x => x.product_hold_id,
                        principalTable: "product_holds",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_inventory_movements_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_inventory_movements_sale_products_sale_product_id",
                        column: x => x.sale_product_id,
                        principalTable: "sale_products",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "financial_movements",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    movement_direction_id = table.Column<int>(type: "integer", nullable: false),
                    financial_movement_type_id = table.Column<int>(type: "integer", nullable: false),
                    expense_category_id = table.Column<int>(type: "integer", nullable: true),
                    order_id = table.Column<int>(type: "integer", nullable: true),
                    sale_payment_id = table.Column<int>(type: "integer", nullable: true),
                    loan_id = table.Column<int>(type: "integer", nullable: true),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    dollar_exchange_rate_id = table.Column<int>(type: "integer", nullable: false),
                    comments = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_financial_movements", x => x.id);
                    table.ForeignKey(
                        name: "fk_financial_movements_dollar_exchange_rates_dollar_exchange_r",
                        column: x => x.dollar_exchange_rate_id,
                        principalTable: "dollar_exchange_rates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_financial_movements_expense_categories_expense_category_id",
                        column: x => x.expense_category_id,
                        principalTable: "expense_categories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_financial_movements_financial_movement_types_financial_move",
                        column: x => x.financial_movement_type_id,
                        principalTable: "financial_movement_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_financial_movements_loans_loan_id",
                        column: x => x.loan_id,
                        principalTable: "loans",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_financial_movements_movement_directions_movement_direction_",
                        column: x => x.movement_direction_id,
                        principalTable: "movement_directions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_financial_movements_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_financial_movements_sale_payments_sale_payment_id",
                        column: x => x.sale_payment_id,
                        principalTable: "sale_payments",
                        principalColumn: "id");
                });


            migrationBuilder.InsertData(
                table: "financial_movement_types",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { (int)FinancialMovementTypeOption.OwnerInvestment, nameof(FinancialMovementTypeOption.OwnerInvestment) },
                    { (int)FinancialMovementTypeOption.SupplierPayment, nameof(FinancialMovementTypeOption.SupplierPayment) },
                    { (int)FinancialMovementTypeOption.SalePayment, nameof(FinancialMovementTypeOption.SalePayment) },
                    { (int)FinancialMovementTypeOption.Expense, nameof(FinancialMovementTypeOption.Expense) },
                    { (int)FinancialMovementTypeOption.OwnerWithdrawal, nameof(FinancialMovementTypeOption.OwnerWithdrawal) },
                    { (int)FinancialMovementTypeOption.SupplierRefund, nameof(FinancialMovementTypeOption.SupplierRefund) },
                    { (int)FinancialMovementTypeOption.CustomerRefund, nameof(FinancialMovementTypeOption.CustomerRefund) },
                    { (int)FinancialMovementTypeOption.LoanReceived, nameof(FinancialMovementTypeOption.LoanReceived) },
                    { (int)FinancialMovementTypeOption.LoanPayment, nameof(FinancialMovementTypeOption.LoanPayment) },
                    { (int)FinancialMovementTypeOption.Adjustment, nameof(FinancialMovementTypeOption.Adjustment) }
                });

            migrationBuilder.InsertData(
                table: "inventory_movement_types",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { (int)InventoryMovementTypeOption.PurchaseReceived, nameof(InventoryMovementTypeOption.PurchaseReceived) },
                    { (int)InventoryMovementTypeOption.Sale, nameof(InventoryMovementTypeOption.Sale) },
                    { (int)InventoryMovementTypeOption.SaleCancelled, nameof(InventoryMovementTypeOption.SaleCancelled) },
                    { (int)InventoryMovementTypeOption.CustomerReturn, nameof(InventoryMovementTypeOption.CustomerReturn) },
                    { (int)InventoryMovementTypeOption.ExchangeReturn, nameof(InventoryMovementTypeOption.ExchangeReturn) },
                    { (int)InventoryMovementTypeOption.Damaged, nameof(InventoryMovementTypeOption.Damaged) },
                    { (int)InventoryMovementTypeOption.Repaired, nameof(InventoryMovementTypeOption.Repaired) },
                    { (int)InventoryMovementTypeOption.Lost, nameof(InventoryMovementTypeOption.Lost) },
                    { (int)InventoryMovementTypeOption.Found, nameof(InventoryMovementTypeOption.Found) },
                    { (int)InventoryMovementTypeOption.Discarded, nameof(InventoryMovementTypeOption.Discarded) },
                    { (int)InventoryMovementTypeOption.Donation, nameof(InventoryMovementTypeOption.Donation) },
                    { (int)InventoryMovementTypeOption.AdjustmentIncrease, nameof(InventoryMovementTypeOption.AdjustmentIncrease) },
                    { (int)InventoryMovementTypeOption.AdjustmentDecrease, nameof(InventoryMovementTypeOption.AdjustmentDecrease) },
                    { (int)InventoryMovementTypeOption.ReservationCreated, nameof(InventoryMovementTypeOption.ReservationCreated) },
                    { (int)InventoryMovementTypeOption.ReservationReleased, nameof(InventoryMovementTypeOption.ReservationReleased) },
                    { (int)InventoryMovementTypeOption.ReservationConvertedToSale, nameof(InventoryMovementTypeOption.ReservationConvertedToSale) }
                });

            migrationBuilder.InsertData(
                table: "movement_directions",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { (int)MovementDirectionOptions.In, nameof(MovementDirectionOptions.In) },
                    { (int)MovementDirectionOptions.Out, nameof(MovementDirectionOptions.Out) }
                });

            migrationBuilder.Sql("ALTER TABLE financial_movement_types ALTER COLUMN id RESTART WITH 11;");
            migrationBuilder.Sql("ALTER TABLE inventory_movement_types ALTER COLUMN id RESTART WITH 17;");
            migrationBuilder.Sql("ALTER TABLE movement_directions ALTER COLUMN id RESTART WITH 3;");


            migrationBuilder.CreateIndex(
                name: "ix_orders_dollar_exchange_rate_id",
                table: "orders",
                column: "dollar_exchange_rate_id");

            migrationBuilder.CreateIndex(
                name: "ix_financial_movements_dollar_exchange_rate_id",
                table: "financial_movements",
                column: "dollar_exchange_rate_id");

            migrationBuilder.CreateIndex(
                name: "ix_financial_movements_expense_category_id",
                table: "financial_movements",
                column: "expense_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_financial_movements_financial_movement_type_id",
                table: "financial_movements",
                column: "financial_movement_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_financial_movements_loan_id",
                table: "financial_movements",
                column: "loan_id");

            migrationBuilder.CreateIndex(
                name: "ix_financial_movements_movement_direction_id",
                table: "financial_movements",
                column: "movement_direction_id");

            migrationBuilder.CreateIndex(
                name: "ix_financial_movements_order_id",
                table: "financial_movements",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_financial_movements_sale_payment_id",
                table: "financial_movements",
                column: "sale_payment_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_inventory_movement_type_id",
                table: "inventory_movements",
                column: "inventory_movement_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_movement_direction_id",
                table: "inventory_movements",
                column: "movement_direction_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_order_id",
                table: "inventory_movements",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_product_hold_id",
                table: "inventory_movements",
                column: "product_hold_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_product_id",
                table: "inventory_movements",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_sale_product_id",
                table: "inventory_movements",
                column: "sale_product_id");

            migrationBuilder.CreateIndex(
                name: "ix_loans_dollar_exchange_rate_id",
                table: "loans",
                column: "dollar_exchange_rate_id");

            migrationBuilder.CreateIndex(
                name: "ix_loans_loan_owner_id",
                table: "loans",
                column: "loan_owner_id");

            migrationBuilder.AddForeignKey(
                name: "fk_orders_dollar_exchange_rates_dollar_exchange_rate_id",
                table: "orders",
                column: "dollar_exchange_rate_id",
                principalTable: "dollar_exchange_rates",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_orders_dollar_exchange_rates_dollar_exchange_rate_id",
                table: "orders");

            migrationBuilder.DropTable(
                name: "financial_movements");

            migrationBuilder.DropTable(
                name: "inventory_movements");

            migrationBuilder.DropTable(
                name: "expense_categories");

            migrationBuilder.DropTable(
                name: "financial_movement_types");

            migrationBuilder.DropTable(
                name: "loans");

            migrationBuilder.DropTable(
                name: "inventory_movement_types");

            migrationBuilder.DropTable(
                name: "movement_directions");

            migrationBuilder.DropTable(
                name: "dollar_exchange_rates");

            migrationBuilder.DropTable(
                name: "loan_owners");

            migrationBuilder.DropIndex(
                name: "ix_orders_dollar_exchange_rate_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "dollar_exchange_rate_id",
                table: "orders");
        }
    }
}
