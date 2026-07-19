using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Products;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.Property(p => p.UnitCostUsd).HasPrecision(14, 2);
        builder.Property(p => p.MerchandiseTotalCostNio).HasPrecision(14, 2);
        builder.Property(p => p.AllocatedShippingCostNio).HasPrecision(14, 2);
        builder.Property(p => p.TotalCostNio).HasPrecision(14, 2);
        builder.Property(p => p.UnitCostNio).HasPrecision(18, 6);
        builder.Property(p => p.SalePrice).HasPrecision(12, 2);
        builder.Property(x => x.Color).HasMaxLength(50);

        builder.HasOne(x => x.Size).WithMany().HasForeignKey(x => x.SizeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Order).WithMany(x => x.Products).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductDetail).WithMany(x => x.Products).HasForeignKey(x => x.ProductDetailId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new
        {
            x.ProductDetailId,
            x.SizeId,
            x.Color
        });

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_products_quantity_non_negative",
                "quantity >= 0");

            t.HasCheckConstraint(
                "ck_products_sale_price_positive",
                "sale_price >= 0");

            t.HasCheckConstraint(
                "ck_products_received_quantity_non_negative",
                "received_quantity >= 0");

            t.HasCheckConstraint(
                "ck_products_available_quantity_non_negative",
                "available_quantity >= 0");

            t.HasCheckConstraint(
                "ck_products_reserved_quantity_non_negative",
                "reserved_quantity >= 0");

            t.HasCheckConstraint(
                "ck_products_unavailable_quantity_non_negative",
                "unavailable_quantity >= 0");

            t.HasCheckConstraint(
                "ck_products_received_quantity_not_greater_than_quantity",
                "received_quantity <= quantity");

            t.HasCheckConstraint(
                "ck_products_stock_not_greater_than_received",
                "available_quantity + reserved_quantity + unavailable_quantity <= received_quantity");

            t.HasCheckConstraint(
                "ck_products_unit_cost_usd_non_negative",
                "unit_cost_usd >= 0");

            t.HasCheckConstraint(
                "ck_products_merchandise_total_cost_nio_non_negative",
                "merchandise_total_cost_nio >= 0");

            t.HasCheckConstraint(
                "ck_products_allocated_shipping_cost_nio_non_negative",
                "allocated_shipping_cost_nio >= 0");

            t.HasCheckConstraint(
                "ck_products_total_cost_nio_non_negative",
                "total_cost_nio >= 0");

            t.HasCheckConstraint(
                "ck_products_unit_cost_nio_non_negative",
                "unit_cost_nio >= 0");
        });
    }
}

