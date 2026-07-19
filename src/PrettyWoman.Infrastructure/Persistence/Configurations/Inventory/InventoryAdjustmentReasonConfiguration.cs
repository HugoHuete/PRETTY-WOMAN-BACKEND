using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Inventory;

public class InventoryAdjustmentReasonConfiguration : IEntityTypeConfiguration<InventoryAdjustmentReason>
{
    public void Configure(EntityTypeBuilder<InventoryAdjustmentReason> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(60);

        builder.HasIndex(x => x.Name).IsUnique();

        builder.HasData(
            new InventoryAdjustmentReason { Id = (int)InventoryAdjustmentReasonOption.ManualCorrection, Name = nameof(InventoryAdjustmentReasonOption.ManualCorrection) },
            new InventoryAdjustmentReason { Id = (int)InventoryAdjustmentReasonOption.ProductCodeMixUp, Name = nameof(InventoryAdjustmentReasonOption.ProductCodeMixUp) },
            new InventoryAdjustmentReason { Id = (int)InventoryAdjustmentReasonOption.PurchaseSurplus, Name = nameof(InventoryAdjustmentReasonOption.PurchaseSurplus) },
            new InventoryAdjustmentReason { Id = (int)InventoryAdjustmentReasonOption.PurchaseShortage, Name = nameof(InventoryAdjustmentReasonOption.PurchaseShortage) },
            new InventoryAdjustmentReason { Id = (int)InventoryAdjustmentReasonOption.LostItem, Name = nameof(InventoryAdjustmentReasonOption.LostItem) },
            new InventoryAdjustmentReason { Id = (int)InventoryAdjustmentReasonOption.FoundItem, Name = nameof(InventoryAdjustmentReasonOption.FoundItem) },
            new InventoryAdjustmentReason { Id = (int)InventoryAdjustmentReasonOption.Donation, Name = nameof(InventoryAdjustmentReasonOption.Donation) },
            new InventoryAdjustmentReason { Id = (int)InventoryAdjustmentReasonOption.Other, Name = nameof(InventoryAdjustmentReasonOption.Other) }
        );
    }
}
