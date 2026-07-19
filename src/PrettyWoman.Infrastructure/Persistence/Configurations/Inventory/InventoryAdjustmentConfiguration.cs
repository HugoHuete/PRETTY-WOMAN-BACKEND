using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Inventory;

public class InventoryAdjustmentConfiguration : IEntityTypeConfiguration<InventoryAdjustment>
{
    public void Configure(EntityTypeBuilder<InventoryAdjustment> builder)
    {
        builder.Property(x => x.Reference).HasMaxLength(100);
        builder.Property(x => x.Comments).HasMaxLength(500);

        builder.HasOne(x => x.InventoryAdjustmentReason)
            .WithMany()
            .HasForeignKey(x => x.InventoryAdjustmentReasonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.InventoryAdjustmentReasonId);
        builder.HasIndex(x => x.AdjustmentDate);
        builder.HasIndex(x => x.CreatedAt);
    }
}
