using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Sales;

public class SaleReturnItemConfiguration : IEntityTypeConfiguration<SaleReturnItem>
{
    public void Configure(EntityTypeBuilder<SaleReturnItem> builder)
    {
        builder.Property(x => x.RecognizedUnitAmount).HasPrecision(14, 2);
        builder.Property(x => x.Comments).HasMaxLength(500);
        builder.HasOne(x => x.SaleReturn).WithMany(x => x.Items).HasForeignKey(x => x.SaleReturnId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.OriginalSaleProduct).WithMany().HasForeignKey(x => x.OriginalSaleProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductInventoryIssue).WithMany().HasForeignKey(x => x.ProductInventoryIssueId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.SaleReturnId);
        builder.HasIndex(x => x.OriginalSaleProductId);
        builder.ToTable(t => t.HasCheckConstraint("ck_sale_return_items_quantity_positive", "quantity > 0"));
    }
}
