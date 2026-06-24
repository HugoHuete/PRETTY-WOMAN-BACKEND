using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Inventory;

public class InventoryStockBucketConfiguration : IEntityTypeConfiguration<InventoryStockBucket>
{
    public void Configure(EntityTypeBuilder<InventoryStockBucket> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(x => x.Name).IsUnique();

        builder.HasData(
            new InventoryStockBucket { Id = (int)InventoryStockBucketOption.External, Name = nameof(InventoryStockBucketOption.External) },
            new InventoryStockBucket { Id = (int)InventoryStockBucketOption.Available, Name = nameof(InventoryStockBucketOption.Available) },
            new InventoryStockBucket { Id = (int)InventoryStockBucketOption.Reserved, Name = nameof(InventoryStockBucketOption.Reserved) },
            new InventoryStockBucket { Id = (int)InventoryStockBucketOption.Unavailable, Name = nameof(InventoryStockBucketOption.Unavailable) },
            new InventoryStockBucket { Id = (int)InventoryStockBucketOption.OutOfInventory, Name = nameof(InventoryStockBucketOption.OutOfInventory) }
        );
    }
}
