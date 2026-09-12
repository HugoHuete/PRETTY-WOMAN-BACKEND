using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrettyWoman.Domain.Entities;

namespace PrettyWoman.Infrastructure.Persistence.Configurations.Media;

public class MediaCleanupItemConfiguration : IEntityTypeConfiguration<MediaCleanupItem>
{
    public void Configure(EntityTypeBuilder<MediaCleanupItem> builder)
    {
        builder.HasKey(item => item.Id);
        builder.Property(item => item.StorageKey).IsRequired().HasMaxLength(600);
        builder.Property(item => item.LastError).HasMaxLength(2000);
        builder.HasIndex(item => new { item.Bucket, item.StorageKey }).IsUnique();
        builder.HasIndex(item => new { item.Status, item.CreatedAtUtc });
    }
}
