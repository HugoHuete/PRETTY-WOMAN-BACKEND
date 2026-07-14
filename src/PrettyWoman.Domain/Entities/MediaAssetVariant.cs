using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Domain.Entities;

public class MediaAssetVariant
{
    public Guid Id { get; set; }
    public Guid MediaAssetId { get; set; }
    public MediaVariantType Type { get; set; }
    public MediaBucket Bucket { get; set; }
    public required string StorageKey { get; set; }
    public required string ContentType { get; set; }
    public long SizeBytes { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public MediaAsset? MediaAsset { get; set; }
}
