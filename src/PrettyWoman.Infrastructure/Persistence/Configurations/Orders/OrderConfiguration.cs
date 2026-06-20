using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Orders;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.Property(x => x.Comments).HasMaxLength(300);
        builder.Property(x => x.AmountUsd).HasPrecision(14, 2);
        builder.Property(x => x.ExchangeRate).HasPrecision(10, 4);
        builder.Property(x => x.MerchandiseTotalNio).HasPrecision(14, 2);
        builder.Property(x => x.ReceivedAmountNio).HasPrecision(14, 2);
        builder.Property(x => x.ShippingCostNio).HasPrecision(14, 2);
        builder.Property(x => x.TotalCostNio).HasPrecision(14, 2);


        builder.HasOne(x => x.OrderStatus).WithMany().HasForeignKey(x => x.OrderStatusId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Supplier).WithMany(x => x.Orders).HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_orders_amount_usd_non_negative",
                "amount_usd >= 0");

            t.HasCheckConstraint(
                "ck_orders_received_amount_nio_non_negative",
                "received_amount_nio >= 0");

            t.HasCheckConstraint(
                "ck_orders_exchange_rate_positive",
                "exchange_rate > 0");

            t.HasCheckConstraint(
                "ck_orders_merchandise_total_nio_non_negative",
                "merchandise_total_nio >= 0");

            t.HasCheckConstraint(
                "ck_orders_shipping_cost_nio_non_negative",
                "shipping_cost_nio >= 0");

            t.HasCheckConstraint(
                "ck_orders_total_cost_nio_non_negative",
                "total_cost_nio >= 0");
        });


    }
}