using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSaleProductStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_sale_products_sale_product_statuses_sale_product_status_id",
                table: "sale_products");

            migrationBuilder.DropTable(
                name: "sale_product_statuses");

            migrationBuilder.DropIndex(
                name: "ix_sale_products_sale_id_sale_product_status_id",
                table: "sale_products");

            migrationBuilder.DropIndex(
                name: "ix_sale_products_sale_product_status_id",
                table: "sale_products");

            migrationBuilder.DropColumn(
                name: "sale_product_status_id",
                table: "sale_products");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "sale_product_status_id",
                table: "sale_products",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.CreateTable(
                name: "sale_product_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sale_product_statuses", x => x.id);
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

            migrationBuilder.CreateIndex(
                name: "ix_sale_products_sale_id_sale_product_status_id",
                table: "sale_products",
                columns: new[] { "sale_id", "sale_product_status_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sale_products_sale_product_status_id",
                table: "sale_products",
                column: "sale_product_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_product_statuses_name",
                table: "sale_product_statuses",
                column: "name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_sale_products_sale_product_statuses_sale_product_status_id",
                table: "sale_products",
                column: "sale_product_status_id",
                principalTable: "sale_product_statuses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
