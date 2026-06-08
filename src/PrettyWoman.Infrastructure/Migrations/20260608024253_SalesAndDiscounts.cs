using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SalesAndDiscounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "clients",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    instagram_user = table.Column<string>(type: "text", nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_purchase_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_blocked = table.Column<bool>(type: "boolean", nullable: false),
                    is_friend = table.Column<bool>(type: "boolean", nullable: false),
                    blocked_reason = table.Column<string>(type: "text", nullable: true),
                    comments = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clients", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "discount_campaigns",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_discount_campaigns", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "discount_sources",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_discount_sources", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "discount_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_discount_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sale_channels",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sale_channels", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sale_product_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sale_product_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sale_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sale_statuses", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "discount_sources",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 1, "None" },
                    { 2, "Campaign" },
                    { 3, "Manual" },
                    { 4, "Employee" }
                });

            migrationBuilder.InsertData(
                table: "discount_types",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 1, "FixedAmount" },
                    { 2, "Percentage" },
                    { 3, "FixedPrice" }
                });

            migrationBuilder.InsertData(
                table: "sale_channels",
                columns: new[] { "id", "enabled", "name" },
                values: new object[,]
                {
                    { 1, true, "InStoreSale" },
                    { 2, true, "Whatsapp" },
                    { 3, true, "Instagram" },
                    { 4, true, "Messenger" },
                    { 5, true, "Others" }
                });

            migrationBuilder.InsertData(
                table: "sale_product_statuses",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 1, "Pending" },
                    { 2, "Completed" },
                    { 3, "Refunded" },
                    { 4, "Changed" },
                    { 5, "Cancelled" }
                });

            migrationBuilder.InsertData(
                table: "sale_statuses",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 1, "Pending" },
                    { 2, "Completed" },
                    { 3, "Cancelled" }
                });

            migrationBuilder.Sql("ALTER TABLE discount_sources ALTER COLUMN id RESTART WITH 5;");
            migrationBuilder.Sql("ALTER TABLE discount_types ALTER COLUMN id RESTART WITH 4;");
            migrationBuilder.Sql("ALTER TABLE sale_channels ALTER COLUMN id RESTART WITH 6;");
            migrationBuilder.Sql("ALTER TABLE sale_product_statuses ALTER COLUMN id RESTART WITH 6;");
            migrationBuilder.Sql("ALTER TABLE sale_statuses ALTER COLUMN id RESTART WITH 4;");

            migrationBuilder.CreateTable(
                name: "discount_campaign_products",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    discount_campaign_id = table.Column<int>(type: "integer", nullable: false),
                    discount_type_id = table.Column<int>(type: "integer", nullable: false),
                    discount_value = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_discount_campaign_products", x => x.id);
                    table.ForeignKey(
                        name: "fk_discount_campaign_products_discount_campaigns_discount_camp",
                        column: x => x.discount_campaign_id,
                        principalTable: "discount_campaigns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_discount_campaign_products_discount_types_discount_type_id",
                        column: x => x.discount_type_id,
                        principalTable: "discount_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_discount_campaign_products_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sales",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    sale_channel_id = table.Column<int>(type: "integer", nullable: false),
                    sale_status_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    subtotal_before_discount = table.Column<decimal>(type: "numeric", nullable: false),
                    total_discount = table.Column<decimal>(type: "numeric", nullable: false),
                    sub_total = table.Column<decimal>(type: "numeric", nullable: false),
                    comission = table.Column<decimal>(type: "numeric", nullable: false),
                    total = table.Column<decimal>(type: "numeric", nullable: false),
                    comments = table.Column<string>(type: "text", nullable: true),
                    client_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales", x => x.id);
                    table.ForeignKey(
                        name: "fk_sales_clients_client_id",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_sales_sale_channels_sale_channel_id",
                        column: x => x.sale_channel_id,
                        principalTable: "sale_channels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_sales_sale_statuses_sale_status_id",
                        column: x => x.sale_status_id,
                        principalTable: "sale_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_holds",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    sale_id = table.Column<int>(type: "integer", nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    hold_reason = table.Column<string>(type: "text", nullable: false),
                    comments = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_holds", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_holds_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_product_holds_sales_sale_id",
                        column: x => x.sale_id,
                        principalTable: "sales",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "sale_products",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sale_id = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    cost_at_sale = table.Column<decimal>(type: "numeric", nullable: false),
                    original_sale_price = table.Column<decimal>(type: "numeric", nullable: false),
                    discount_source_id = table.Column<int>(type: "integer", nullable: false),
                    discount_campaign_id = table.Column<int>(type: "integer", nullable: true),
                    discount_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    final_sale_price = table.Column<decimal>(type: "numeric", nullable: false),
                    payment_comission = table.Column<decimal>(type: "numeric", nullable: false),
                    gross_profit = table.Column<decimal>(type: "numeric", nullable: false),
                    sale_product_status_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sale_products", x => x.id);
                    table.ForeignKey(
                        name: "fk_sale_products_discount_campaigns_discount_campaign_id",
                        column: x => x.discount_campaign_id,
                        principalTable: "discount_campaigns",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_sale_products_discount_sources_discount_source_id",
                        column: x => x.discount_source_id,
                        principalTable: "discount_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_sale_products_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_sale_products_sale_product_statuses_sale_product_status_id",
                        column: x => x.sale_product_status_id,
                        principalTable: "sale_product_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_sale_products_sales_sale_id",
                        column: x => x.sale_id,
                        principalTable: "sales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_discount_campaign_products_discount_campaign_id",
                table: "discount_campaign_products",
                column: "discount_campaign_id");

            migrationBuilder.CreateIndex(
                name: "ix_discount_campaign_products_discount_type_id",
                table: "discount_campaign_products",
                column: "discount_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_discount_campaign_products_product_id",
                table: "discount_campaign_products",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_holds_product_id",
                table: "product_holds",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_holds_sale_id",
                table: "product_holds",
                column: "sale_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_products_discount_campaign_id",
                table: "sale_products",
                column: "discount_campaign_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_products_discount_source_id",
                table: "sale_products",
                column: "discount_source_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_products_product_id",
                table: "sale_products",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_products_sale_id",
                table: "sale_products",
                column: "sale_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_products_sale_product_status_id",
                table: "sale_products",
                column: "sale_product_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_client_id",
                table: "sales",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_sale_channel_id",
                table: "sales",
                column: "sale_channel_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_sale_status_id",
                table: "sales",
                column: "sale_status_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "discount_campaign_products");

            migrationBuilder.DropTable(
                name: "product_holds");

            migrationBuilder.DropTable(
                name: "sale_products");

            migrationBuilder.DropTable(
                name: "discount_types");

            migrationBuilder.DropTable(
                name: "discount_campaigns");

            migrationBuilder.DropTable(
                name: "discount_sources");

            migrationBuilder.DropTable(
                name: "sale_product_statuses");

            migrationBuilder.DropTable(
                name: "sales");

            migrationBuilder.DropTable(
                name: "clients");

            migrationBuilder.DropTable(
                name: "sale_channels");

            migrationBuilder.DropTable(
                name: "sale_statuses");
        }
    }
}
