using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Discounts;

public class DiscountSourceConfiguration : IEntityTypeConfiguration<DiscountSource>
{
    public void Configure (EntityTypeBuilder<DiscountSource> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(x => x.Name)
            .IsUnique();

        builder.HasData(
            new DiscountSource
            {
                Id = (int)DiscountSourceOption.None,
                Name = nameof(DiscountSourceOption.None)
            },
            new DiscountSource
            {
                Id = (int)DiscountSourceOption.Campaign,
                Name = nameof(DiscountSourceOption.Campaign)
            },
            new DiscountSource
            {
                Id = (int)DiscountSourceOption.Manual,
                Name = nameof(DiscountSourceOption.Manual)
            },
            new DiscountSource
            {
                Id = (int)DiscountSourceOption.Employee,
                Name = nameof(DiscountSourceOption.Employee)
            }
        );
    }
}