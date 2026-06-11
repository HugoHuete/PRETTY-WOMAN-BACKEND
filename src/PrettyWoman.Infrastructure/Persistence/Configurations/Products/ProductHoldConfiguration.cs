using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Products;

public class ProductHoldConfiguration : IEntityTypeConfiguration<ProductHold>
{
    public void Configure (EntityTypeBuilder<ProductHold> builder)
    {
        builder.Property(x => x.HoldReason).HasMaxLength(200);
        builder.Property(x => x.Comments).HasMaxLength(300);

        builder.HasOne(x => x.Product).WithMany(x => x.ProductHolds).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductHoldStatus).WithMany().HasForeignKey(x => x.ProductHoldStatusId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Sale).WithMany().HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Restrict);


        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_product_holds_quantity_non_negative",
                "quantity >= 1");
        });
    }
}