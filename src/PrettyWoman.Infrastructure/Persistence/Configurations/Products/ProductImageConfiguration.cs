using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Products;

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.Property(x => x.ImageUrl).IsRequired().HasMaxLength(1000);

        builder.HasOne(x => x.ProductDetail).WithMany(x => x.ProductImages).HasForeignKey(x => x.ProductDetailId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.ProductDetailId);

        builder.HasIndex(x => new { x.ProductDetailId, x.SortOrder });

        builder.HasIndex(x => new { x.ProductDetailId, x.IsPrimary })
            .IsUnique()
            .HasFilter("is_primary = true");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_product_images_sort_order_non_negative",
                "sort_order >= 0");
        });
    }
}