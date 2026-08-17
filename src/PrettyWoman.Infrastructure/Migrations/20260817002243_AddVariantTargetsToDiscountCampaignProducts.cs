using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVariantTargetsToDiscountCampaignProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF to_regclass('ix_discount_campaign_products_discount_campaign_id_product_det') IS NOT NULL
                       AND to_regclass('ix_discount_campaign_products_discount_campaign_id_product_id') IS NULL THEN
                        ALTER INDEX ix_discount_campaign_products_discount_campaign_id_product_det
                            RENAME TO ix_discount_campaign_products_discount_campaign_id_product_id;
                    END IF;
                END $$;
                """);

            migrationBuilder.DropIndex(
                name: "ix_discount_campaign_products_discount_campaign_id_product_id",
                table: "discount_campaign_products");

            migrationBuilder.AlterColumn<int>(
                name: "product_id",
                table: "discount_campaign_products",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "product_variant_id",
                table: "discount_campaign_products",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_discount_campaign_products_discount_campaign_id_product_id",
                table: "discount_campaign_products",
                columns: new[] { "discount_campaign_id", "product_id" },
                unique: true,
                filter: "product_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_discount_campaign_products_discount_campaign_id_product_var",
                table: "discount_campaign_products",
                columns: new[] { "discount_campaign_id", "product_variant_id" },
                unique: true,
                filter: "product_variant_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_discount_campaign_products_product_variant_id",
                table: "discount_campaign_products",
                column: "product_variant_id");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_discount_campaign_products_product_variants_product_variant",
                table: "discount_campaign_products");

            migrationBuilder.DropIndex(
                name: "ix_discount_campaign_products_discount_campaign_id_product_id",
                table: "discount_campaign_products");

            migrationBuilder.DropIndex(
                name: "ix_discount_campaign_products_discount_campaign_id_product_var",
                table: "discount_campaign_products");

            migrationBuilder.DropIndex(
                name: "ix_discount_campaign_products_product_variant_id",
                table: "discount_campaign_products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_discount_campaign_product_exactly_one_target",
                table: "discount_campaign_products");

            migrationBuilder.DropColumn(
                name: "product_variant_id",
                table: "discount_campaign_products");

            migrationBuilder.AlterColumn<int>(
                name: "product_id",
                table: "discount_campaign_products",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_discount_campaign_products_discount_campaign_id_product_id",
                table: "discount_campaign_products",
                columns: new[] { "discount_campaign_id", "product_id" },
                unique: true);
        }
    }
}
