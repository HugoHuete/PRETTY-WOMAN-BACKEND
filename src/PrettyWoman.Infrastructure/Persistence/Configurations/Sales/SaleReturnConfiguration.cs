using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Sales;

public class SaleReturnConfiguration : IEntityTypeConfiguration<SaleReturn>
{
    public void Configure(EntityTypeBuilder<SaleReturn> builder)
    {
        builder.Property(x => x.ProductRefundTotal).HasPrecision(14, 2);
        builder.Property(x => x.ReturnShippingChargedToClient).HasPrecision(14, 2);
        builder.Property(x => x.ReturnShippingPaidToAgency).HasPrecision(14, 2);
        builder.Property(x => x.OriginalShippingRefund).HasPrecision(14, 2);
        builder.Property(x => x.RefundTotal).HasPrecision(14, 2);
        builder.Property(x => x.ReturnCode).HasMaxLength(100);
        builder.Property(x => x.Comments).HasMaxLength(500);
        builder.HasOne(x => x.OriginalSale).WithMany(x => x.Returns).HasForeignKey(x => x.OriginalSaleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.DeliveryAgency).WithMany().HasForeignKey(x => x.DeliveryAgencyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.OriginalSaleId);
        builder.HasIndex(x => new { x.OriginalSaleId, x.StatusId });
        builder.ToTable(t => t.HasCheckConstraint("ck_sale_returns_totals_non_negative", "product_refund_total >= 0 AND return_shipping_charged_to_client >= 0 AND return_shipping_paid_to_agency >= 0 AND original_shipping_refund >= 0 AND refund_total >= 0"));
    }
}
