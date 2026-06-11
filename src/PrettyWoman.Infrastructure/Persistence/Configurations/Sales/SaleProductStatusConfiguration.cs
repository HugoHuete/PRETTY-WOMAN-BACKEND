using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Sales;

public class SaleProductStatusConfiguration : IEntityTypeConfiguration<SaleProductStatus>
{
    public void Configure(EntityTypeBuilder<SaleProductStatus> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).HasMaxLength(30);

        builder.HasIndex(x => x.Name).IsUnique();

        builder.HasData(
            new SaleStatus
            {
                Id = (int) SaleProductStatusOption.Pending,
                Name = nameof(SaleProductStatusOption.Pending)
            },
            new SaleStatus
            {
                Id = (int) SaleProductStatusOption.Completed,
                Name = nameof(SaleProductStatusOption.Completed)
            },
            new SaleStatus
            {
                Id = (int) SaleProductStatusOption.Refunded,
                Name = nameof(SaleProductStatusOption.Refunded)
            },
            new SaleStatus
            {
                Id = (int) SaleProductStatusOption.Changed,
                Name = nameof(SaleProductStatusOption.Changed)
            },
            new SaleStatus
            {
                Id = (int) SaleProductStatusOption.Cancelled,
                Name = nameof(SaleProductStatusOption.Cancelled)
            }
        );

    }
}