using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Discounts;

public class DiscountTypeConfiguration : IEntityTypeConfiguration<DiscountType>
{
    public void Configure (EntityTypeBuilder<DiscountType> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(x => x.Name)
            .IsUnique();

        builder.HasData(
            new DiscountType
            {
                Id = (int)DiscountTypeOption.FixedAmount,
                Name = nameof(DiscountTypeOption.FixedAmount)
            },
            new DiscountType
            {
                Id = (int)DiscountTypeOption.Percentage,
                Name = nameof(DiscountTypeOption.Percentage)
            },
            new DiscountType
            {
                Id = (int)DiscountTypeOption.FixedPrice,
                Name = nameof(DiscountTypeOption.FixedPrice)
            }
        );
    }
}