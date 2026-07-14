using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Media;

public class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> builder)
    {
        builder.Property(x => x.StorageKey).IsRequired().HasMaxLength(500);
        builder.Property(x => x.OriginalContentType).IsRequired().HasMaxLength(100);
        builder.HasIndex(x => x.StorageKey).IsUnique();
        builder.HasMany(x => x.Variants).WithOne(x => x.MediaAsset).HasForeignKey(x => x.MediaAssetId).OnDelete(DeleteBehavior.Cascade);
    }
}
