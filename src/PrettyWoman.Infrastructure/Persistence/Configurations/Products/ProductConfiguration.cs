using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Products;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure (EntityTypeBuilder<Product> builder)
    {
        builder.Property(x => x.SupplierProductCode).HasMaxLength(50);
        builder.Property(x => x.Name).HasMaxLength(200);

        builder.HasIndex(x => x.Code).IsUnique();

        builder.HasOne(x => x.Subcategory).WithMany(x => x.Products).HasForeignKey(x => x.SubcategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}