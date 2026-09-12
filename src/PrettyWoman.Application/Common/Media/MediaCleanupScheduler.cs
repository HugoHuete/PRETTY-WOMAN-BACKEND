using PrettyWoman.Application.Interfaces;
using PrettyWoman.Domain.Entities;
using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Application.Common.Media;

public static class MediaCleanupScheduler
{
    public static void Enqueue(IApplicationDbContext context, IEnumerable<MediaAsset> assets)
    {
        var items = assets
            .SelectMany(asset => asset.Variants.Select(variant => new MediaCleanupItem
            {
                MediaAssetId = asset.Id,
                Bucket = variant.Bucket,
                StorageKey = variant.StorageKey,
                Status = MediaCleanupStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow
            }))
            .DistinctBy(item => (item.Bucket, item.StorageKey))
            .ToList();

        if (items.Count != 0)
        {
            context.MediaCleanupItems.AddRange(items);
        }
    }
}
