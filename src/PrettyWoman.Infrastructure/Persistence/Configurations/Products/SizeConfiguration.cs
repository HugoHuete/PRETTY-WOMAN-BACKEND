using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Products;

public class SizeConfiguration : IEntityTypeConfiguration<Size>
{
    public void Configure (EntityTypeBuilder<Size> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(10);
        builder.HasIndex(x => x.Name).IsUnique();
    }
}