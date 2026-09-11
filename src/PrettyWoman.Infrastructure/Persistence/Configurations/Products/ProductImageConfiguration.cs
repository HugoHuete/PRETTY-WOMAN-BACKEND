using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Products;

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.HasOne(x => x.Product).WithMany(x => x.ProductImages).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.ProductPresentation)
            .WithMany(x => x.ProductImages)
            .HasForeignKey(x => new { x.ProductPresentationId, x.ProductId })
            .HasPrincipalKey(x => new { x.Id, x.ProductId })
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.MediaAsset).WithMany().HasForeignKey(x => x.MediaAssetId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.MediaAssetId).IsUnique().HasFilter("media_asset_id is not null");

        builder.HasIndex(x => new { x.ProductId, x.SortOrder });

        builder.HasIndex(x => x.ProductId)
            .IsUnique()
            .HasDatabaseName("ix_product_images_product_id_general_primary")
            .HasFilter("is_primary = true AND product_presentation_id IS NULL");

        builder.HasIndex(x => new { x.ProductId, x.ProductPresentationId })
            .IsUnique()
            .HasDatabaseName("ix_product_images_product_id_presentation_primary")
            .HasFilter("is_primary = true AND product_presentation_id IS NOT NULL");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_product_images_sort_order_non_negative",
                "sort_order >= 0");
        });
    }
}
