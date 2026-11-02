using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Deliveries;

public class PostSaleDeliveryConfiguration : IEntityTypeConfiguration<PostSaleDelivery>
{
    public void Configure(EntityTypeBuilder<PostSaleDelivery> builder)
    {
        builder.Property(item => item.Code).HasMaxLength(100);
        builder.Property(item => item.Address).HasMaxLength(500);
        builder.Property(item => item.Comments).HasMaxLength(500);
        builder.Property(item => item.ShippingChargedToClient).HasPrecision(14, 2);
        builder.Property(item => item.ShippingPaidToAgency).HasPrecision(14, 2);
        builder.Property(item => item.AmountToCollect).HasPrecision(14, 2);
        builder.Property(item => item.AmountCollectedNio).HasPrecision(14, 2);
        builder.Property(item => item.AmountCollectedUsd).HasPrecision(14, 2);
        builder.Property(item => item.ChangeGivenNio).HasPrecision(14, 2);
        builder.Property(item => item.CollectionExchangeRate).HasPrecision(10, 4);

        builder.HasOne(item => item.SaleReturn).WithOne(item => item.PostSaleDelivery)
            .HasForeignKey<PostSaleDelivery>(item => item.SaleReturnId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.SaleExchange).WithOne(item => item.PostSaleDelivery)
            .HasForeignKey<PostSaleDelivery>(item => item.SaleExchangeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.DeliveryAgency).WithMany().HasForeignKey(item => item.DeliveryAgencyId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.DeliveryAgencyReconciliation).WithMany(item => item.PostSaleDeliveries)
            .HasForeignKey(item => item.DeliveryAgencyReconciliationId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(item => item.SaleReturnId).IsUnique().HasFilter("sale_return_id IS NOT NULL");
        builder.HasIndex(item => item.SaleExchangeId).IsUnique().HasFilter("sale_exchange_id IS NOT NULL");
        builder.HasIndex(item => new { item.DeliveryAgencyId, item.StatusId, item.DeliveryAgencyReconciliationId });
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("ck_post_sale_deliveries_parent", "(sale_return_id IS NOT NULL AND sale_exchange_id IS NULL) OR (sale_return_id IS NULL AND sale_exchange_id IS NOT NULL)");
            table.HasCheckConstraint("ck_post_sale_deliveries_shipping_non_negative", "shipping_charged_to_client >= 0 AND shipping_paid_to_agency >= 0");
        });
    }
}
