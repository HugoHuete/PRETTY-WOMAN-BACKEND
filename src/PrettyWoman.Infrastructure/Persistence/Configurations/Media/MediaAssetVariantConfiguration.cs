using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Media;

public class MediaAssetVariantConfiguration : IEntityTypeConfiguration<MediaAssetVariant>
{
    public void Configure(EntityTypeBuilder<MediaAssetVariant> builder)
    {
        builder.Property(x => x.StorageKey).IsRequired().HasMaxLength(600);
        builder.Property(x => x.ContentType).IsRequired().HasMaxLength(100);
        builder.HasIndex(x => new { x.MediaAssetId, x.Type }).IsUnique();
        builder.HasIndex(x => new { x.Bucket, x.StorageKey }).IsUnique();
    }
}
