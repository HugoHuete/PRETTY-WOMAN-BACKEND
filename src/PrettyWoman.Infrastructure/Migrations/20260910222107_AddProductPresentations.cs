using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PrettyWoman.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductPresentations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_product_variants_product_id_size_id_variant",
                table: "product_variants");

            migrationBuilder.CreateTable(
                name: "product_presentations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    normalized_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_presentations", x => x.id);
                    table.UniqueConstraint("ak_product_presentations_id_product_id", x => new { x.id, x.product_id });
                    table.CheckConstraint("ck_product_presentations_sort_order_non_negative", "sort_order >= 0");
                    table.ForeignKey(
                        name: "fk_product_presentations_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddColumn<int>(
                name: "product_presentation_id",
                table: "product_variants",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "product_presentation_id",
                table: "product_images",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql("""
                CREATE TEMP TABLE product_variant_deduplication ON COMMIT DROP AS
                WITH ranked_variants AS (
                    SELECT
                        id AS duplicate_id,
                        FIRST_VALUE(id) OVER (
                            PARTITION BY product_id, size_id, NULLIF(UPPER(BTRIM(variant)), '')
                            ORDER BY id) AS canonical_id
                    FROM product_variants
                )
                SELECT duplicate_id, canonical_id
                FROM ranked_variants
                WHERE duplicate_id <> canonical_id;

                UPDATE purchase_shortages AS duplicate
                SET product_id = mapping.canonical_id
                FROM product_variant_deduplication AS mapping
                WHERE duplicate.product_id = mapping.duplicate_id
                  AND NOT EXISTS (
                      SELECT 1
                      FROM purchase_shortages AS canonical
                      WHERE canonical.product_id = mapping.canonical_id);

                UPDATE purchase_shortages AS canonical
                SET quantity = canonical.quantity + duplicates.quantity,
                    loss_amount_nio = canonical.loss_amount_nio + duplicates.loss_amount_nio
                FROM (
                    SELECT
                        mapping.canonical_id,
                        SUM(shortage.quantity) AS quantity,
                        SUM(shortage.loss_amount_nio) AS loss_amount_nio
                    FROM product_variant_deduplication AS mapping
                    JOIN purchase_shortages AS shortage ON shortage.product_id = mapping.duplicate_id
                    GROUP BY mapping.canonical_id
                ) AS duplicates
                WHERE canonical.product_id = duplicates.canonical_id;

                DELETE FROM purchase_shortages AS duplicate
                USING product_variant_deduplication AS mapping
                WHERE duplicate.product_id = mapping.duplicate_id;

                WITH campaign_targets AS (
                    SELECT
                        item.id,
                        item.discount_campaign_id,
                        COALESCE(mapping.canonical_id, item.product_variant_id) AS canonical_variant_id,
                        ROW_NUMBER() OVER (
                            PARTITION BY item.discount_campaign_id,
                                COALESCE(mapping.canonical_id, item.product_variant_id)
                            ORDER BY CASE WHEN mapping.duplicate_id IS NULL THEN 0 ELSE 1 END,
                                item.id) AS duplicate_rank
                    FROM discount_campaign_products AS item
                    LEFT JOIN product_variant_deduplication AS mapping
                        ON mapping.duplicate_id = item.product_variant_id
                    WHERE item.product_variant_id IS NOT NULL
                )
                DELETE FROM discount_campaign_products AS item
                USING campaign_targets AS target
                WHERE item.id = target.id
                  AND target.duplicate_rank > 1;

                WITH duplicate_totals AS (
                    SELECT
                        mapping.canonical_id,
                        SUM(variant.quantity) AS quantity,
                        SUM(variant.received_quantity) AS received_quantity,
                        SUM(variant.available_quantity) AS available_quantity,
                        SUM(variant.reserved_quantity) AS reserved_quantity,
                        SUM(variant.unavailable_quantity) AS unavailable_quantity,
                        SUM(variant.merchandise_total_cost_nio) AS merchandise_total_cost_nio,
                        SUM(variant.allocated_shipping_cost_nio) AS allocated_shipping_cost_nio,
                        SUM(variant.total_cost_nio) AS total_cost_nio,
                        SUM(variant.unit_cost_usd * variant.quantity) AS unit_cost_usd
                    FROM product_variant_deduplication AS mapping
                    JOIN product_variants AS variant ON variant.id = mapping.duplicate_id
                    GROUP BY mapping.canonical_id
                )
                UPDATE product_variants AS canonical
                SET quantity = canonical.quantity + duplicate_totals.quantity,
                    received_quantity = canonical.received_quantity + duplicate_totals.received_quantity,
                    available_quantity = canonical.available_quantity + duplicate_totals.available_quantity,
                    reserved_quantity = canonical.reserved_quantity + duplicate_totals.reserved_quantity,
                    unavailable_quantity = canonical.unavailable_quantity + duplicate_totals.unavailable_quantity,
                    merchandise_total_cost_nio = canonical.merchandise_total_cost_nio + duplicate_totals.merchandise_total_cost_nio,
                    allocated_shipping_cost_nio = canonical.allocated_shipping_cost_nio + duplicate_totals.allocated_shipping_cost_nio,
                    total_cost_nio = canonical.total_cost_nio + duplicate_totals.total_cost_nio,
                    unit_cost_usd = CASE
                        WHEN canonical.quantity + duplicate_totals.quantity = 0 THEN 0
                        ELSE (canonical.unit_cost_usd * canonical.quantity + duplicate_totals.unit_cost_usd)
                            / (canonical.quantity + duplicate_totals.quantity)
                    END,
                    unit_cost_nio = CASE
                        WHEN canonical.quantity + duplicate_totals.quantity = 0 THEN 0
                        ELSE (canonical.total_cost_nio + duplicate_totals.total_cost_nio)
                            / (canonical.quantity + duplicate_totals.quantity)
                    END
                FROM duplicate_totals
                WHERE canonical.id = duplicate_totals.canonical_id;

                UPDATE inventory_movements AS item
                SET product_id = mapping.canonical_id
                FROM product_variant_deduplication AS mapping
                WHERE item.product_id = mapping.duplicate_id;

                UPDATE product_holds AS item
                SET product_id = mapping.canonical_id
                FROM product_variant_deduplication AS mapping
                WHERE item.product_id = mapping.duplicate_id;

                UPDATE product_inventory_issues AS item
                SET product_id = mapping.canonical_id
                FROM product_variant_deduplication AS mapping
                WHERE item.product_id = mapping.duplicate_id;

                UPDATE product_receipt_details AS item
                SET product_id = mapping.canonical_id
                FROM product_variant_deduplication AS mapping
                WHERE item.product_id = mapping.duplicate_id;

                UPDATE inventory_adjustment_items AS item
                SET product_id = mapping.canonical_id
                FROM product_variant_deduplication AS mapping
                WHERE item.product_id = mapping.duplicate_id;

                UPDATE sale_products AS item
                SET product_id = mapping.canonical_id
                FROM product_variant_deduplication AS mapping
                WHERE item.product_id = mapping.duplicate_id;

                UPDATE sale_return_items AS item
                SET product_id = mapping.canonical_id
                FROM product_variant_deduplication AS mapping
                WHERE item.product_id = mapping.duplicate_id;

                UPDATE exchange_outbound_items AS item
                SET product_id = mapping.canonical_id
                FROM product_variant_deduplication AS mapping
                WHERE item.product_id = mapping.duplicate_id;

                UPDATE exchange_return_items AS item
                SET product_id = mapping.canonical_id
                FROM product_variant_deduplication AS mapping
                WHERE item.product_id = mapping.duplicate_id;

                UPDATE discount_campaign_products AS item
                SET product_variant_id = mapping.canonical_id
                FROM product_variant_deduplication AS mapping
                WHERE item.product_variant_id = mapping.duplicate_id;

                DELETE FROM product_variants AS duplicate
                USING product_variant_deduplication AS mapping
                WHERE duplicate.id = mapping.duplicate_id;

                INSERT INTO product_presentations (product_id, name, normalized_name, sort_order)
                SELECT
                    grouped.product_id,
                    grouped.normalized_name,
                    grouped.normalized_name,
                    ROW_NUMBER() OVER (
                        PARTITION BY grouped.product_id
                        ORDER BY grouped.normalized_name NULLS FIRST) - 1
                FROM (
                    SELECT DISTINCT
                        product_id,
                        NULLIF(UPPER(BTRIM(variant)), '') AS normalized_name
                    FROM product_variants
                ) AS grouped;

                UPDATE product_variants AS variant
                SET product_presentation_id = presentation.id
                FROM product_presentations AS presentation
                WHERE presentation.product_id = variant.product_id
                  AND presentation.normalized_name IS NOT DISTINCT FROM
                      NULLIF(UPPER(BTRIM(variant.variant)), '');

                DO $migration$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM product_variants
                        WHERE product_presentation_id IS NULL
                    ) THEN
                        RAISE EXCEPTION 'Product presentation backfill left unmatched product variants';
                    END IF;
                END
                $migration$;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "product_presentation_id",
                table: "product_variants",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "variant",
                table: "product_variants");

            migrationBuilder.CreateIndex(
                name: "ix_product_variants_product_id",
                table: "product_variants",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_variants_product_presentation_id_product_id",
                table: "product_variants",
                columns: new[] { "product_presentation_id", "product_id" });

            migrationBuilder.CreateIndex(
                name: "ix_product_variants_product_presentation_id_size_id",
                table: "product_variants",
                columns: new[] { "product_presentation_id", "size_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_product_images_product_presentation_id_product_id",
                table: "product_images",
                columns: new[] { "product_presentation_id", "product_id" });

            migrationBuilder.DropIndex(
                name: "ix_product_images_product_id_is_primary",
                table: "product_images");

            migrationBuilder.CreateIndex(
                name: "ix_product_images_product_id_general_primary",
                table: "product_images",
                column: "product_id",
                unique: true,
                filter: "is_primary = true AND product_presentation_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_product_images_product_id_presentation_primary",
                table: "product_images",
                columns: new[] { "product_id", "product_presentation_id" },
                unique: true,
                filter: "is_primary = true AND product_presentation_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_product_presentations_product_id",
                table: "product_presentations",
                column: "product_id",
                unique: true,
                filter: "normalized_name is null");

            migrationBuilder.CreateIndex(
                name: "ix_product_presentations_product_id_normalized_name",
                table: "product_presentations",
                columns: new[] { "product_id", "normalized_name" },
                unique: true,
                filter: "normalized_name is not null");

            migrationBuilder.AddForeignKey(
                name: "fk_product_images_product_presentations_product_presentation_i",
                table: "product_images",
                columns: new[] { "product_presentation_id", "product_id" },
                principalTable: "product_presentations",
                principalColumns: new[] { "id", "product_id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_product_variants_product_presentations_product_presentation",
                table: "product_variants",
                columns: new[] { "product_presentation_id", "product_id" },
                principalTable: "product_presentations",
                principalColumns: new[] { "id", "product_id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "variant",
                table: "product_variants",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE product_variants AS variant
                SET variant = presentation.name
                FROM product_presentations AS presentation
                WHERE presentation.id = variant.product_presentation_id
                  AND presentation.product_id = variant.product_id;
                """);

            migrationBuilder.DropForeignKey(
                name: "fk_product_images_product_presentations_product_presentation_i",
                table: "product_images");

            migrationBuilder.DropForeignKey(
                name: "fk_product_variants_product_presentations_product_presentation",
                table: "product_variants");

            migrationBuilder.DropTable(
                name: "product_presentations");

            migrationBuilder.DropIndex(
                name: "ix_product_variants_product_id",
                table: "product_variants");

            migrationBuilder.DropIndex(
                name: "ix_product_variants_product_presentation_id_product_id",
                table: "product_variants");

            migrationBuilder.DropIndex(
                name: "ix_product_variants_product_presentation_id_size_id",
                table: "product_variants");

            migrationBuilder.DropIndex(
                name: "ix_product_images_product_presentation_id_product_id",
                table: "product_images");

            migrationBuilder.DropIndex(
                name: "ix_product_images_product_id_general_primary",
                table: "product_images");

            migrationBuilder.DropIndex(
                name: "ix_product_images_product_id_presentation_primary",
                table: "product_images");

            migrationBuilder.CreateIndex(
                name: "ix_product_images_product_id_is_primary",
                table: "product_images",
                columns: new[] { "product_id", "is_primary" },
                unique: true,
                filter: "is_primary = true");

            migrationBuilder.DropColumn(
                name: "product_presentation_id",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "product_presentation_id",
                table: "product_images");

            migrationBuilder.CreateIndex(
                name: "ix_product_variants_product_id_size_id_variant",
                table: "product_variants",
                columns: new[] { "product_id", "size_id", "variant" });
        }
    }
}
