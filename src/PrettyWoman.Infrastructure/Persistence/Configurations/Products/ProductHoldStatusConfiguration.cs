using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Products;

public class ProductHoldStatusConfiguration : IEntityTypeConfiguration<ProductHoldStatus>
{
    public void Configure(EntityTypeBuilder<ProductHoldStatus> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(x => x.Name)
            .IsUnique();

        builder.HasData(
            new ProductHoldStatus
            {
                Id = (int)ProductHoldStatusOption.Active,
                Name = nameof(ProductHoldStatusOption.Active)
            },
            new ProductHoldStatus
            {
                Id = (int)ProductHoldStatusOption.ConvertedToSale,
                Name = nameof(ProductHoldStatusOption.ConvertedToSale)
            },
            new ProductHoldStatus
            {
                Id = (int)ProductHoldStatusOption.NotSelected,
                Name = nameof(ProductHoldStatusOption.NotSelected)
            },
            new ProductHoldStatus
            {
                Id = (int)ProductHoldStatusOption.Found,
                Name = nameof(ProductHoldStatusOption.Found)
            },
            new ProductHoldStatus
            {
                Id = (int)ProductHoldStatusOption.Repaired,
                Name = nameof(ProductHoldStatusOption.Repaired)
            },
            new ProductHoldStatus
            {
                Id = (int)ProductHoldStatusOption.Discarded,
                Name = nameof(ProductHoldStatusOption.Discarded)
            }
        );
    }
}