using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Orders;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure (EntityTypeBuilder<Order> builder)
    {
        builder.Property(x => x.Comments).HasMaxLength(300);
        builder.Property(x => x.Amount).HasPrecision(12,2);
        builder.Property(x => x.AmountUsd).HasPrecision(12,2);
        builder.Property(x => x.ReceivedAmount).HasPrecision(12,2);
        builder.Property(x => x.TotalShippingCost).HasPrecision(12,2);
        builder.Property(x => x.ExchangeRate).HasPrecision(10,4);


        builder.HasOne(x => x.OrderStatus).WithMany().HasForeignKey(x => x.OrderStatusId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Supplier).WithMany(x => x.Orders).HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_order_amount_non_negative",
                "amount >= 0");
            t.HasCheckConstraint(
                "ck_order_amount_usd_non_negative",
                "amount_usd >= 0");
            t.HasCheckConstraint(
                "ck_order_received_amount_non_negative",
                "received_amount >= 0");
            t.HasCheckConstraint(
                "ck_order_total_shipping_cost_non_negative",
                "total_shipping_cost >= 0");
            t.HasCheckConstraint(
                "ck_order_exchange_rate_non_negative",
                "exchange_rate >= 0");
        });

        
    }
}