using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Products;

public class SizeConfiguration : IEntityTypeConfiguration<Size>
{
    public void Configure (EntityTypeBuilder<Size> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(10);
        builder.HasIndex(x => new { x.SizeGroupId, x.Name }).IsUnique();

        builder.HasOne(x => x.SizeGroup)
            .WithMany(x => x.Sizes)
            .HasForeignKey(x => x.SizeGroupId)
            .IsRequired();
    }
}
