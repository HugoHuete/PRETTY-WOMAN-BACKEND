using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Sales;

public class ExchangeReturnItemConfiguration : IEntityTypeConfiguration<ExchangeReturnItem>
{
    public void Configure(EntityTypeBuilder<ExchangeReturnItem> builder)
    {
        builder.Property(x => x.RecognizedUnitAmount).HasPrecision(14, 2);
        builder.Property(x => x.OriginalUnitCost).HasPrecision(18, 6);
        builder.Property(x => x.Comments).HasMaxLength(500);
        builder.HasOne(x => x.SaleExchange).WithMany(x => x.ReturnItems).HasForeignKey(x => x.SaleExchangeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.OriginalSaleProduct).WithMany().HasForeignKey(x => x.OriginalSaleProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.SaleExchangeId);
        builder.HasIndex(x => x.OriginalSaleProductId);
        builder.ToTable(t => t.HasCheckConstraint("ck_exchange_return_items_quantity_positive", "quantity > 0"));
    }
}
