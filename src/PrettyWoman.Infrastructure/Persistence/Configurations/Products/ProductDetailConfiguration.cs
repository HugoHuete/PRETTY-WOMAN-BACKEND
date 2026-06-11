using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Products;

public class ProductDetailConfiguration : IEntityTypeConfiguration<ProductDetail>
{
    public void Configure (EntityTypeBuilder<ProductDetail> builder)
    {
        builder.Property(x => x.SupplierProductCode).HasMaxLength(50);
        builder.Property(x => x.Code).HasMaxLength(10);
        builder.Property(x => x.Name).HasMaxLength(100);

        builder.HasOne(x => x.Subcategory).WithMany(x => x.ProductDetails).HasForeignKey(x => x.SubcategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}