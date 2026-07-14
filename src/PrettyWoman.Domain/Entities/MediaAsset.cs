using PrettyWoman.Domain.Enums;

namespace PrettyWoman.Domain.Entities;

/// <summary>Represents one logical uploaded file and all of its derived variants.</summary>
public class MediaAsset
{
    public Guid Id { get; set; }
    public required string StorageKey { get; set; }
    public MediaBucket OriginalBucket { get; set; }
    public MediaVisibility Visibility { get; set; }
    public required string OriginalContentType { get; set; }
    public long OriginalSizeBytes { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public MediaAssetStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<MediaAssetVariant> Variants { get; set; } = [];
}
