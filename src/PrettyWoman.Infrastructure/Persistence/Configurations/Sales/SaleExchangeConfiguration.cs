using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Sales;

public class SaleExchangeConfiguration : IEntityTypeConfiguration<SaleExchange>
{
    public void Configure(EntityTypeBuilder<SaleExchange> builder)
    {
        builder.Property(x => x.RecognizedReturnTotal).HasPrecision(14, 2);
        builder.Property(x => x.OutboundItemsTotal).HasPrecision(14, 2);
        builder.Property(x => x.BalanceToCollect).HasPrecision(14, 2);
        builder.Property(x => x.Comments).HasMaxLength(500);
        builder.HasOne(x => x.OriginalSale).WithMany(x => x.Exchanges).HasForeignKey(x => x.OriginalSaleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.OriginalSaleId);
        builder.HasIndex(x => new { x.OriginalSaleId, x.StatusId });
        builder.ToTable(t => t.HasCheckConstraint("ck_sale_exchanges_totals_non_negative", "recognized_return_total >= 0 AND outbound_items_total >= 0"));
    }
}
