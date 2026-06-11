using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Products;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure (EntityTypeBuilder<Product> builder)
    {
        builder.Property(p => p.UnitCost).HasPrecision(12, 2);
        builder.Property(p => p.UnitCostWithShipping).HasPrecision(12, 2);
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
                "ck_products_received_quantity_non_negative",
                "received_quantity >= 0");

            t.HasCheckConstraint(
                "ck_products_available_quantity_non_negative",
                "available_quantity >= 0");

            t.HasCheckConstraint(
                "ck_products_reserved_quantity_non_negative",
                "reserved_quantity >= 0");

            t.HasCheckConstraint(
                "ck_products_cost_non_negative",
                "unit_cost >= 0");
                
            t.HasCheckConstraint(
                "ck_products_unit_cost_with_shipping_non_negative",
                "unit_cost_with_shipping >= 0");

            t.HasCheckConstraint(
                "ck_products_sale_price_non_negative",
                "sale_price >= 0");
        });
    }
}