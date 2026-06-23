using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProductInventoryIssues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_products_available_reserved_not_greater_than_received",
                table: "products");

            migrationBuilder.DeleteData(
                table: "product_hold_statuses",
                keyColumn: "id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "product_hold_statuses",
                keyColumn: "id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "product_hold_statuses",
                keyColumn: "id",
                keyValue: 6);

            migrationBuilder.AddColumn<int>(
                name: "unavailable_quantity",
                table: "products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "product_inventory_issue_id",
                table: "inventory_movements",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "product_inventory_issue_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_inventory_issue_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "product_inventory_issue_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_inventory_issue_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "product_inventory_issues",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    product_inventory_issue_type_id = table.Column<int>(type: "integer", nullable: false),
                    product_inventory_issue_status_id = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    comments = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_inventory_issues", x => x.id);
                    table.CheckConstraint("ck_product_inventory_issues_quantity_positive", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_product_inventory_issues_product_inventory_issue_statuses_p",
                        column: x => x.product_inventory_issue_status_id,
                        principalTable: "product_inventory_issue_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_inventory_issues_product_inventory_issue_types_prod",
                        column: x => x.product_inventory_issue_type_id,
                        principalTable: "product_inventory_issue_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_product_inventory_issues_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "product_inventory_issue_statuses",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 1, "Open" },
                    { 2, "ResolvedToAvailable" },
                    { 3, "Discarded" },
                    { 4, "ConfirmedLost" },
                    { 5, "Cancelled" }
                });

            migrationBuilder.InsertData(
                table: "product_inventory_issue_types",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 1, "Damaged" },
                    { 2, "Dirty" },
                    { 3, "Missing" },
                    { 4, "UnderReview" },
                    { 5, "Repairing" }
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_stock_not_greater_than_received",
                table: "products",
                sql: "available_quantity + reserved_quantity + unavailable_quantity <= received_quantity");

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_unavailable_quantity_non_negative",
                table: "products",
                sql: "unavailable_quantity >= 0");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_product_inventory_issue_id",
                table: "inventory_movements",
                column: "product_inventory_issue_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_inventory_issue_statuses_name",
                table: "product_inventory_issue_statuses",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_product_inventory_issue_types_name",
                table: "product_inventory_issue_types",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_product_inventory_issues_created_at",
                table: "product_inventory_issues",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_product_inventory_issues_product_id",
                table: "product_inventory_issues",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_inventory_issues_product_inventory_issue_status_id",
                table: "product_inventory_issues",
                column: "product_inventory_issue_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_inventory_issues_product_inventory_issue_type_id",
                table: "product_inventory_issues",
                column: "product_inventory_issue_type_id");

            migrationBuilder.AddForeignKey(
                name: "fk_inventory_movements_product_inventory_issues_product_invent",
                table: "inventory_movements",
                column: "product_inventory_issue_id",
                principalTable: "product_inventory_issues",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_inventory_movements_product_inventory_issues_product_invent",
                table: "inventory_movements");

            migrationBuilder.DropTable(
                name: "product_inventory_issues");

            migrationBuilder.DropTable(
                name: "product_inventory_issue_statuses");

            migrationBuilder.DropTable(
                name: "product_inventory_issue_types");

            migrationBuilder.DropCheckConstraint(
                name: "ck_products_stock_not_greater_than_received",
                table: "products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_products_unavailable_quantity_non_negative",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_inventory_movements_product_inventory_issue_id",
                table: "inventory_movements");

            migrationBuilder.DropColumn(
                name: "unavailable_quantity",
                table: "products");

            migrationBuilder.DropColumn(
                name: "product_inventory_issue_id",
                table: "inventory_movements");

            migrationBuilder.InsertData(
                table: "product_hold_statuses",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 4, "Found" },
                    { 5, "Repaired" },
                    { 6, "Discarded" }
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_available_reserved_not_greater_than_received",
                table: "products",
                sql: "available_quantity + reserved_quantity <= received_quantity");
        }
    }
}
