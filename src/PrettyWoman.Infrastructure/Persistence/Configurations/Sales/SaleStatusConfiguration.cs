using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Sales;

public class SaleStatusConfiguration : IEntityTypeConfiguration<SaleStatus>
{
    public void Configure(EntityTypeBuilder<SaleStatus> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).HasMaxLength(30);

        builder.HasIndex(x => x.Name).IsUnique();

        builder.HasData(
            new SaleStatus
            {
                Id = (int) SaleStatusOption.Pending,
                Name = nameof(SaleStatusOption.Pending)
            },
            new SaleStatus
            {
                Id = (int) SaleStatusOption.Reserved,
                Name = nameof(SaleStatusOption.Reserved)
            },
            new SaleStatus
            {
                Id = (int) SaleStatusOption.ReadyForDelivery,
                Name = nameof(SaleStatusOption.ReadyForDelivery)
            },
            new SaleStatus
            {
                Id = (int) SaleStatusOption.SentForDelivery,
                Name = nameof(SaleStatusOption.SentForDelivery)
            },
            new SaleStatus
            {
                Id = (int) SaleStatusOption.Completed,
                Name = nameof(SaleStatusOption.Completed)
            },
            new SaleStatus
            {
                Id = (int) SaleStatusOption.Cancelled,
                Name = nameof(SaleStatusOption.Cancelled)
            }
        );

    }
}