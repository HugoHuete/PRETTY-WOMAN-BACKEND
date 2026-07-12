using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Sales;

public class ExchangeOutboundItemConfiguration : IEntityTypeConfiguration<ExchangeOutboundItem>
{
    public void Configure(EntityTypeBuilder<ExchangeOutboundItem> builder)
    {
        builder.Property(x => x.UnitPrice).HasPrecision(14, 2);
        builder.Property(x => x.UnitCost).HasPrecision(18, 6);
        builder.Property(x => x.LineTotal).HasPrecision(14, 2);
        builder.Property(x => x.TotalCost).HasPrecision(18, 6);
        builder.Property(x => x.Comments).HasMaxLength(500);
        builder.HasOne(x => x.SaleExchange).WithMany(x => x.OutboundItems).HasForeignKey(x => x.SaleExchangeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.SaleExchangeId);
        builder.HasIndex(x => x.ProductId);
        builder.ToTable(t => t.HasCheckConstraint("ck_exchange_outbound_items_quantity_positive", "quantity > 0"));
    }
}
