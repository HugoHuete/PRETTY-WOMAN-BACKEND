using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceVariantDiscountTargetsWithPresentations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_discount_campaign_products_product_variants_product_variant",
                table: "discount_campaign_products");

            migrationBuilder.DropIndex(
                name: "ix_discount_campaign_products_discount_campaign_id_product_var",
                table: "discount_campaign_products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_discount_campaign_product_exactly_one_target",
                table: "discount_campaign_products");

            migrationBuilder.RenameColumn(
                name: "product_variant_id",
                table: "discount_campaign_products",
                newName: "product_presentation_id");

            migrationBuilder.RenameIndex(
                name: "ix_discount_campaign_products_product_variant_id",
                table: "discount_campaign_products",
                newName: "ix_discount_campaign_products_product_presentation_id");

            migrationBuilder.CreateIndex(
                name: "ix_discount_campaign_products_discount_campaign_id_product_pre",
                table: "discount_campaign_products",
                columns: new[] { "discount_campaign_id", "product_presentation_id" },
                unique: true,
                filter: "product_presentation_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_discount_campaign_product_exactly_one_target",
                table: "discount_campaign_products",
                sql: "(product_id IS NOT NULL) <> (product_presentation_id IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "fk_discount_campaign_products_product_presentations_product_pr",
                table: "discount_campaign_products",
                column: "product_presentation_id",
                principalTable: "product_presentations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_discount_campaign_products_product_presentations_product_pr",
                table: "discount_campaign_products");

            migrationBuilder.DropIndex(
                name: "ix_discount_campaign_products_discount_campaign_id_product_pre",
                table: "discount_campaign_products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_discount_campaign_product_exactly_one_target",
                table: "discount_campaign_products");

            migrationBuilder.RenameColumn(
                name: "product_presentation_id",
                table: "discount_campaign_products",
                newName: "product_variant_id");

            migrationBuilder.RenameIndex(
                name: "ix_discount_campaign_products_product_presentation_id",
                table: "discount_campaign_products",
                newName: "ix_discount_campaign_products_product_variant_id");

            migrationBuilder.CreateIndex(
                name: "ix_discount_campaign_products_discount_campaign_id_product_var",
                table: "discount_campaign_products",
                columns: new[] { "discount_campaign_id", "product_variant_id" },
                unique: true,
                filter: "product_variant_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_discount_campaign_product_exactly_one_target",
                table: "discount_campaign_products",
                sql: "(product_id IS NOT NULL) <> (product_variant_id IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "fk_discount_campaign_products_product_variants_product_variant",
                table: "discount_campaign_products",
                column: "product_variant_id",
                principalTable: "product_variants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
