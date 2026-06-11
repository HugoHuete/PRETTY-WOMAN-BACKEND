using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Orders;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure (EntityTypeBuilder<Supplier> builder)
    {

        builder.Property(x => x.Name).HasMaxLength(50);
        builder.HasIndex(x => x.Name).IsUnique();
        
        builder.Property(x => x.Url).HasMaxLength(300);
    }
}