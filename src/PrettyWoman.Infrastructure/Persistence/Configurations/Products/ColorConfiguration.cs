using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Products;

public class ColorConfiguration : IEntityTypeConfiguration<Color>
{
    public void Configure(EntityTypeBuilder<Color> builder)
    {
        builder.Property(color => color.Name).HasMaxLength(50);
        builder.Property(color => color.HexCode).HasMaxLength(7);
        builder.HasIndex(color => color.Name).IsUnique();
        builder.ToTable(table => table.HasCheckConstraint("ck_colors_hex_code", "hex_code ~ '^#[0-9A-Fa-f]{6}$'"));
    }
}
