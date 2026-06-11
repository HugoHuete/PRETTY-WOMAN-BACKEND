using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Orders;

public class ShippingCompanyConfiguration : IEntityTypeConfiguration<ShippingCompany>
{
    public void Configure (EntityTypeBuilder<ShippingCompany> builder)
    {

        builder.Property(x => x.Name).HasMaxLength(50);
        builder.HasIndex(x => x.Name).IsUnique();
        
        builder.Property(x => x.Url).HasMaxLength(300);
    }
}