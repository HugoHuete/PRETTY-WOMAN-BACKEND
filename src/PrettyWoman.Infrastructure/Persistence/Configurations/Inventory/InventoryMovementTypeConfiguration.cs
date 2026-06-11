using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Inventory;

public class InventoryMovementTypeConfiguration : IEntityTypeConfiguration<InventoryMovementType>
{
    public void Configure(EntityTypeBuilder<InventoryMovementType> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(60);

        builder.HasIndex(x => x.Name).IsUnique();

        builder.HasData(
            new InventoryMovementType { Id = (int)InventoryMovementTypeOption.PurchaseReceived, Name = nameof(InventoryMovementTypeOption.PurchaseReceived) },
            new InventoryMovementType { Id = (int)InventoryMovementTypeOption.Sale, Name = nameof(InventoryMovementTypeOption.Sale) },
            new InventoryMovementType { Id = (int)InventoryMovementTypeOption.SaleCancelled, Name = nameof(InventoryMovementTypeOption.SaleCancelled) },
            new InventoryMovementType { Id = (int)InventoryMovementTypeOption.CustomerReturn, Name = nameof(InventoryMovementTypeOption.CustomerReturn) },
            new InventoryMovementType { Id = (int)InventoryMovementTypeOption.ExchangeReturn, Name = nameof(InventoryMovementTypeOption.ExchangeReturn) },
            new InventoryMovementType { Id = (int)InventoryMovementTypeOption.Damaged, Name = nameof(InventoryMovementTypeOption.Damaged) },
            new InventoryMovementType { Id = (int)InventoryMovementTypeOption.Repaired, Name = nameof(InventoryMovementTypeOption.Repaired) },
            new InventoryMovementType { Id = (int)InventoryMovementTypeOption.Lost, Name = nameof(InventoryMovementTypeOption.Lost) },
            new InventoryMovementType { Id = (int)InventoryMovementTypeOption.Found, Name = nameof(InventoryMovementTypeOption.Found) },
            new InventoryMovementType { Id = (int)InventoryMovementTypeOption.Discarded, Name = nameof(InventoryMovementTypeOption.Discarded) },
            new InventoryMovementType { Id = (int)InventoryMovementTypeOption.Donation, Name = nameof(InventoryMovementTypeOption.Donation) },
            new InventoryMovementType { Id = (int)InventoryMovementTypeOption.AdjustmentIncrease, Name = nameof(InventoryMovementTypeOption.AdjustmentIncrease) },
            new InventoryMovementType { Id = (int)InventoryMovementTypeOption.AdjustmentDecrease, Name = nameof(InventoryMovementTypeOption.AdjustmentDecrease) },
            new InventoryMovementType { Id = (int)InventoryMovementTypeOption.ReservationCreated, Name = nameof(InventoryMovementTypeOption.ReservationCreated) },
            new InventoryMovementType { Id = (int)InventoryMovementTypeOption.ReservationReleased, Name = nameof(InventoryMovementTypeOption.ReservationReleased) },
            new InventoryMovementType { Id = (int)InventoryMovementTypeOption.ReservationConvertedToSale, Name = nameof(InventoryMovementTypeOption.ReservationConvertedToSale) }
        );
    }
}
