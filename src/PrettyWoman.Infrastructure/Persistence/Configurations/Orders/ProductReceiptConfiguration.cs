using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Orders;

public class ProductReceiptConfiguration : IEntityTypeConfiguration<ProductReceipt>
{
    public void Configure(EntityTypeBuilder<ProductReceipt> builder)
    {
        builder.HasOne(x => x.Order).WithMany(x => x.ProductReceipts).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => new { x.OrderId, x.ReceivedDate });
        builder.HasIndex(x => x.ReceivedDate);
        builder.HasIndex(x => x.CreatedAt);
    }
}