using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Orders;

public class ProductReceiptConfiguration : IEntityTypeConfiguration<ProductReceipt>
{
    public void Configure(EntityTypeBuilder<ProductReceipt> builder)
    {
        builder.Property(x => x.WarehouseShippingCostUsd).HasPrecision(12, 2);
        builder.Property(x => x.WarehouseShippingCostNio).HasPrecision(12, 2);

        builder.HasOne(x => x.Order).WithMany(x => x.ProductReceipts).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => new { x.OrderId, x.ReceivedDate });
        builder.HasIndex(x => x.ReceivedDate);
        builder.HasIndex(x => x.CreatedAt);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_product_receipt_warehouse_shipping_cost_usd_non_negative", "warehouse_shipping_cost_usd >= 0");
            t.HasCheckConstraint("ck_product_receipt_warehouse_shipping_cost_nio_non_negative", "warehouse_shipping_cost_nio >= 0");
        });
    }
}
