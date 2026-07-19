using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Inventory;

public class InventoryAdjustmentItemConfiguration : IEntityTypeConfiguration<InventoryAdjustmentItem>
{
    public void Configure(EntityTypeBuilder<InventoryAdjustmentItem> builder)
    {
        builder.Property(x => x.Comments).HasMaxLength(500);

        builder.HasOne(x => x.InventoryAdjustment)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.InventoryAdjustmentId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Product)
            .WithMany(x => x.InventoryAdjustmentItems)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.FromStockBucket)
            .WithMany()
            .HasForeignKey(x => x.FromStockBucketId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ToStockBucket)
            .WithMany()
            .HasForeignKey(x => x.ToStockBucketId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.InventoryAdjustmentId);
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.FromStockBucketId);
        builder.HasIndex(x => x.ToStockBucketId);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_inventory_adjustment_items_quantity_positive",
                "quantity > 0");
            t.HasCheckConstraint(
                "ck_inventory_adjustment_items_different_buckets",
                "from_stock_bucket_id <> to_stock_bucket_id");
        });
    }
}
