using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Orders;

public class OrderTrackingNumberConfiguration : IEntityTypeConfiguration<OrderTrackingNumber>
{
    public void Configure (EntityTypeBuilder<OrderTrackingNumber> builder)
    {

        builder.Property(x => x.TrackingNumber)
            .IsRequired()
            .HasMaxLength(60);

        builder.HasIndex(x => x.TrackingNumber)
            .IsUnique();

        builder.Property(x => x.Weight).HasPrecision(12, 2);
        builder.Property(x => x.ShippingCost).HasPrecision(12, 2);


        builder.HasOne(x => x.ShippingCompany).WithMany().HasForeignKey(x => x.ShippingCompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Order).WithMany(x => x.OrderTrackingNumbers).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductReceipt).WithMany(x => x.OrderTrackingNumbers).HasForeignKey(x => x.ProductReceiptId).OnDelete(DeleteBehavior.Restrict);


        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_order_tracking_number_weight_non_negative",
                "weight >= 0");

            t.HasCheckConstraint(
                "ck_order_tracking_number_shipping_cost_non_negative",
                "shipping_cost >= 0");
        });
    }
}