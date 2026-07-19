using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Inventory;

public class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.Property(x => x.Comments).HasMaxLength(500);

        builder.HasOne(x => x.Product).WithMany(x => x.InventoryMovements).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.InventoryMovementType).WithMany().HasForeignKey(x => x.InventoryMovementTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.FromStockBucket).WithMany().HasForeignKey(x => x.FromStockBucketId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ToStockBucket).WithMany().HasForeignKey(x => x.ToStockBucketId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SaleProduct).WithMany().HasForeignKey(x => x.SaleProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductHold).WithMany().HasForeignKey(x => x.ProductHoldId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductInventoryIssue).WithMany(x => x.InventoryMovements).HasForeignKey(x => x.ProductInventoryIssueId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ExchangeReturnItem).WithMany().HasForeignKey(x => x.ExchangeReturnItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ExchangeOutboundItem).WithMany().HasForeignKey(x => x.ExchangeOutboundItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SaleReturnItem).WithMany().HasForeignKey(x => x.SaleReturnItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.InventoryAdjustmentItem).WithOne(x => x.InventoryMovement).HasForeignKey<InventoryMovement>(x => x.InventoryAdjustmentItemId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.MovementDate);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.InventoryMovementTypeId);
        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.SaleProductId);
        builder.HasIndex(x => x.ProductHoldId);
        builder.HasIndex(x => x.ProductInventoryIssueId);
        builder.HasIndex(x => x.ExchangeReturnItemId);
        builder.HasIndex(x => x.ExchangeOutboundItemId);
        builder.HasIndex(x => x.SaleReturnItemId);
        builder.HasIndex(x => x.InventoryAdjustmentItemId).IsUnique().HasFilter("inventory_adjustment_item_id IS NOT NULL");
        builder.HasIndex(x => new { x.ProductId, x.MovementDate });
        builder.HasIndex(x => new { x.InventoryMovementTypeId, x.MovementDate });

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_inventory_movements_quantity_positive",
                "quantity > 0");
        });
    }
}
