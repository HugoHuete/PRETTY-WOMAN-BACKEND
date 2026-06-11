using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Sales;

public class SaleChannelConfiguration : IEntityTypeConfiguration<SaleChannel>
{
    public void Configure(EntityTypeBuilder<SaleChannel> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).HasMaxLength(30);

        builder.HasIndex(x => x.Name).IsUnique();

        builder.HasData(
            new SaleChannel
            {
                Id = (int) SaleChannelOption.InStoreSale,
                Name = nameof(SaleChannelOption.InStoreSale)
            },
            new SaleChannel
            {
                Id = (int) SaleChannelOption.Whatsapp,
                Name = nameof(SaleChannelOption.Whatsapp)
            },
            new SaleChannel
            {
                Id = (int) SaleChannelOption.Instagram,
                Name = nameof(SaleChannelOption.Instagram)
            },
            new SaleChannel
            {
                Id = (int) SaleChannelOption.Messenger,
                Name = nameof(SaleChannelOption.Messenger)
            },
            new SaleChannel
            {
                Id = (int) SaleChannelOption.Other,
                Name = nameof(SaleChannelOption.Other)
            }
        );

    }
}