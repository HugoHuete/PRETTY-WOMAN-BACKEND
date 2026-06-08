using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PaymentsAndDeliveries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "municipality_id",
                table: "sales",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "delivery_agencies",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    phone_number = table.Column<string>(type: "text", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_delivery_agencies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "delivery_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_delivery_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "departments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_departments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payment_methods",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_methods", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payment_terminals",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    comission_percentage = table.Column<decimal>(type: "numeric", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_terminals", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "delivery_statuses",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 1, "Pending" },
                    { 2, "Completed" },
                    { 3, "Cancelled" }
                });

            migrationBuilder.InsertData(
                table: "payment_methods",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 1, "Cash" },
                    { 2, "Transfer" },
                    { 3, "Card" }
                });

            migrationBuilder.Sql("ALTER TABLE delivery_statuses ALTER COLUMN id RESTART WITH 4;");
            migrationBuilder.Sql("ALTER TABLE payment_methods ALTER COLUMN id RESTART WITH 4;");

            migrationBuilder.CreateTable(
                name: "municipalities",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    department_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_municipalities", x => x.id);
                    table.ForeignKey(
                        name: "fk_municipalities_departments_department_id",
                        column: x => x.department_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sale_payments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    sale_id = table.Column<int>(type: "integer", nullable: false),
                    payment_method_id = table.Column<int>(type: "integer", nullable: false),
                    payment_terminal_id = table.Column<int>(type: "integer", nullable: true),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    comission_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    net_received_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sale_payments", x => x.id);
                    table.ForeignKey(
                        name: "fk_sale_payments_payment_methods_payment_method_id",
                        column: x => x.payment_method_id,
                        principalTable: "payment_methods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_sale_payments_payment_terminals_payment_terminal_id",
                        column: x => x.payment_terminal_id,
                        principalTable: "payment_terminals",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_sale_payments_sales_sale_id",
                        column: x => x.sale_id,
                        principalTable: "sales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sale_deliveries",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    sale_id = table.Column<int>(type: "integer", nullable: false),
                    municipality_id = table.Column<int>(type: "integer", nullable: false),
                    delivery_agency_id = table.Column<int>(type: "integer", nullable: false),
                    delivery_status_id = table.Column<int>(type: "integer", nullable: false),
                    client_id = table.Column<int>(type: "integer", nullable: true),
                    amount_to_collect = table.Column<decimal>(type: "numeric", nullable: false),
                    shipping_charged_to_client = table.Column<decimal>(type: "numeric", nullable: false),
                    shipping_paid_to_agency = table.Column<decimal>(type: "numeric", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    comments = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sale_deliveries", x => x.id);
                    table.ForeignKey(
                        name: "fk_sale_deliveries_clients_client_id",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_sale_deliveries_delivery_agencies_delivery_agency_id",
                        column: x => x.delivery_agency_id,
                        principalTable: "delivery_agencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_sale_deliveries_delivery_statuses_delivery_status_id",
                        column: x => x.delivery_status_id,
                        principalTable: "delivery_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_sale_deliveries_municipalities_municipality_id",
                        column: x => x.municipality_id,
                        principalTable: "municipalities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_sale_deliveries_sales_sale_id",
                        column: x => x.sale_id,
                        principalTable: "sales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sales_municipality_id",
                table: "sales",
                column: "municipality_id");

            migrationBuilder.CreateIndex(
                name: "ix_municipalities_department_id",
                table: "municipalities",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_deliveries_client_id",
                table: "sale_deliveries",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_deliveries_delivery_agency_id",
                table: "sale_deliveries",
                column: "delivery_agency_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_deliveries_delivery_status_id",
                table: "sale_deliveries",
                column: "delivery_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_deliveries_municipality_id",
                table: "sale_deliveries",
                column: "municipality_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_deliveries_sale_id",
                table: "sale_deliveries",
                column: "sale_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_payments_payment_method_id",
                table: "sale_payments",
                column: "payment_method_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_payments_payment_terminal_id",
                table: "sale_payments",
                column: "payment_terminal_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_payments_sale_id",
                table: "sale_payments",
                column: "sale_id");

            migrationBuilder.AddForeignKey(
                name: "fk_sales_municipalities_municipality_id",
                table: "sales",
                column: "municipality_id",
                principalTable: "municipalities",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_sales_municipalities_municipality_id",
                table: "sales");

            migrationBuilder.DropTable(
                name: "sale_deliveries");

            migrationBuilder.DropTable(
                name: "sale_payments");

            migrationBuilder.DropTable(
                name: "delivery_agencies");

            migrationBuilder.DropTable(
                name: "delivery_statuses");

            migrationBuilder.DropTable(
                name: "municipalities");

            migrationBuilder.DropTable(
                name: "payment_methods");

            migrationBuilder.DropTable(
                name: "payment_terminals");

            migrationBuilder.DropTable(
                name: "departments");

            migrationBuilder.DropIndex(
                name: "ix_sales_municipality_id",
                table: "sales");

            migrationBuilder.DropColumn(
                name: "municipality_id",
                table: "sales");
        }
    }
}
