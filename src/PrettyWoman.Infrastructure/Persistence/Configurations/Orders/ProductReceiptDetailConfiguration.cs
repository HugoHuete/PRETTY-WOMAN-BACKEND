using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Orders;

public class ProductReceiptDetailConfiguration : IEntityTypeConfiguration<ProductReceiptDetail>
{
    public void Configure (EntityTypeBuilder<ProductReceiptDetail> builder)
    {
        builder.HasOne(x => x.ProductReceipt).WithMany(x => x.ProductReceiptDetails).HasForeignKey(x => x.ProductReceiptId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Product).WithMany(x => x.ProductReceiptDetails).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_product_receipt_detail_quantity_non_negative",
                "quantity >= 0");
        });
        
    }
}