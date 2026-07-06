using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Orders;

public class ProductReceiptConfiguration : IEntityTypeConfiguration<ProductReceipt>
{
    public void Configure(EntityTypeBuilder<ProductReceipt> builder)
    {
        builder.HasIndex(x => x.ReceivedDate);
        builder.HasIndex(x => x.CreatedAt);
    }
}