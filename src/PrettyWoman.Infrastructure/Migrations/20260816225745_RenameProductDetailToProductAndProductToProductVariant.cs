using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrettyWoman.Infrastructure.Migrations;

public partial class RenameProductDetailToProductAndProductToProductVariant : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE product_details RENAME TO products_legacy;
            ALTER TABLE products RENAME TO product_variants;
            ALTER TABLE products_legacy RENAME TO products;

            ALTER TABLE product_images RENAME COLUMN product_detail_id TO product_id;
            ALTER TABLE discount_campaign_products RENAME COLUMN product_detail_id TO product_id;
            ALTER TABLE product_variants RENAME COLUMN product_detail_id TO product_id;

            DO $$
            BEGIN
                IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'pk_products' AND conrelid = 'product_variants'::regclass) THEN
                    ALTER TABLE product_variants RENAME CONSTRAINT pk_products TO pk_product_variants_legacy;
                END IF;
                IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'pk_product_details' AND conrelid = 'products'::regclass) THEN
                    ALTER TABLE products RENAME CONSTRAINT pk_product_details TO pk_products;
                END IF;
                IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'pk_product_variants_legacy' AND conrelid = 'product_variants'::regclass) THEN
                    ALTER TABLE product_variants RENAME CONSTRAINT pk_product_variants_legacy TO pk_product_variants;
                END IF;
                IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_discount_campaign_products_product_details_product_detail_id' AND conrelid = 'discount_campaign_products'::regclass) THEN
                    ALTER TABLE discount_campaign_products RENAME CONSTRAINT fk_discount_campaign_products_product_details_product_detail_id TO fk_discount_campaign_products_products_product_id;
                END IF;
                IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_exchange_outbound_items_products_product_id' AND conrelid = 'exchange_outbound_items'::regclass) THEN
                    ALTER TABLE exchange_outbound_items RENAME CONSTRAINT fk_exchange_outbound_items_products_product_id TO fk_exchange_outbound_items_product_variants_product_id;
                END IF;
                IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_exchange_return_items_products_product_id' AND conrelid = 'exchange_return_items'::regclass) THEN
                    ALTER TABLE exchange_return_items RENAME CONSTRAINT fk_exchange_return_items_products_product_id TO fk_exchange_return_items_product_variants_product_id;
                END IF;
                IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_inventory_adjustment_items_products_product_id' AND conrelid = 'inventory_adjustment_items'::regclass) THEN
                    ALTER TABLE inventory_adjustment_items RENAME CONSTRAINT fk_inventory_adjustment_items_products_product_id TO fk_inventory_adjustment_items_product_variants_product_id;
                END IF;
                IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_inventory_movements_products_product_id' AND conrelid = 'inventory_movements'::regclass) THEN
                    ALTER TABLE inventory_movements RENAME CONSTRAINT fk_inventory_movements_products_product_id TO fk_inventory_movements_product_variants_product_id;
                END IF;
                IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_products_orders_order_id' AND conrelid = 'product_variants'::regclass) THEN
                    ALTER TABLE product_variants RENAME CONSTRAINT fk_products_orders_order_id TO fk_product_variants_orders_order_id;
                END IF;
                IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_products_product_details_product_detail_id' AND conrelid = 'product_variants'::regclass) THEN
                    ALTER TABLE product_variants RENAME CONSTRAINT fk_products_product_details_product_detail_id TO fk_product_variants_products_product_id;
                END IF;
                IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_products_sizes_size_id' AND conrelid = 'product_variants'::regclass) THEN
                    ALTER TABLE product_variants RENAME CONSTRAINT fk_products_sizes_size_id TO fk_product_variants_sizes_size_id;
                END IF;
                IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_product_details_subcategories_subcategory_id' AND conrelid = 'products'::regclass) THEN
                    ALTER TABLE products RENAME CONSTRAINT fk_product_details_subcategories_subcategory_id TO fk_products_subcategories_subcategory_id;
                END IF;
                IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_product_holds_products_product_id' AND conrelid = 'product_holds'::regclass) THEN
                    ALTER TABLE product_holds RENAME CONSTRAINT fk_product_holds_products_product_id TO fk_product_holds_product_variants_product_id;
                END IF;
                IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_product_images_product_details_product_detail_id' AND conrelid = 'product_images'::regclass) THEN
                    ALTER TABLE product_images RENAME CONSTRAINT fk_product_images_product_details_product_detail_id TO fk_product_images_products_product_id;
                END IF;
                IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_product_inventory_issues_products_product_id' AND conrelid = 'product_inventory_issues'::regclass) THEN
                    ALTER TABLE product_inventory_issues RENAME CONSTRAINT fk_product_inventory_issues_products_product_id TO fk_product_inventory_issues_product_variants_product_id;
                END IF;
                IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_product_receipt_details_products_product_id' AND conrelid = 'product_receipt_details'::regclass) THEN
                    ALTER TABLE product_receipt_details RENAME CONSTRAINT fk_product_receipt_details_products_product_id TO fk_product_receipt_details_product_variants_product_id;
                END IF;
                IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_purchase_shortages_products_product_id' AND conrelid = 'purchase_shortages'::regclass) THEN
                    ALTER TABLE purchase_shortages RENAME CONSTRAINT fk_purchase_shortages_products_product_id TO fk_purchase_shortages_product_variants_product_id;
                END IF;
                IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_sale_products_products_product_id' AND conrelid = 'sale_products'::regclass) THEN
                    ALTER TABLE sale_products RENAME CONSTRAINT fk_sale_products_products_product_id TO fk_sale_products_product_variants_product_id;
                END IF;
                IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_sale_return_items_products_product_id' AND conrelid = 'sale_return_items'::regclass) THEN
                    ALTER TABLE sale_return_items RENAME CONSTRAINT fk_sale_return_items_products_product_id TO fk_sale_return_items_product_variants_product_id;
                END IF;
            END $$;

            ALTER INDEX IF EXISTS ix_product_details_code RENAME TO ix_products_code;
            ALTER INDEX IF EXISTS ix_product_details_subcategory_id RENAME TO ix_products_subcategory_id;
            ALTER INDEX IF EXISTS ix_product_images_product_detail_id RENAME TO ix_product_images_product_id;
            ALTER INDEX IF EXISTS ix_product_images_product_detail_id_is_primary RENAME TO ix_product_images_product_id_is_primary;
            ALTER INDEX IF EXISTS ix_product_images_product_detail_id_sort_order RENAME TO ix_product_images_product_id_sort_order;
            ALTER INDEX IF EXISTS ix_discount_campaign_products_product_detail_id RENAME TO ix_discount_campaign_products_product_id;
            ALTER INDEX IF EXISTS ix_products_order_id RENAME TO ix_product_variants_order_id;
            ALTER INDEX IF EXISTS ix_products_product_detail_id_size_id_variant RENAME TO ix_product_variants_product_id_size_id_variant;
            ALTER INDEX IF EXISTS ix_products_size_id RENAME TO ix_product_variants_size_id;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE products RENAME TO product_details_legacy;
            ALTER TABLE product_variants RENAME TO products;
            ALTER TABLE product_details_legacy RENAME TO product_details;

            ALTER TABLE product_images RENAME COLUMN product_id TO product_detail_id;
            ALTER TABLE discount_campaign_products RENAME COLUMN product_id TO product_detail_id;
            ALTER TABLE products RENAME COLUMN product_id TO product_detail_id;

            DO $$
            BEGIN
                IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'pk_product_variants' AND conrelid = 'products'::regclass) THEN
                    ALTER TABLE products RENAME CONSTRAINT pk_product_variants TO pk_product_variants_legacy;
                END IF;
                IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'pk_products' AND conrelid = 'product_details'::regclass) THEN
                    ALTER TABLE product_details RENAME CONSTRAINT pk_products TO pk_product_details;
                END IF;
                IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'pk_product_variants_legacy' AND conrelid = 'products'::regclass) THEN
                    ALTER TABLE products RENAME CONSTRAINT pk_product_variants_legacy TO pk_products;
                END IF;
                IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_discount_campaign_products_products_product_id' AND conrelid = 'discount_campaign_products'::regclass) THEN
                    ALTER TABLE discount_campaign_products RENAME CONSTRAINT fk_discount_campaign_products_products_product_id TO fk_discount_campaign_products_product_details_product_detail_id;
                END IF;
                IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_exchange_outbound_items_product_variants_product_id' AND conrelid = 'exchange_outbound_items'::regclass) THEN
                    ALTER TABLE exchange_outbound_items RENAME CONSTRAINT fk_exchange_outbound_items_product_variants_product_id TO fk_exchange_outbound_items_products_product_id;
                END IF;
                IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_exchange_return_items_product_variants_product_id' AND conrelid = 'exchange_return_items'::regclass) THEN
                    ALTER TABLE exchange_return_items RENAME CONSTRAINT fk_exchange_return_items_product_variants_product_id TO fk_exchange_return_items_products_product_id;
                END IF;
                IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_inventory_adjustment_items_product_variants_product_id' AND conrelid = 'inventory_adjustment_items'::regclass) THEN
                    ALTER TABLE inventory_adjustment_items RENAME CONSTRAINT fk_inventory_adjustment_items_product_variants_product_id TO fk_inventory_adjustment_items_products_product_id;
                END IF;
                IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_inventory_movements_product_variants_product_id' AND conrelid = 'inventory_movements'::regclass) THEN
                    ALTER TABLE inventory_movements RENAME CONSTRAINT fk_inventory_movements_product_variants_product_id TO fk_inventory_movements_products_product_id;
                END IF;
                IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_product_variants_orders_order_id' AND conrelid = 'products'::regclass) THEN
                    ALTER TABLE products RENAME CONSTRAINT fk_product_variants_orders_order_id TO fk_products_orders_order_id;
                END IF;
                IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_product_variants_products_product_id' AND conrelid = 'products'::regclass) THEN
                    ALTER TABLE products RENAME CONSTRAINT fk_product_variants_products_product_id TO fk_products_product_details_product_detail_id;
                END IF;
                IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_product_variants_sizes_size_id' AND conrelid = 'products'::regclass) THEN
                    ALTER TABLE products RENAME CONSTRAINT fk_product_variants_sizes_size_id TO fk_products_sizes_size_id;
                END IF;
                IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_products_subcategories_subcategory_id' AND conrelid = 'product_details'::regclass) THEN
                    ALTER TABLE product_details RENAME CONSTRAINT fk_products_subcategories_subcategory_id TO fk_product_details_subcategories_subcategory_id;
                END IF;
                IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_product_holds_product_variants_product_id' AND conrelid = 'product_holds'::regclass) THEN
                    ALTER TABLE product_holds RENAME CONSTRAINT fk_product_holds_product_variants_product_id TO fk_product_holds_products_product_id;
                END IF;
                IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_product_images_products_product_id' AND conrelid = 'product_images'::regclass) THEN
                    ALTER TABLE product_images RENAME CONSTRAINT fk_product_images_products_product_id TO fk_product_images_product_details_product_detail_id;
                END IF;
                IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_product_inventory_issues_product_variants_product_id' AND conrelid = 'product_inventory_issues'::regclass) THEN
                    ALTER TABLE product_inventory_issues RENAME CONSTRAINT fk_product_inventory_issues_product_variants_product_id TO fk_product_inventory_issues_products_product_id;
                END IF;
                IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_product_receipt_details_product_variants_product_id' AND conrelid = 'product_receipt_details'::regclass) THEN
                    ALTER TABLE product_receipt_details RENAME CONSTRAINT fk_product_receipt_details_product_variants_product_id TO fk_product_receipt_details_products_product_id;
                END IF;
                IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_purchase_shortages_product_variants_product_id' AND conrelid = 'purchase_shortages'::regclass) THEN
                    ALTER TABLE purchase_shortages RENAME CONSTRAINT fk_purchase_shortages_product_variants_product_id TO fk_purchase_shortages_products_product_id;
                END IF;
                IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_sale_products_product_variants_product_id' AND conrelid = 'sale_products'::regclass) THEN
                    ALTER TABLE sale_products RENAME CONSTRAINT fk_sale_products_product_variants_product_id TO fk_sale_products_products_product_id;
                END IF;
                IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_sale_return_items_product_variants_product_id' AND conrelid = 'sale_return_items'::regclass) THEN
                    ALTER TABLE sale_return_items RENAME CONSTRAINT fk_sale_return_items_product_variants_product_id TO fk_sale_return_items_products_product_id;
                END IF;
            END $$;

            ALTER INDEX IF EXISTS ix_products_code RENAME TO ix_product_details_code;
            ALTER INDEX IF EXISTS ix_products_subcategory_id RENAME TO ix_product_details_subcategory_id;
            ALTER INDEX IF EXISTS ix_product_images_product_id RENAME TO ix_product_images_product_detail_id;
            ALTER INDEX IF EXISTS ix_product_images_product_id_is_primary RENAME TO ix_product_images_product_detail_id_is_primary;
            ALTER INDEX IF EXISTS ix_product_images_product_id_sort_order RENAME TO ix_product_images_product_detail_id_sort_order;
            ALTER INDEX IF EXISTS ix_discount_campaign_products_product_id RENAME TO ix_discount_campaign_products_product_detail_id;
            ALTER INDEX IF EXISTS ix_product_variants_order_id RENAME TO ix_products_order_id;
            ALTER INDEX IF EXISTS ix_product_variants_product_id_size_id_variant RENAME TO ix_products_product_detail_id_size_id_variant;
            ALTER INDEX IF EXISTS ix_product_variants_size_id RENAME TO ix_products_size_id;
            """);
    }
}
