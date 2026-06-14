using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Products;

public class SizeGroupConfiguration : IEntityTypeConfiguration<SizeGroup>
{
    public void Configure(EntityTypeBuilder<SizeGroup> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(50);
        builder.HasIndex(x => x.Name).IsUnique();
    }
}
