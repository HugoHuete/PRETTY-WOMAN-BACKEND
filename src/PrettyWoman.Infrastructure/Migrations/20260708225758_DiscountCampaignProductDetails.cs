using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DiscountCampaignProductDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_discount_campaign_products_products_product_id",
                table: "discount_campaign_products");

            migrationBuilder.DropIndex(
                name: "ix_discount_campaign_products_discount_campaign_id_product_id",
                table: "discount_campaign_products");

            migrationBuilder.DropIndex(
                name: "ix_discount_campaign_products_product_id",
                table: "discount_campaign_products");

            migrationBuilder.AddColumn<int>(
                name: "product_detail_id",
                table: "discount_campaign_products",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql("UPDATE discount_campaign_products AS discount SET product_detail_id = product.product_detail_id FROM products AS product WHERE product.id = discount.product_id;");

            migrationBuilder.Sql(@"
DELETE FROM discount_campaign_products AS duplicate
USING discount_campaign_products AS kept
WHERE duplicate.id > kept.id
  AND duplicate.discount_campaign_id = kept.discount_campaign_id
  AND duplicate.product_detail_id = kept.product_detail_id
  AND duplicate.discount_type_id = kept.discount_type_id
  AND duplicate.discount_value = kept.discount_value;");

            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM discount_campaign_products
        GROUP BY discount_campaign_id, product_detail_id
        HAVING COUNT(*) > 1
    ) THEN
        RAISE EXCEPTION 'No se puede migrar discount_campaign_products a product_detail_id porque existen variantes del mismo producto detalle con descuentos distintos en la misma campania.';
    END IF;
END $$;");

            migrationBuilder.AlterColumn<int>(
                name: "product_detail_id",
                table: "discount_campaign_products",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "product_id",
                table: "discount_campaign_products");

            migrationBuilder.CreateIndex(
                name: "ix_discount_campaign_products_discount_campaign_id_product_det",
                table: "discount_campaign_products",
                columns: new[] { "discount_campaign_id", "product_detail_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_discount_campaign_products_product_detail_id",
                table: "discount_campaign_products",
                column: "product_detail_id");

            migrationBuilder.AddForeignKey(
                name: "fk_discount_campaign_products_product_details_product_detail_id",
                table: "discount_campaign_products",
                column: "product_detail_id",
                principalTable: "product_details",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_discount_campaign_products_product_details_product_detail_id",
                table: "discount_campaign_products");

            migrationBuilder.DropIndex(
                name: "ix_discount_campaign_products_discount_campaign_id_product_det",
                table: "discount_campaign_products");

            migrationBuilder.DropIndex(
                name: "ix_discount_campaign_products_product_detail_id",
                table: "discount_campaign_products");

            migrationBuilder.AddColumn<int>(
                name: "product_id",
                table: "discount_campaign_products",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE discount_campaign_products AS discount
SET product_id = source.product_id
FROM (
    SELECT product_detail_id, MIN(id) AS product_id
    FROM products
    GROUP BY product_detail_id
) AS source
WHERE source.product_detail_id = discount.product_detail_id;");

            migrationBuilder.AlterColumn<int>(
                name: "product_id",
                table: "discount_campaign_products",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "product_detail_id",
                table: "discount_campaign_products");

            migrationBuilder.CreateIndex(
                name: "ix_discount_campaign_products_discount_campaign_id_product_id",
                table: "discount_campaign_products",
                columns: new[] { "discount_campaign_id", "product_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_discount_campaign_products_product_id",
                table: "discount_campaign_products",
                column: "product_id");

            migrationBuilder.AddForeignKey(
                name: "fk_discount_campaign_products_products_product_id",
                table: "discount_campaign_products",
                column: "product_id",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}