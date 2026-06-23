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
        builder.HasOne(x => x.MovementDirection).WithMany().HasForeignKey(x => x.MovementDirectionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.InventoryMovementType).WithMany().HasForeignKey(x => x.InventoryMovementTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SaleProduct).WithMany().HasForeignKey(x => x.SaleProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductHold).WithMany().HasForeignKey(x => x.ProductHoldId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductInventoryIssue).WithMany(x => x.InventoryMovements).HasForeignKey(x => x.ProductInventoryIssueId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.MovementDirectionId);
        builder.HasIndex(x => x.InventoryMovementTypeId);
        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.SaleProductId);
        builder.HasIndex(x => x.ProductHoldId);
        builder.HasIndex(x => x.ProductInventoryIssueId);
        builder.HasIndex(x => new { x.ProductId, x.CreatedAt });
        builder.HasIndex(x => new { x.InventoryMovementTypeId, x.CreatedAt });

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_inventory_movements_quantity_positive",
                "quantity > 0");
        });
    }
}

