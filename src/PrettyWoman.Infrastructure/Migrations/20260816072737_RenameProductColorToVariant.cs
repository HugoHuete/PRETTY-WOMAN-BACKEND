using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameProductColorToVariant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "color",
                table: "products",
                newName: "variant");

            migrationBuilder.RenameIndex(
                name: "ix_products_product_detail_id_size_id_color",
                table: "products",
                newName: "ix_products_product_detail_id_size_id_variant");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "variant",
                table: "products",
                newName: "color");

            migrationBuilder.RenameIndex(
                name: "ix_products_product_detail_id_size_id_variant",
                table: "products",
                newName: "ix_products_product_detail_id_size_id_color");
        }
    }
}
