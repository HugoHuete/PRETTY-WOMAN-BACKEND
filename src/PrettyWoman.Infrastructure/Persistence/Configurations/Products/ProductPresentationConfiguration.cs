using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Products;

public class ProductPresentationConfiguration : IEntityTypeConfiguration<ProductPresentation>
{
    public void Configure(EntityTypeBuilder<ProductPresentation> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(50);
        builder.Property(x => x.NormalizedName).HasMaxLength(50);

        builder.HasAlternateKey(x => new { x.Id, x.ProductId });

        builder.HasOne(x => x.Product)
            .WithMany(x => x.ProductPresentations)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ProductId, x.NormalizedName })
            .IsUnique()
            .HasFilter("normalized_name is not null");

        builder.HasIndex(x => x.ProductId)
            .IsUnique()
            .HasFilter("normalized_name is null");

        builder.ToTable(t => t.HasCheckConstraint(
            "ck_product_presentations_sort_order_non_negative",
            "sort_order >= 0"));
    }
}
